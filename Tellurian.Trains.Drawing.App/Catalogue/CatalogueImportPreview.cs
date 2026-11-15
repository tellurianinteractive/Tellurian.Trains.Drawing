using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Catalogue;

/// <summary>The end profiles found in imported files, compared with the catalogue, before the user chooses what to keep.</summary>
public sealed record CatalogueImportPreview(IReadOnlyList<EndProfileImportItem> Items, IReadOnlyList<ImportProblem> Problems, int Skipped);
