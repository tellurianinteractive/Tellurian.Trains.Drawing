using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Editing.Snapping;

/// <summary>Something a point can snap to; <see cref="Source"/> tells the editor what it belongs to.</summary>
public abstract record SnapTarget(object? Source)
{
    public abstract BoundingBox Bounds { get; }

    /// <summary>The closest point of the target to <paramref name="point"/>.</summary>
    public abstract Point ClosestTo(Point point);

    public abstract SnapKind Kind { get; }
}

/// <summary>A significant point, e.g. a corner.</summary>
public sealed record PointTarget(Point Point, object? Source = null) : SnapTarget(Source)
{
    public override BoundingBox Bounds => new(Point, Point);
    public override Point ClosestTo(Point point) => Point;
    public override SnapKind Kind => SnapKind.Point;
}

/// <summary>A straight edge between two points.</summary>
public sealed record EdgeTarget(Point Start, Point End, object? Source = null) : SnapTarget(Source)
{
    public override BoundingBox Bounds => BoundingBox.FromPoints([Start, End]);
    public override Point ClosestTo(Point point) => Measure.ClosestPointOnSegment(point, Start, End);
    public override SnapKind Kind => SnapKind.Edge;
}

/// <summary>A connection point with an outward heading, e.g. a module exit.</summary>
public sealed record ConnectionTarget(Pose Pose, object? Source = null) : SnapTarget(Source)
{
    public override BoundingBox Bounds => new(Pose.Position, Pose.Position);
    public override Point ClosestTo(Point point) => Pose.Position;
    public override SnapKind Kind => SnapKind.Connection;
}
