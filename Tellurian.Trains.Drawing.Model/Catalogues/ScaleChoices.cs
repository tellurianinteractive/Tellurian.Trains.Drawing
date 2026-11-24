using Tellurian.Trains.Schedules.Model.Layouts;

namespace Tellurian.Trains.Drawing.Model.Catalogues;

/// <summary>The scales to choose from, e.g. in the settings or for a new standard, also before any catalogue is imported.</summary>
public static class ScaleChoices
{
    /// <summary>
    /// The suite's known scales (<c>Scale.Scales</c>) and those of <paramref name="catalogue"/> that aren't among them,
    /// one per short name, since a scale is chosen by its short name; ordered by short name.
    /// </summary>
    public static IReadOnlyList<Scale> Of(Catalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        return [.. Scale.Scales.Concat(catalogue.Scales)
            .DistinctBy(s => s.ShortName.Trim().ToUpperInvariant())
            .OrderBy(s => s.ShortName, StringComparer.OrdinalIgnoreCase)];
    }
}
