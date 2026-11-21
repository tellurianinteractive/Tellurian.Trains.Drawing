using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Tellurian.Trains.Drawing.App.Catalogue;
using Tellurian.Trains.Drawing.App.Translations.Resources;
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

    [TestMethod]
    public void EveryTextUsedInMarkupExistsInEnglish()
    {
        var keys = Directory.GetFiles(RepositoryPaths.App, "*.razor", SearchOption.AllDirectories)
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
            .Concat(Keys<Editing.EndProfiles.MeasureOrigin>("MeasureOrigin"))
            .Concat(Keys<Tellurian.Trains.Schedules.Model.Layouts.TrackDirection>("TrackDirection"))
            .Concat(Keys<Model.Tracks.StationTrackProblemCode>("TrackProblem"))
            .Concat(Keys<Tellurian.Trains.Schedules.Model.Layouts.OperationLocationKind>("LocationKind"))
            .Concat(LocationProblemsShown.Select(code => $"LocationProblem_{code}"))
            .Concat(new[] { "StraightTrack", "CurvedTrack", "FlexTrack", "Turnout", "Crossing", "DoubleSlip" }.Select(n => $"TrackElement_{n}"))
            .Concat(Keys<PlateSegmentKind>("SegmentKind"))
            .Concat(Keys<FilletSide>("FilletSide"));
        foreach (var key in keys) Assert.IsNotNull(Resources.GetString(key, CultureInfo.InvariantCulture), key);
    }

    [TestMethod]
    public void EveryCatalogueFieldHasALabel()
    {
        string[] notShown = ["Id", "Source", "ExternalReferences", "ExtensionData"];
        var properties = new[] { typeof(ModuleStandard), typeof(StandardAddOn), typeof(EndProfileCompatibilityOverride) }
            .SelectMany(t => t.GetProperties().Where(p => p.CanWrite).Select(p => p.Name))
            .Except(notShown);
        foreach (var property in properties)
            Assert.IsNotNull(Resources.GetString(string.Join("_", "Field", property), CultureInfo.InvariantCulture), property);
    }

    /// <summary>
    /// The rules of the shared layout model that the app checks: those of an operation location itself. The rules of
    /// its tracks have the app's own texts (<c>TrackProblem</c>), and what is decided for a location in a layout
    /// (manned, controlled by, instructions) isn't entered in the app.
    /// </summary>
    private static IEnumerable<Tellurian.Trains.Schedules.Model.Layouts.LocationProblemCode> LocationProblemsShown =>
        Enum.GetValues<Tellurian.Trains.Schedules.Model.Layouts.LocationProblemCode>().Except(
        [
            Tellurian.Trains.Schedules.Model.Layouts.LocationProblemCode.TrackDesignationMissing,
            Tellurian.Trains.Schedules.Model.Layouts.LocationProblemCode.TrackDesignationNotUnique,
            Tellurian.Trains.Schedules.Model.Layouts.LocationProblemCode.TrackLengthNegative,
            Tellurian.Trains.Schedules.Model.Layouts.LocationProblemCode.KeyNameWithoutLock,
            Tellurian.Trains.Schedules.Model.Layouts.LocationProblemCode.TrackUnknown,
            Tellurian.Trains.Schedules.Model.Layouts.LocationProblemCode.MannedNotAllowed,
            Tellurian.Trains.Schedules.Model.Layouts.LocationProblemCode.ControlledByNotAllowed,
            Tellurian.Trains.Schedules.Model.Layouts.LocationProblemCode.ControlledByNotManned,
            Tellurian.Trains.Schedules.Model.Layouts.LocationProblemCode.InstructionsNotAllowed,
            Tellurian.Trains.Schedules.Model.Layouts.LocationProblemCode.OperationLocationMissing,
        ]);

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
        System.Xml.Linq.XDocument.Load(Path.Combine(RepositoryPaths.Translations, "Resources", fileName)).Root!.Elements("data")
            .ToDictionary(d => (string)d.Attribute("name")!, d => (string)d.Element("value")!);

    private static string[] Placeholders(string text) => [.. Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Order()];

    [TestMethod]
    public void EveryFileKindHasAName()
    {
        foreach (var kind in Enum.GetValues<DrawingFileKind>())
            Assert.IsNotNull(Resources.GetString($"Kind_{kind}", CultureInfo.InvariantCulture), kind.ToString());
    }
}
