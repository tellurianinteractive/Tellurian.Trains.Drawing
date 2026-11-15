using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>The point on the anchor module that its dimensions are measured from (HP-21).</summary>
public sealed record AnchorReferencePoint
{
    public required ReferencePointKind Kind { get; init; }

    /// <summary>The exit, for <see cref="ReferencePointKind.ExitCentre"/> and <see cref="ReferencePointKind.PlateCorner"/>.</summary>
    public string? ExitLabel { get; init; }

    /// <summary>The corner, for <see cref="ReferencePointKind.PlateCorner"/>.</summary>
    public PlateSide? Corner { get; init; }

    /// <summary>The point in module coordinates, for <see cref="ReferencePointKind.MarkedPoint"/>.</summary>
    public Point? Point { get; init; }

    public static AnchorReferencePoint ExitCentre(string exitLabel) =>
        new() { Kind = ReferencePointKind.ExitCentre, ExitLabel = exitLabel };

    public static AnchorReferencePoint PlateCorner(string exitLabel, PlateSide corner) =>
        new() { Kind = ReferencePointKind.PlateCorner, ExitLabel = exitLabel, Corner = corner };

    public static AnchorReferencePoint Marked(Point point) =>
        new() { Kind = ReferencePointKind.MarkedPoint, Point = point };
}
