using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>
/// Scales, module standards, add-ons, end profiles and compatibility overrides, with lookups.
/// Short names and designations are compared without regard to case.
/// </summary>
public sealed record DrawingCatalogue
{
    public static DrawingCatalogue Empty { get; } = new();

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

    private static bool Same(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
