using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.App.Settings;

namespace Tellurian.Trains.Drawing.App.Tests.Settings;

[TestClass]
public sealed class SettingsFileTests
{
    private static readonly UserSettings Sample = new() { UserName = "Anna", CountryId = 46, Scale = "H0", ApiKey = "secret" };

    [TestMethod]
    public void SettingsAndCultureRoundTrip()
    {
        var file = SettingsFile.Read(SettingsFile.Write(Sample, "sv-SE"));
        Assert.IsNotNull(file);
        Assert.AreEqual(Sample, file.Settings);
        Assert.AreEqual("sv-SE", file.Culture);
    }

    [TestMethod]
    public void FileNamesItsFormatAndVersion()
    {
        var json = SettingsFile.Write(Sample, null);
        StringAssert.Contains(json, $"\"format\": \"{SettingsFile.FormatName}\"");
        StringAssert.Contains(json, "\"version\": 1");
    }

    [TestMethod]
    [DataRow("not json")]
    [DataRow("{}")]
    [DataRow("""{ "format": "tellurian-drawing", "kind": "module", "schemaVersion": 1 }""")]
    [DataRow("""{ "format": "tellurian-drawing-settings", "version": 2, "settings": {} }""")]
    [DataRow("""{ "format": "tellurian-drawing-settings", "version": 1 }""")]
    public void OtherFilesAreRefused(string json) => Assert.IsNull(SettingsFile.Read(json));

    [TestMethod]
    [DataRow("Firefox", BrowserKind.Firefox)]
    [DataRow("edge", BrowserKind.Edge)]
    [DataRow("Other", BrowserKind.Other)]
    [DataRow("Netscape", BrowserKind.Other)]
    [DataRow(null, BrowserKind.Other)]
    public void BrowserIsRecognisedFromTheScriptsName(string? name, BrowserKind kind) =>
        Assert.AreEqual(kind, new BrowserCapabilities { Browser = name }.Kind);
}
