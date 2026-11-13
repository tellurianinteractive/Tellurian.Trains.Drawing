namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>A straight segment from <see cref="Start"/> to <see cref="End"/>.</summary>
public sealed record LineSegment : Segment
{
    private readonly Point _start;
    private readonly Point _end;

    public LineSegment(Point start, Point end)
    {
        if (start.IsCloseTo(end)) throw new ArgumentException("A line segment must have a length.", nameof(end));
        _start = start;
        _end = end;
    }

    public override Point Start => _start;
    public override Point End => _end;
    public override double Length => _start.DistanceTo(_end);
    public override Angle StartHeading => (_end - _start).Direction;
    public override Angle EndHeading => StartHeading;
    public override BoundingBox Bounds => BoundingBox.Empty.Including(_start).Including(_end);

    /// <summary>A line of <paramref name="length"/> starting at <paramref name="start"/> in its heading.</summary>
    public static LineSegment FromStart(Pose start, double length) => new(start.Position, start.Advanced(length).Position);

    public override Point PointAt(double distance) => Point.Lerp(_start, _end, Clamp(distance) / Length);

    public override Angle HeadingAt(double distance) => StartHeading;

    public override Segment Reversed() => new LineSegment(_end, _start);

    public override Segment Transformed(Transform transform) => new LineSegment(transform.Apply(_start), transform.Apply(_end));

    public override Segment MirroredX() => new LineSegment(_start.MirroredX(), _end.MirroredX());

    public override Segment Offset(double distance)
    {
        var shift = (_end - _start).Normalized.LeftNormal * distance;
        return new LineSegment(_start + shift, _end + shift);
    }

    public override void AppendTessellation(List<Point> points, double chordTolerance) => points.Add(_end);
}
