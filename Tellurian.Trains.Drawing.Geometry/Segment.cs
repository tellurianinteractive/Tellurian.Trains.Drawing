namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>
/// A part of a path: a straight line or a circular arc. Segments are stored exactly;
/// tessellation is only used by algorithms that need polylines.
/// </summary>
public abstract record Segment
{
    public abstract Point2 Start { get; }
    public abstract Point2 End { get; }
    public abstract double Length { get; }

    /// <summary>Direction of travel at the start.</summary>
    public abstract Angle StartHeading { get; }

    /// <summary>Direction of travel at the end.</summary>
    public abstract Angle EndHeading { get; }

    public abstract BoundingBox Bounds { get; }

    public Pose2 StartPose => new(Start, StartHeading);
    public Pose2 EndPose => new(End, EndHeading);

    /// <summary>The point at <paramref name="distance"/> from the start, clamped to the segment.</summary>
    public abstract Point2 PointAt(double distance);

    /// <summary>The direction of travel at <paramref name="distance"/> from the start, clamped to the segment.</summary>
    public abstract Angle HeadingAt(double distance);

    public Pose2 PoseAt(double distance) => new(PointAt(distance), HeadingAt(distance));

    /// <summary>The same segment, travelled in the opposite direction.</summary>
    public abstract Segment Reversed();

    public abstract Segment Transformed(Transform2 transform);

    /// <summary>The segment mirrored about the Y axis. Only for comparing end profiles.</summary>
    public abstract Segment MirroredX();

    /// <summary>
    /// A parallel segment at <paramref name="distance"/> to the left of the direction of travel
    /// (negative distance: to the right). Arcs keep their centre and change their radius.
    /// </summary>
    public abstract Segment Offset(double distance);

    /// <summary>
    /// Appends points along the segment to <paramref name="points"/>, excluding the start point.
    /// The deviation from the true geometry is at most <paramref name="chordTolerance"/>.
    /// </summary>
    public abstract void AppendTessellation(List<Point2> points, double chordTolerance);

    protected double Clamp(double distance) => Math.Clamp(distance, 0, Length);
}
