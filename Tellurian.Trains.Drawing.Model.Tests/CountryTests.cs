using Tellurian.Trains.Drawing.Model.Catalogues;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class CountryTests
{
    private static Catalogue BuiltIn => SampleCatalogue.Instance;

    private static bool IsMmm(string shortName) => shortName.StartsWith("MMM", StringComparison.OrdinalIgnoreCase);

    [TestMethod]
    public void MmmStandardsApplyInSwedenAndFremoStandardsEverywhere()
    {
        foreach (var standard in BuiltIn.Standards)
        {
            if (IsMmm(standard.ShortName)) CollectionAssert.AreEqual(new[] { "SE" }, (standard.Countries ?? []).ToArray(), standard.ShortName);
            else Assert.IsEmpty(standard.Countries ?? [], standard.ShortName);
        }
    }

    [TestMethod]
    [DataRow(null, false), DataRow("DE", false), DataRow("SE", true), DataRow("se", true)]
    public void SwedishStandardAppliesOnlyInSweden(string? country, bool expected)
    {
        var mmm = BuiltIn.Standards.First(s => IsMmm(s.ShortName));
        Assert.AreEqual(expected, mmm.AppliesTo(country));
        Assert.IsTrue(BuiltIn.Standards.First(s => !IsMmm(s.ShortName)).AppliesTo(country));
    }

    [TestMethod]
    public void OutsideSwedenTheMmmEntriesAreNotOffered()
    {
        foreach (var country in new string?[] { null, "DE" })
        {
            var catalogue = BuiltIn.ForCountry(country);
            Assert.IsNotEmpty(catalogue.Standards);
            Assert.IsFalse(catalogue.Standards.Any(s => IsMmm(s.ShortName)), $"Standards for {country ?? "no country"}.");
            Assert.IsEmpty(catalogue.AddOns, "The MMM add-ons belong to MMM-2R.");
            Assert.IsFalse(catalogue.EndProfiles.Any(p => p.Standards.Count > 0 && p.Standards.All(s => IsMmm(s.Name))), "MMM end profiles.");
            Assert.IsTrue(catalogue.Overrides.All(o => catalogue.FindEndProfile(o.FirstEndProfileId) is not null && catalogue.FindEndProfile(o.SecondEndProfileId) is not null));
        }
    }

    [TestMethod]
    public void InSwedenEverythingIsOffered() =>
        Assert.AreSame(BuiltIn, BuiltIn.ForCountry("SE"));

    [TestMethod]
    public void EndProfilesWithoutStandardAreAlwaysOffered()
    {
        var loose = TestCatalogue.Mmm480.With(edited => { edited.Id = Guid.NewGuid(); edited.Standards = []; });
        Assert.IsNotNull(BuiltIn.With(loose).ForCountry(null).FindEndProfile(loose.Id));
    }

    [TestMethod]
    [DataRow("SE", true), DataRow("se", false), DataRow("SWE", false), DataRow("", false), DataRow(null, false)]
    public void CountryCodes(string? value, bool expected) => Assert.AreEqual(expected, CatalogueValidation.IsCountryCode(value));

    [TestMethod]
    public void StandardWithAnInvalidCountryIsReported()
    {
        var standard = new ModuleStandard { Id = Guid.NewGuid(), ShortName = "MY-H0", Scale = new() { ShortName = "H0" }, Gauges = [new(16.5)], Countries = ["SE", "Sweden"] };
        var problem = Assert.ContainsSingle(CatalogueValidation.Validate(standard, TestCatalogue.Instance));
        Assert.AreEqual(new CatalogueProblem(CatalogueProblemCode.CountryInvalid, nameof(ModuleStandard.Countries)), problem);
    }
}
