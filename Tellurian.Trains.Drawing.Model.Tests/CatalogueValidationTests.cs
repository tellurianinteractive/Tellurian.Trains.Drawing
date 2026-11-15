using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class CatalogueValidationTests
{
    private static DrawingCatalogue Catalogue => TestCatalogue.Instance;

    private static ModuleStandard Fremo => Catalogue.FindStandard(TestCatalogue.FremoId)!;

    private static ModuleStandard NewStandard() => new() { Id = Guid.NewGuid(), ShortName = "MY-H0", Scale = "H0", NormalGauge = 16.5 };

    private static CatalogueProblemCode[] Codes(IEnumerable<CatalogueProblem> problems) => [.. problems.Select(p => p.Code)];

    [TestMethod]
    public void ValidStandardHasNoProblems()
    {
        Assert.IsEmpty(CatalogueValidation.Validate(NewStandard(), Catalogue));
        foreach (var standard in BuiltInCatalogue.Instance.Standards)
            Assert.IsEmpty(CatalogueValidation.Validate(standard, BuiltInCatalogue.Instance), $"{standard.ShortName} is valid as it is built in.");
    }

    [TestMethod]
    public void EditedStandardMayKeepItsOwnName()
    {
        var builtIn = BuiltInCatalogue.Instance.Standards[0];
        Assert.IsEmpty(CatalogueValidation.Validate(builtIn with { MinimumClearance = 50 }, BuiltInCatalogue.Instance));
    }

    [TestMethod]
    public void StandardProblemsAreListedPerProperty()
    {
        var standard = NewStandard() with
        {
            ShortName = " fremo/h0-re ",
            Scale = "Z",
            NormalGauge = 0,
            RailTopHeight = -1,
            TrackColor = "blue",
            DefaultSingleTrackEndProfileId = TestCatalogue.Mmm480.Id,
        };
        var problems = CatalogueValidation.Validate(standard, Catalogue);
        CollectionAssert.AreEquivalent(
            new[] { "ShortName", "Scale", "NormalGauge", "RailTopHeight", "TrackColor", "DefaultSingleTrackEndProfileId" },
            problems.Select(p => p.Property).ToArray());
        Assert.AreEqual(CatalogueProblemCode.ShortNameInUse, problems[0].Code);
    }

    [TestMethod]
    public void MissingNameIsReported() =>
        CollectionAssert.AreEqual(new[] { CatalogueProblemCode.ShortNameMissing }, Codes(CatalogueValidation.Validate(NewStandard() with { ShortName = " " }, Catalogue)));

    [TestMethod]
    [DataRow("#1f5fbf", true), DataRow("#abc", true), DataRow("1f5fbf", false), DataRow("#12345", false), DataRow("#gggggg", false)]
    public void HexColors(string value, bool expected) => Assert.AreEqual(expected, CatalogueValidation.IsHexColor(value));

    [TestMethod]
    public void AddOnNamesAreUniquePerBaseStandard()
    {
        var summer = Catalogue.AddOns.Single();
        var duplicate = new StandardAddOn { Id = Guid.NewGuid(), ShortName = "mmm-summer", BaseStandardId = summer.BaseStandardId };
        CollectionAssert.AreEqual(new[] { CatalogueProblemCode.ShortNameInUse }, Codes(CatalogueValidation.Validate(duplicate, Catalogue)));
        Assert.IsEmpty(CatalogueValidation.Validate(duplicate with { BaseStandardId = TestCatalogue.FremoId }, Catalogue));
    }

    [TestMethod]
    public void AddOnNeedsAKnownBaseStandard() =>
        CollectionAssert.AreEqual(new[] { CatalogueProblemCode.BaseStandardUnknown },
            Codes(CatalogueValidation.Validate(new StandardAddOn { Id = Guid.NewGuid(), ShortName = "X", BaseStandardId = Guid.NewGuid() }, Catalogue)));

    [TestMethod]
    public void OverrideNeedsTwoDifferentProfilesOfOneScale()
    {
        EndProfileCompatibilityOverride Entry(Guid a, Guid b) => new() { FirstEndProfileId = a, SecondEndProfileId = b, Fit = JointFit.FitsWithClamps };
        Assert.IsEmpty(CatalogueValidation.Validate(Entry(TestCatalogue.FremoSingle.Id, TestCatalogue.Mmm480.Id), Catalogue));
        CollectionAssert.AreEqual(new[] { CatalogueProblemCode.SameEndProfile }, Codes(CatalogueValidation.Validate(Entry(TestCatalogue.Mmm480.Id, TestCatalogue.Mmm480.Id), Catalogue)));
        CollectionAssert.AreEqual(new[] { CatalogueProblemCode.EndProfileUnknown }, Codes(CatalogueValidation.Validate(Entry(Guid.NewGuid(), TestCatalogue.Mmm480.Id), Catalogue)));
    }

    [TestMethod]
    public void SecondOverrideForAPairIsRefusedUnlessItReplacesTheFirst()
    {
        var existing = new EndProfileCompatibilityOverride { FirstEndProfileId = TestCatalogue.FremoSingle.Id, SecondEndProfileId = TestCatalogue.Mmm480.Id, Fit = JointFit.FitsWithClamps };
        var catalogue = Catalogue.With(existing);
        var reversed = existing with { FirstEndProfileId = existing.SecondEndProfileId, SecondEndProfileId = existing.FirstEndProfileId, Fit = JointFit.Fits };
        CollectionAssert.AreEqual(new[] { CatalogueProblemCode.OverrideExists }, Codes(CatalogueValidation.Validate(reversed, catalogue)));
        Assert.IsEmpty(CatalogueValidation.Validate(reversed, catalogue, replacing: existing));
        Assert.AreEqual(JointFit.Fits, catalogue.With(reversed, existing).Overrides.Single().Fit);
    }

    [TestMethod]
    public void WithAndWithoutReplaceAndRemoveEntries()
    {
        var edited = Fremo with { MinimumClearance = 77 };
        var catalogue = Catalogue.With(edited);
        Assert.HasCount(Catalogue.Standards.Count, catalogue.Standards);
        Assert.AreEqual(77, catalogue.FindStandard(Fremo.Id)!.MinimumClearance);
        Assert.IsNull(catalogue.WithoutStandard(Fremo.Id).FindStandard(Fremo.Id));

        var addOn = new StandardAddOn { Id = Guid.NewGuid(), ShortName = "X", BaseStandardId = Fremo.Id };
        Assert.HasCount(Catalogue.AddOns.Count + 1, Catalogue.With(addOn).AddOns);
        Assert.HasCount(Catalogue.AddOns.Count, Catalogue.With(addOn).WithoutAddOn(addOn.Id).AddOns);
    }
}
