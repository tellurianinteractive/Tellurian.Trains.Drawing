namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>
/// A standard that modules follow, e.g. FREMO H0-RE, MMM-2R or MMM-3R
/// (Module Registry <c>ModuleStandard</c>, with the drawing app's additions).
/// </summary>
public sealed record ModuleStandard
{
    public const double DefaultRailTopHeight = 1300;
    public const string DefaultTrackColor = "#1f5fbf";

    public required Guid Id { get; init; }
    public required string ShortName { get; init; }

    /// <summary>The short name of the <see cref="Catalogue.Scale"/>.</summary>
    public required string Scale { get; init; }

    public string? TrackSystem { get; init; }
    public RailSystem RailSystem { get; init; }
    public required double NormalGauge { get; init; }
    public double? NarrowGauge { get; init; }
    public string? Couplings { get; init; }
    public string? Electricity { get; init; }
    public string? Wheelset { get; init; }

    /// <summary>Height of rail top above the floor (RÖK), mm. All other heights are offsets from it.</summary>
    public double RailTopHeight { get; init; } = DefaultRailTopHeight;

    /// <summary>Default minimum clearance between modules, and between modules and walls, mm.</summary>
    public double MinimumClearance { get; init; }

    /// <summary>Default depth of operator spaces, mm.</summary>
    public double OperatorSpaceDepth { get; init; } = 800;

    /// <summary>Colour of tracks of this standard, as a CSS hex colour.</summary>
    public string TrackColor { get; init; } = DefaultTrackColor;

    /// <summary>Minimum radius of reverse curves that auto-draw generates, mm.</summary>
    public double? MinimumReverseCurveRadius { get; init; }

    /// <summary>Curve radius auto-draw uses when a module has an angle but no radius, mm.</summary>
    public double? DefaultCurveRadius { get; init; }

    /// <summary>End profile auto-draw uses for inferred single-track exits.</summary>
    public Guid? DefaultSingleTrackEndProfileId { get; init; }

    /// <summary>End profile auto-draw uses for inferred double-track exits.</summary>
    public Guid? DefaultDoubleTrackEndProfileId { get; init; }

    public DocumentReference? Source { get; init; }

    /// <summary>The Module Registry id of the standard, if known.</summary>
    public int? RegistryId { get; init; }

    public override string ToString() => ShortName;
}
