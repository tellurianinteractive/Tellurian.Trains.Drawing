namespace Tellurian.Trains.Drawing.Model.EndProfiles;

/// <summary>What differs between two end profiles at a joint, or between a profile and its mirror image.</summary>
[Flags]
public enum FitDifferences
{
    None = 0,
    TrackCount = 1 << 0,
    TrackPositions = 1 << 1,
    Gauge = 1 << 2,
    RailSystem = 1 << 3,
    ScrewHoles = 1 << 4,

    /// <summary>Neither drawn screw holes nor pattern identifiers are available on both ends; assumed bolted.</summary>
    ScrewHolesUnknown = 1 << 5,

    GroundLine = 1 << 6,

    /// <summary>At least one end has no ground line, so it wasn't compared.</summary>
    GroundLineUnknown = 1 << 7,

    /// <summary>The end-plate outlines differ. Informational: plate size isn't a fit criterion.</summary>
    Outline = 1 << 8,

    Dowels = 1 << 9,

    /// <summary>Any difference in the track geometry (spec §3.2 rule 1).</summary>
    Tracks = TrackCount | TrackPositions | Gauge | RailSystem,
}
