using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>
/// The catalogue that ships with the library (CA-1): scales, standards, add-ons and end profile drawings,
/// read from the embedded catalogue file <c>BuiltInCatalogue.tdcatalogue</c>.
/// </summary>
public static class BuiltInCatalogue
{
    /// <summary>
    /// The <see cref="DocumentReference.Revision"/> of entries that are entered by hand and not yet
    /// checked against the published PDF drawings of FREMO or MMM.
    /// </summary>
    public const string ProvisionalRevision = "provisional";

    internal const string ResourceName = "Tellurian.Trains.Drawing.Model.BuiltInCatalogue.tdcatalogue";

    private static readonly Lazy<DrawingCatalogue> Catalogue = new(Load);

    public static DrawingCatalogue Instance => Catalogue.Value;

    private static DrawingCatalogue Load()
    {
        using var stream = typeof(BuiltInCatalogue).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The embedded resource {ResourceName} is missing.");
        return DrawingFileSerializer.DeserializeCatalogue(stream);
    }
}
