using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Halls;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>
/// A hall plus the modules placed in it (HP-1). Modules are ordered into lines of layouts; their poses are
/// computed from each layout's anchor, not stored. The plan is self-contained (spec §6): it embeds the hall,
/// every module it uses, their end profiles and the catalogue entries needed to evaluate them.
/// Embedded modules are read-only in the plan (HP-16).
/// </summary>
public sealed record HallPlan
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Note { get; init; }

    /// <summary>An embedded copy of the hall (HA-5).</summary>
    public required Hall Hall { get; init; }

    /// <summary>The short name of the scale of the primary standard; all accepted standards have it.</summary>
    public required string Scale { get; init; }

    public required CatalogueReference PrimaryStandard { get; init; }

    /// <summary>Further standards accepted in the plan, besides the primary one (HP-1).</summary>
    public IReadOnlyList<CatalogueReference> AcceptedStandards { get; init; } = [];

    /// <summary>Add-ons the plan prefers; modules that don't meet them are warned about (VA-14).</summary>
    public IReadOnlyList<CatalogueReference> PreferredAddOns { get; init; } = [];

    public HallPlanSettings Settings { get; init; } = new();

    public IReadOnlyList<Layout> Layouts { get; init; } = [];

    /// <summary>Joints whose properties differ from the defaults: join-method override, clamps, note (HP-11).</summary>
    public IReadOnlyList<Joint> Joints { get; init; } = [];

    public IReadOnlyList<PublicArea> PublicAreas { get; init; } = [];
    public IReadOnlyList<WalkingPath> WalkingPaths { get; init; } = [];
    public IReadOnlyList<Acknowledgement> Acknowledgements { get; init; } = [];

    /// <summary>Read-only copies of the placed modules, with their revisions (HP-16).</summary>
    public IReadOnlyList<Module> Modules { get; init; } = [];

    /// <summary>Every end profile the embedded modules use.</summary>
    public IReadOnlyList<EndProfile> EndProfiles { get; init; } = [];

    /// <summary>The standards of the plan and of the embedded modules, e.g. for rail-top heights and clearances.</summary>
    public IReadOnlyList<ModuleStandard> Standards { get; init; } = [];

    /// <summary>Compatibility overrides that apply to pairs of the embedded end profiles.</summary>
    public IReadOnlyList<EndProfileCompatibilityOverride> Overrides { get; init; } = [];

    /// <summary>Incremented on every save.</summary>
    public int Revision { get; init; }

    /// <summary>The primary standard and the further accepted standards.</summary>
    public IEnumerable<CatalogueReference> AllAcceptedStandards() => AcceptedStandards.Prepend(PrimaryStandard).DistinctBy(s => s.Id);

    public bool IsAccepted(Guid standardId) => AllAcceptedStandards().Any(s => s.Id == standardId);

    /// <summary>Every line entry of every layout.</summary>
    public IEnumerable<LineEntry> Entries() => Layouts.SelectMany(l => l.Entries());

    /// <summary>The entry that places <paramref name="moduleId"/>; a module is placed at most once (HP-7).</summary>
    public LineEntry? FindEntry(Guid moduleId) => Entries().FirstOrDefault(e => e.ModuleId == moduleId);

    /// <summary>Modules placed more than once, which HP-7 doesn't allow.</summary>
    public IEnumerable<Guid> ModulesPlacedMoreThanOnce() =>
        Entries().GroupBy(e => e.ModuleId).Where(g => g.Count() > 1).Select(g => g.Key);

    public Module? FindModule(Guid id) => Modules.FirstOrDefault(m => m.Id == id);

    public EndProfile? FindEndProfile(Guid id) => EndProfiles.FirstOrDefault(p => p.Id == id);

    public ModuleStandard? FindStandard(Guid id) => Standards.FirstOrDefault(s => s.Id == id);

    /// <summary>The stored properties of the joint between two exits, in either order, if any.</summary>
    public Joint? FindJoint(ExitReference a, ExitReference b) => Joints.FirstOrDefault(j => j.Connects(a, b));

    public override string ToString() => Name;
}
