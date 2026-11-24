using Tellurian.Trains.Drawing.Model.Catalogues;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Catalogues;

/// <summary>
/// The end profiles found in imported files, compared with the catalogue, before the user chooses what to keep, and
/// the other entries of the catalogue files among them (<see cref="Entries"/>), which are taken in with the chosen profiles.
/// </summary>
public sealed record CatalogueImportPreview(IReadOnlyList<EndProfileImportItem> Items, IReadOnlyList<ImportProblem> Problems, int Skipped, Catalogue Entries)
{
    /// <summary>Whether the files had scales, standards, add-ons or overrides.</summary>
    public bool HasEntries => Entries.Scales.Count + Entries.Standards.Count + Entries.AddOns.Count + Entries.Overrides.Count > 0;
}
