namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>
/// Builds a path step by step from a start pose, e.g. a module main path:
/// <c>new PathBuilder(Pose.Origin).Straight(500).Curve(2000, 30).Build()</c>.
/// </summary>
public sealed class PathBuilder(Pose start)
{
    private readonly List<Segment> _segments = [];

    public Pose Current { get; private set; } = start;

    public PathBuilder Straight(double length)
    {
        if (length <= Tolerance.Length) return this;
        return Add(LineSegment.FromStart(Current, length));
    }

    /// <summary>A tangent arc turning <paramref name="turnDegrees"/> (positive left, negative right).</summary>
    public PathBuilder Curve(double radius, double turnDegrees)
    {
        if (Math.Abs(turnDegrees) <= Tolerance.Angle) return this;
        return Add(ArcSegment.FromStart(Current, radius, turnDegrees));
    }

    /// <summary>A straight line to <paramref name="point"/>; the heading becomes the line's direction.</summary>
    public PathBuilder LineTo(Point point) => Add(new LineSegment(Current.Position, point));

    public PathBuilder Add(Segment segment)
    {
        _segments.Add(segment);
        Current = segment.EndPose;
        return this;
    }

    public TrackPath Build(bool isClosed = false) => new(_segments, isClosed);
}
