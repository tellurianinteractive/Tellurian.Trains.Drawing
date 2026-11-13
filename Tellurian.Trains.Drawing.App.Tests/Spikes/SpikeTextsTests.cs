using System.Globalization;
using System.Resources;
using Tellurian.Trains.Drawing.App.Spikes.Resources;

namespace Tellurian.Trains.Drawing.App.Tests.Spikes;

[TestClass]
public sealed class SpikeTextsTests
{
    private static readonly ResourceManager Resources = new(typeof(SpikeTexts));

    [TestMethod]
    [DataRow("en", "Save")]
    [DataRow("sv-SE", "Spara")]
    [DataRow("de", "Speichern")]
    [DataRow("da-DK", "Gem")]
    [DataRow("nb-NO", "Lagre")]
    public void TranslationsResolveFromSatelliteAssemblies(string culture, string expected) =>
        Assert.AreEqual(expected, Resources.GetString("Save", CultureInfo.GetCultureInfo(culture)));

    [TestMethod]
    public void MissingLanguageFallsBackToNeutralEnglish() =>
        Assert.AreEqual("Save", Resources.GetString("Save", CultureInfo.GetCultureInfo("fr-FR")));
}
