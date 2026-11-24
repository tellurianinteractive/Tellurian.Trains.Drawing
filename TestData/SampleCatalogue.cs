using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.TestData;

/// <summary>
/// A catalogue for tests: the FREMO and MMM standards, add-ons and end profiles that were the app's built-in catalogue
/// until it was withdrawn (survey step 7). The app has no catalogue of its own; users import one. Read from
/// <c>sample.catalogue.json</c>, embedded in each test project that links this file.
/// </summary>
internal static class SampleCatalogue
{
    private const string ResourceName = "sample.catalogue.json";

    /// <summary>The revision of entries that were entered by hand and not checked against the published PDF drawings.</summary>
    public const string ProvisionalRevision = "provisional";

    private static readonly Lazy<Catalogue> Catalogue = new(Load);

    /// <summary>The sample catalogue.</summary>
    public static Catalogue Instance => Catalogue.Value;

    /// <summary>The text of the catalogue file, e.g. to import it in a test.</summary>
    public static string Json
    {
        get
        {
            using var stream = Stream();
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    private static Catalogue Load()
    {
        using var stream = Stream();
        return DrawingFileSerializer.DeserializeCatalogue(stream);
    }

    private static Stream Stream() =>
        typeof(SampleCatalogue).Assembly.GetManifestResourceStream(ResourceName)
        ?? throw new InvalidOperationException($"The embedded resource {ResourceName} is missing.");
}
