using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>The point on the anchor module that its dimensions are measured from (HP-21).</summary>
public sealed record AnchorReferencePoint : ExtensibleRecord
{
    public required ReferencePointKind Kind { get; init; }

    /// <summary>The exit of the anchor module, by side and number, for <see cref="ReferencePointKind.ExitCentre"/> and <see cref="ReferencePointKind.PlateCorner"/>.</summary>
    public ExitPlace? Exit { get; init; }

    /// <summary>The corner, for <see cref="ReferencePointKind.PlateCorner"/>.</summary>
    public PlateSide? Corner { get; init; }

    /// <summary>The point in module coordinates, for <see cref="ReferencePointKind.MarkedPoint"/>.</summary>
    public Point? Point { get; init; }

    public static AnchorReferencePoint ExitCentre(ExitPlace exit) =>
        new() { Kind = ReferencePointKind.ExitCentre, Exit = exit };

    public static AnchorReferencePoint PlateCorner(ExitPlace exit, PlateSide corner) =>
        new() { Kind = ReferencePointKind.PlateCorner, Exit = exit, Corner = corner };

    public static AnchorReferencePoint Marked(Point point) =>
        new() { Kind = ReferencePointKind.MarkedPoint, Point = point };
}
