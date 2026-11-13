using Tellurian.Trains.Drawing.Model.Tracks;

namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// One drawing of a module (Registry <c>ConfigurationLabel</c>), in the module's local coordinates:
/// origin at the first exit's end-plate centre, X pointing into the module from it (ModuleDrawing §2).
/// </summary>
public sealed record ModuleVariant
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The configuration label; may be empty for a module with one variant.</summary>
    public string Name { get; init; } = string.Empty;

    public bool IsDefault { get; init; }

    /// <summary>The Registry module row of this variant, if known (spec §10, variant mapping).</summary>
    public int? RegistryModuleId { get; init; }

    public IReadOnlyList<ModuleExit> Exits { get; init; } = [];

    /// <summary>The centre line from the first exit to the second; null if there is none.</summary>
    public PathDrawing? MainPath { get; init; }

    /// <summary>The sides between end plates, in counter-clockwise order.</summary>
    public IReadOnlyList<ModuleSide> Sides { get; init; } = [];

    public IReadOnlyList<Section> Sections { get; init; } = [];
    public IReadOnlyList<InternalJoint> InternalJoints { get; init; } = [];
    public IReadOnlyList<TrackElement> Tracks { get; init; } = [];
    public IReadOnlyList<OperatorSpace> OperatorSpaces { get; init; } = [];
    public IReadOnlyList<PublicSide> PublicSides { get; init; } = [];
    public IReadOnlyList<DuckUnderArea> DuckUnderAreas { get; init; } = [];
    public IReadOnlyList<ModuleLabel> Labels { get; init; } = [];

    /// <summary>Measures entered in or imported from the Registry, for comparison (MD-10).</summary>
    public RegistryMeasures? RegistryMeasures { get; init; }

    public ModuleKind Kind => Exits.Count switch
    {
        0 => ModuleKind.Invalid,
        1 => ModuleKind.EndOfLine,
        2 => ModuleKind.PassThrough,
        _ => ModuleKind.Junction,
    };

    public ModuleExit? Exit(string label) => Exits.FirstOrDefault(e => e.Label == label);

    public override string ToString() => Name;
}
