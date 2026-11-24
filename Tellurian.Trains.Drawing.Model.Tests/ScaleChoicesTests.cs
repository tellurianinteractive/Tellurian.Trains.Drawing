using Tellurian.Trains.Drawing.Model.Catalogues;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class ScaleChoicesTests
{
    [TestMethod]
    public void TheKnownScalesAreOfferedWithoutACatalogue()
    {
        var names = ScaleChoices.Of(Catalogue.Empty).Select(s => s.ShortName).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "H0", "N", "TT", "0" }, names);
        Assert.HasCount(names.Distinct().Count(), names, "One choice per short name.");
    }

    [TestMethod]
    public void ACatalogueScaleThatIsntKnownIsAdded()
    {
        var catalogue = Catalogue.Empty with { Scales = [new() { ShortName = "Z", Denominator = 220 }, new() { ShortName = "h0", Denominator = 87 }] };

        var names = ScaleChoices.Of(catalogue).Select(s => s.ShortName).ToArray();

        CollectionAssert.Contains(names, "Z");
        Assert.AreEqual(1, names.Count(n => string.Equals(n, "H0", StringComparison.OrdinalIgnoreCase)));
    }
}
