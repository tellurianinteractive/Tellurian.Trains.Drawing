using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Tellurian.Trains.Drawing.App.Catalogue;
using Tellurian.Trains.Drawing.App.Resources;
using Tellurian.Trains.Drawing.Model.AutoDraw;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Tests.Resources;

[TestClass]
public sealed partial class AppTextsTests
{
    private static readonly ResourceManager Resources = new(typeof(AppTexts));

    [GeneratedRegex("""T(?:\.Format\()?\[?\(?"(?<key>[A-Za-z_]+)"[\],]""")]
    private static partial Regex TextKey();

    private static string AppDirectory
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Tellurian.Trains.Drawing.slnx"))) directory = directory.Parent;
            return Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException("The repository root wasn't found."), "Tellurian.Trains.Drawing.App");
        }
    }

    [TestMethod]
    public void EveryTextUsedInMarkupExistsInEnglish()
    {
        var keys = Directory.GetFiles(AppDirectory, "*.razor", SearchOption.AllDirectories)
            .SelectMany(f => TextKey().Matches(File.ReadAllText(f)).Select(m => (File: Path.GetFileName(f), Key: m.Groups["key"].Value)))
            .ToList();
        Assert.IsNotEmpty(keys);
        foreach (var (file, key) in keys)
            Assert.IsNotNull(Resources.GetString(key, CultureInfo.InvariantCulture), $"{file} uses the text {key}, which isn't in AppTexts.resx.");
    }

    [TestMethod]
    [DataRow("RestoreBuiltIn"), DataRow("Delete"), DataRow("ConfirmRestoreBuiltIn"), DataRow("ConfirmDelete")]
    public void TextsChosenInCodeExist(string key) =>
        Assert.IsNotNull(Resources.GetString(key, CultureInfo.InvariantCulture), key);

    [TestMethod]
    public void EveryEnumValueShownInTheUiHasAText()
    {
        var keys = Keys<CatalogueOrigin>("Origin")
            .Concat(Keys<EndProfileImportStatus>("ImportStatus"))
            .Concat(Keys<EndProfileChanges>("Change").Where(k => k != "Change_None"))
            .Concat(Keys<ModuleTemplateKind>("Template"))
            .Concat(Keys<CurveDirection>("CurveDirection"))
            .Concat(Keys<StraightPosition>("StraightPosition"))
            .Concat(Keys<JointFit>("Fit"))
            .Concat(Keys<RailSystem>("RailSystem"))
            .Concat(Keys<CatalogueProblemCode>("Problem"))
            .Concat(Keys<Editing.EndProfiles.MeasureOrigin>("MeasureOrigin"));
        foreach (var key in keys) Assert.IsNotNull(Resources.GetString(key, CultureInfo.InvariantCulture), key);
    }

    [TestMethod]
    public void EveryCatalogueFieldHasALabel()
    {
        string[] notShown = ["Id", "Source", "RegistryId"];
        var properties = new[] { typeof(ModuleStandard), typeof(StandardAddOn), typeof(EndProfileCompatibilityOverride) }
            .SelectMany(t => t.GetProperties().Where(p => p.CanWrite).Select(p => p.Name))
            .Except(notShown);
        foreach (var property in properties)
            Assert.IsNotNull(Resources.GetString(string.Join("_", "Field", property), CultureInfo.InvariantCulture), property);
    }

    private static IEnumerable<string> Keys<TEnum>(string prefix) where TEnum : struct, Enum =>
        Enum.GetNames<TEnum>().Select(n => string.Join("_", prefix, n));

    [TestMethod]
    [DataRow("sv"), DataRow("de"), DataRow("da"), DataRow("nb")]
    public void EveryLanguageHasEveryTextWithTheSamePlaceholders(string language)
    {
        var neutral = ReadResx("AppTexts.resx");
        var translated = ReadResx($"AppTexts.{language}.resx");
        CollectionAssert.AreEquivalent(neutral.Keys.ToArray(), translated.Keys.ToArray(), $"The {language} texts don't have the same keys as the English ones.");
        foreach (var (key, text) in neutral)
            CollectionAssert.AreEquivalent(Placeholders(text), Placeholders(translated[key]), $"{language} {key}: the placeholders differ.");
    }

    [TestMethod]
    [DataRow("sv-SE", "Fortsätt arbeta"), DataRow("de", "Weiterarbeiten"), DataRow("da-DK", "Fortsæt arbejdet"), DataRow("nb-NO", "Fortsett arbeidet"), DataRow("en", "Resume work")]
    public void TextsAreLookedUpInTheUiCulture(string culture, string expected) =>
        Assert.AreEqual(expected, Resources.GetString("Drafts", CultureInfo.GetCultureInfo(culture)));

    private static Dictionary<string, string> ReadResx(string fileName) =>
        System.Xml.Linq.XDocument.Load(Path.Combine(AppDirectory, "Resources", fileName)).Root!.Elements("data")
            .ToDictionary(d => (string)d.Attribute("name")!, d => (string)d.Element("value")!);

    private static string[] Placeholders(string text) => [.. Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Order()];

    [TestMethod]
    public void EveryFileKindHasAName()
    {
        foreach (var kind in Enum.GetValues<DrawingFileKind>())
            Assert.IsNotNull(Resources.GetString($"Kind_{kind}", CultureInfo.InvariantCulture), kind.ToString());
    }
}
