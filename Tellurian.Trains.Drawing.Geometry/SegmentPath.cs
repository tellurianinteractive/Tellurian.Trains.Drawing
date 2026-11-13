namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>
/// A connected sequence of segments: each segment starts where the previous one ends.
/// A closed path also ends where it starts (outlines, hall walls); an open path doesn't
/// (main paths, track centre lines, ground lines).
/// </summary>
public sealed class SegmentPath
{
    public SegmentPath(IEnumerable<Segment> segments, bool isClosed = false)
    {
        Segments = [.. segments];
        if (Segments.Count == 0) throw new ArgumentException("A path must have at least one segment.", nameof(segments));
        for (var i = 1; i < Segments.Count; i++)
        {
            if (!Segments[i - 1].End.IsCloseTo(Segments[i].Start, Tolerance.Length))
                throw new ArgumentException($"Segment {i} doesn't start where segment {i - 1} ends.", nameof(segments));
        }
        if (isClosed && !End.IsCloseTo(Start, Tolerance.Length))
            throw new ArgumentException("A closed path must end where it starts.", nameof(isClosed));
        IsClosed = isClosed;
    }

    public IReadOnlyList<Segment> Segments { get; }
    public bool IsClosed { get; }
    public Point Start => Segments[0].Start;
    public Point End => Segments[^1].End;
    public Pose StartPose => Segments[0].StartPose;
    public Pose EndPose => Segments[^1].EndPose;
    public double Length => Segments.Sum(s => s.Length);
    public BoundingBox Bounds => Segments.Aggregate(BoundingBox.Empty, (box, s) => box.Union(s.Bounds));

    /// <summary>True if the direction of travel doesn't jump at any joint between segments.</summary>
    public bool IsTangentContinuous(double angleTolerance = Tolerance.Angle)
    {
        for (var i = 1; i < Segments.Count; i++)
        {
            if (!Segments[i - 1].EndHeading.IsCloseTo(Segments[i].StartHeading, angleTolerance)) return false;
        }
        return true;
    }

    /// <summary>The pose at <paramref name="distance"/> from the start, clamped to the path.</summary>
    public Pose PoseAt(double distance)
    {
        var remaining = Math.Max(0, distance);
        foreach (var segment in Segments)
        {
            if (remaining <= segment.Length) return segment.PoseAt(remaining);
            remaining -= segment.Length;
        }
        return EndPose;
    }

    public SegmentPath Reversed() => new(Segments.Reverse().Select(s => s.Reversed()), IsClosed);

    public SegmentPath Transformed(Transform transform) => new(Segments.Select(s => s.Transformed(transform)), IsClosed);

    /// <summary>The path mirrored about the Y axis. Only for comparing end profiles.</summary>
    public SegmentPath MirroredX() => new(Segments.Select(s => s.MirroredX()), IsClosed);

    /// <summary>
    /// A parallel path at <paramref name="distance"/> to the left (negative: right).
    /// Exact for tangent-continuous paths, such as module main paths and track centre lines.
    /// </summary>
    public SegmentPath Offset(double distance)
    {
        if (!IsTangentContinuous())
            throw new InvalidOperationException("Only tangent-continuous paths can be offset exactly.");
        return new(Segments.Select(s => s.Offset(distance)), IsClosed);
    }

    public Polyline Tessellate(double chordTolerance = Tolerance.Chord)
    {
        var points = new List<Point> { Start };
        foreach (var segment in Segments) segment.AppendTessellation(points, chordTolerance);
        if (IsClosed) points.RemoveAt(points.Count - 1);
        return new(points, IsClosed);
    }
}
