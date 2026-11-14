using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Tellurian.Trains.Drawing.App.Resources;
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
    public void EveryFileKindHasAName()
    {
        foreach (var kind in Enum.GetValues<DrawingFileKind>())
            Assert.IsNotNull(Resources.GetString($"Kind_{kind}", CultureInfo.InvariantCulture), kind.ToString());
    }
}
