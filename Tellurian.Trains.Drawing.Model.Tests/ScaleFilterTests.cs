using Tellurian.Trains.Drawing.Model.Catalogue;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class ScaleFilterTests
{
    private static DrawingCatalogue BuiltIn => BuiltInCatalogue.Instance;

    [TestMethod]
    [DataRow("N"), DataRow("n "), DataRow("H0")]
    public void OnlyEntriesOfTheScaleAreOffered(string scale)
    {
        var catalogue = BuiltIn.ForScale(scale);

        Assert.IsNotEmpty(catalogue.Standards);
        Assert.IsTrue(catalogue.Standards.All(s => s.ScaleName.Equals(scale.Trim(), StringComparison.OrdinalIgnoreCase)));
        Assert.IsNotEmpty(catalogue.EndProfiles);
        Assert.IsTrue(catalogue.EndProfiles.All(p => p.Scale.Equals(scale.Trim(), StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(catalogue.AddOns.All(a => catalogue.FindStandard(a.BaseStandardId) is not null));
        CollectionAssert.AreEqual(BuiltIn.Scales.ToArray(), catalogue.Scales.ToArray(), "All scales stay, to choose from.");
    }

    [TestMethod]
    [DataRow(null), DataRow(""), DataRow("  ")]
    public void WithoutAScaleEverythingIsOffered(string? scale) =>
        Assert.AreSame(BuiltIn, BuiltIn.ForScale(scale));

    [TestMethod]
    public void CountryAndScaleCombine()
    {
        var swedishH0 = BuiltIn.ForChoices("SE", "H0");
        Assert.IsTrue(swedishH0.Standards.Any(s => s.ShortName.StartsWith("MMM", StringComparison.Ordinal)));
        Assert.IsTrue(swedishH0.Standards.All(s => s.ScaleName == "H0"));

        var germanN = BuiltIn.ForChoices("DE", "N");
        Assert.IsTrue(germanN.Standards.All(s => s.ScaleName == "N" && s.Countries is not { Count: > 0 }));
        Assert.IsEmpty(germanN.AddOns);
    }
}
