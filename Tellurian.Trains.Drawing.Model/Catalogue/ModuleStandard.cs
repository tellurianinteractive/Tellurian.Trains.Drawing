using System.Text.Json;
using Shared = Tellurian.Trains.Schedules.Model.Layouts;

namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>
/// A standard that modules follow, e.g. FREMO H0-RE, MMM-2R or MMM-3R
/// (Module Registry <c>ModuleStandard</c>, with the drawing app's additions).
/// </summary>
public sealed record ModuleStandard : ExtensibleRecord
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

    /// <summary>
    /// The centre distance at which trains on two tracks pass each other freely, mm. A station track ends there, at the
    /// fouling point, not at the points of a turnout (<see cref="Tracks.StationTrackMeasurement"/>). Null uses the track
    /// distance of the default double-track end profile.
    /// </summary>
    public double? FoulingDistance { get; init; }

    /// <summary>Minimum radius of reverse curves that auto-draw generates, mm.</summary>
    public double? MinimumReverseCurveRadius { get; init; }

    /// <summary>Curve radius auto-draw uses when a module has an angle but no radius, mm.</summary>
    public double? DefaultCurveRadius { get; init; }

    /// <summary>End profile auto-draw uses for inferred single-track exits.</summary>
    public Guid? DefaultSingleTrackEndProfileId { get; init; }

    /// <summary>End profile auto-draw uses for inferred double-track exits.</summary>
    public Guid? DefaultDoubleTrackEndProfileId { get; init; }

    public DocumentReference? Source { get; init; }

    /// <summary>What other systems keep about the standard, with one entry per system; read and written back unchanged (<see cref="Files.ExternalReferences"/>).</summary>
    public IReadOnlyDictionary<string, JsonElement>? ExternalReferences { get; init; }

    /// <summary>
    /// The countries the standard applies to, as ISO 3166 two-letter codes (e.g. <c>SE</c> for the MMM standards);
    /// empty when it applies everywhere, like the FREMO standards.
    /// </summary>
    public IReadOnlyList<string> Countries { get; init; } = [];

    /// <summary>True if the standard applies in <paramref name="country"/> (a two-letter code, or null when not chosen).</summary>
    public bool AppliesTo(string? country) =>
        Countries.Count == 0 || (country is not null && Countries.Any(c => string.Equals(c, country, StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    /// The standard as the shared layout model names it in a module: by its short name, with its scale by short name.
    /// The ids of the shared model are the Module Registry's and are not set here.
    /// </summary>
    public Shared.ModuleStandard ToShared() => new() { ShortName = ShortName, Scale = new() { ShortName = Scale } };

    public override string ToString() => ShortName;
}
