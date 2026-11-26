namespace Tellurian.Trains.Drawing.Model.Catalogues;

/// <summary>
/// The properties of a module standard that Module Draw uses: its short name, its scale and the countries it applies in
/// (CA-6). The shared standard has many more, which the Module Registry and other apps use; Module Draw neither shows
/// nor checks them, and keeps their values as they come in a catalogue file (decided 2026-11-26).
/// </summary>
public static class StandardFields
{
    /// <summary>The names of the properties shown and edited, as catalogue problems name them.</summary>
    public static IReadOnlySet<string> Shown { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        nameof(ModuleStandard.ShortName),
        nameof(ModuleStandard.Scale),
        nameof(ModuleStandard.Countries),
    };

    /// <summary>The problems of a standard with the properties Module Draw uses; others, e.g. a missing gauge, are left out.</summary>
    public static IReadOnlyList<CatalogueProblem> OfShown(IEnumerable<CatalogueProblem> problems) =>
        [.. problems.Where(p => Shown.Contains(p.Property))];

    /// <summary>The problems of a catalogue's entries, with only those of a standard's properties that Module Draw uses.</summary>
    public static IReadOnlyList<CatalogueEntryProblem> OfShown(IEnumerable<CatalogueEntryProblem> problems) =>
        [.. problems.Where(p => p.Kind != CatalogueEntryKind.Standard || Shown.Contains(p.Problem.Property))];
}
