using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>
/// Scales, module standards, add-ons, end profiles and compatibility overrides, with lookups.
/// Short names and designations are compared without regard to case.
/// </summary>
public sealed record DrawingCatalogue
{
    public static DrawingCatalogue Empty { get; } = new();

    /// <summary>The version of the catalogue's content, e.g. of the built-in catalogue (CA-1).</summary>
    public string? Version { get; init; }

    public IReadOnlyList<Scale> Scales { get; init; } = [];
    public IReadOnlyList<ModuleStandard> Standards { get; init; } = [];
    public IReadOnlyList<StandardAddOn> AddOns { get; init; } = [];
    public IReadOnlyList<EndProfile> EndProfiles { get; init; } = [];
    public IReadOnlyList<EndProfileCompatibilityOverride> Overrides { get; init; } = [];

    public Scale? FindScale(string shortName) => Scales.FirstOrDefault(s => Same(s.ShortName, shortName));

    public ModuleStandard? FindStandard(Guid id) => Standards.FirstOrDefault(s => s.Id == id);

    public ModuleStandard? FindStandard(string shortName) => Standards.FirstOrDefault(s => Same(s.ShortName, shortName));

    public StandardAddOn? FindAddOn(Guid baseStandardId, string shortName) =>
        AddOns.FirstOrDefault(a => a.BaseStandardId == baseStandardId && Same(a.ShortName, shortName));

    public IEnumerable<StandardAddOn> AddOnsOf(Guid baseStandardId) => AddOns.Where(a => a.BaseStandardId == baseStandardId);

    public EndProfile? FindEndProfile(Guid id) => EndProfiles.FirstOrDefault(p => p.Id == id);

    public EndProfile? FindEndProfile(string designation, string scale) =>
        EndProfiles.FirstOrDefault(p => Same(p.Designation, designation) && Same(p.Scale, scale));

    /// <summary>The end profiles that belong to the standard.</summary>
    public IEnumerable<EndProfile> EndProfilesOf(Guid standardId) => EndProfiles.Where(p => p.Standards.Any(s => s.Id == standardId));

    /// <summary>The catalogue with <paramref name="standard"/> added, or replacing the standard with the same id.</summary>
    public DrawingCatalogue With(ModuleStandard standard) => this with { Standards = [.. Standards.Where(s => s.Id != standard.Id), standard] };

    /// <summary>The catalogue with <paramref name="addOn"/> added, or replacing the add-on with the same id.</summary>
    public DrawingCatalogue With(StandardAddOn addOn) => this with { AddOns = [.. AddOns.Where(a => a.Id != addOn.Id), addOn] };

    /// <summary>The catalogue with <paramref name="entry"/> added, replacing <paramref name="replacing"/> and any override of the same pair.</summary>
    public DrawingCatalogue With(EndProfileCompatibilityOverride entry, EndProfileCompatibilityOverride? replacing = null) => this with
    {
        Overrides = [.. Overrides.Where(o => o != replacing && !o.Applies(entry.FirstEndProfileId, entry.SecondEndProfileId)), entry],
    };

    public DrawingCatalogue WithoutStandard(Guid id) => this with { Standards = [.. Standards.Where(s => s.Id != id)] };

    public DrawingCatalogue WithoutAddOn(Guid id) => this with { AddOns = [.. AddOns.Where(a => a.Id != id)] };

    public DrawingCatalogue WithoutOverride(EndProfileCompatibilityOverride entry) => this with { Overrides = [.. Overrides.Where(o => o != entry)] };

    /// <summary>
    /// The catalogue to work with: the built-in entries and the user's own (CA-2), which are kept apart.
    /// A user entry replaces a built-in one with the same id (or, for scales, short name).
    /// </summary>
    public static DrawingCatalogue Merge(DrawingCatalogue builtIn, DrawingCatalogue user)
    {
        ArgumentNullException.ThrowIfNull(builtIn);
        ArgumentNullException.ThrowIfNull(user);
        return new()
        {
            Version = builtIn.Version,
            Scales = Layer(builtIn.Scales, user.Scales, s => s.ShortName.Trim().ToUpperInvariant()),
            Standards = Layer(builtIn.Standards, user.Standards, s => s.Id),
            AddOns = Layer(builtIn.AddOns, user.AddOns, a => a.Id),
            EndProfiles = Layer(builtIn.EndProfiles, user.EndProfiles, p => p.Id),
            Overrides = [.. builtIn.Overrides.Where(b => !user.Overrides.Any(u => u.Applies(b.FirstEndProfileId, b.SecondEndProfileId))), .. user.Overrides],
        };
    }

    private static IReadOnlyList<T> Layer<T, TKey>(IReadOnlyList<T> builtIn, IReadOnlyList<T> user, Func<T, TKey> key)
    {
        var userKeys = user.Select(key).ToHashSet();
        return [.. builtIn.Where(b => !userKeys.Contains(key(b))), .. user];
    }

    private static bool Same(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
