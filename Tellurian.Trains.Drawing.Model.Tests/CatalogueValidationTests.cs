using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class CatalogueValidationTests
{
    private static DrawingCatalogue Catalogue => TestCatalogue.Instance;

    private static ModuleStandard Fremo => Catalogue.FindStandard(TestCatalogue.FremoId)!;

    private static ModuleStandard NewStandard() => new() { Id = Guid.NewGuid(), ShortName = "MY-H0", Scale = new() { ShortName = "H0" }, Gauges = [new(16.5)] };

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
        Assert.IsEmpty(CatalogueValidation.Validate(builtIn.With(edited => { edited.MinimumClearance = 50; }), BuiltInCatalogue.Instance));
    }

    [TestMethod]
    public void StandardProblemsAreListedPerProperty()
    {
        var standard = NewStandard().With(edited => { edited.ShortName = " fremo/h0-re "; edited.Scale = new() { ShortName = "Z" }; edited.Gauges = [new(0)]; edited.RailTopHeight = -1; edited.TrackColor = "blue"; edited.DefaultSingleTrackEndProfileId = TestCatalogue.Mmm480.Id; });
        var problems = CatalogueValidation.Validate(standard, Catalogue);
        CollectionAssert.AreEquivalent(
            new[] { "ShortName", "Scale", "NormalGauge", "RailTopHeight", "TrackColor", "DefaultSingleTrackEndProfileId" },
            problems.Select(p => p.Property).ToArray());
        Assert.AreEqual(CatalogueProblemCode.ShortNameInUse, problems[0].Code);
    }

    [TestMethod]
    public void MissingNameIsReported() =>
        CollectionAssert.AreEqual(new[] { CatalogueProblemCode.ShortNameMissing }, Codes(CatalogueValidation.Validate(NewStandard().With(edited => { edited.ShortName = " "; }), Catalogue)));

    [TestMethod]
    [DataRow("#1f5fbf", true), DataRow("#abc", true), DataRow("1f5fbf", false), DataRow("#12345", false), DataRow("#gggggg", false)]
    public void HexColors(string value, bool expected) => Assert.AreEqual(expected, CatalogueValidation.IsHexColor(value));

    [TestMethod]
    public void AddOnNamesAreUniquePerBaseStandard()
    {
        var summer = Catalogue.AddOns.Single();
        var duplicate = new StandardAddOn { Id = Guid.NewGuid(), ShortName = "mmm-summer", BaseStandardId = summer.BaseStandardId };
        CollectionAssert.AreEqual(new[] { CatalogueProblemCode.ShortNameInUse }, Codes(CatalogueValidation.Validate(duplicate, Catalogue)));
        Assert.IsEmpty(CatalogueValidation.Validate(duplicate.With(edited => { edited.BaseStandardId = TestCatalogue.FremoId; }), Catalogue));
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
        var edited = Fremo.With(edited => { edited.MinimumClearance = 77; });
        var catalogue = Catalogue.With(edited);
        Assert.HasCount(Catalogue.Standards.Count, catalogue.Standards);
        Assert.AreEqual(77, catalogue.FindStandard(Fremo.Id!.Value)!.MinimumClearance);
        Assert.IsNull(catalogue.WithoutStandard(Fremo.Id!.Value).FindStandard(Fremo.Id!.Value));

        var addOn = new StandardAddOn { Id = Guid.NewGuid(), ShortName = "X", BaseStandardId = Fremo.Id!.Value };
        Assert.HasCount(Catalogue.AddOns.Count + 1, Catalogue.With(addOn).AddOns);
        Assert.HasCount(Catalogue.AddOns.Count, Catalogue.With(addOn).WithoutAddOn(addOn.Id).AddOns);
    }

    [TestMethod]
    public void ValidUserEntriesHaveNoProblems()
    {
        var user = DrawingCatalogue.Empty
            .With(NewStandard())
            .With(new StandardAddOn { Id = Guid.NewGuid(), ShortName = "X", BaseStandardId = Fremo.Id!.Value })
            .With(new EndProfileCompatibilityOverride { FirstEndProfileId = TestCatalogue.FremoSingle.Id, SecondEndProfileId = TestCatalogue.Mmm480.Id, Fit = JointFit.FitsWithClamps });
        Assert.IsEmpty(CatalogueValidation.ValidateUserEntries(user, DrawingCatalogue.Merge(Catalogue, user)));
    }

    [TestMethod]
    public void UserEntriesThatBecameInvalidAreReportedWithTheirEntry()
    {
        var standard = NewStandard().With(edited => { edited.DefaultSingleTrackEndProfileId = Guid.NewGuid(); });
        var addOn = new StandardAddOn { Id = Guid.NewGuid(), ShortName = "X", BaseStandardId = Guid.NewGuid() };
        var entry = new EndProfileCompatibilityOverride { FirstEndProfileId = TestCatalogue.FremoSingle.Id, SecondEndProfileId = Guid.NewGuid(), Fit = JointFit.Fits };
        var user = DrawingCatalogue.Empty.With(standard).With(addOn).With(entry);

        var problems = CatalogueValidation.ValidateUserEntries(user, DrawingCatalogue.Merge(Catalogue, user));

        CollectionAssert.AreEqual(new[]
        {
            new CatalogueEntryProblem(CatalogueEntryKind.Standard, standard.Id!.Value, null, new(CatalogueProblemCode.EndProfileNotOfStandard, "DefaultSingleTrackEndProfileId")),
            new CatalogueEntryProblem(CatalogueEntryKind.AddOn, addOn.Id, null, new(CatalogueProblemCode.BaseStandardUnknown, "BaseStandardId")),
            new CatalogueEntryProblem(CatalogueEntryKind.Override, entry.FirstEndProfileId, entry.SecondEndProfileId, new(CatalogueProblemCode.EndProfileUnknown, "SecondEndProfileId")),
        }, problems.ToArray());
    }
}
