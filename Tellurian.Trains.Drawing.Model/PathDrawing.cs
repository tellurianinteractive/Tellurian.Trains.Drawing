using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model;

/// <summary>
/// A drawn path of line and arc steps, as stored in files: outlines, ground lines, sides and centre lines.
/// Each step starts where the previous one ends, so the path is connected by construction.
/// A closed path's last step ends at <see cref="Start"/>.
/// </summary>
public sealed record PathDrawing
{
    public required Point Start { get; init; }
    public required IReadOnlyList<PathStep> Steps { get; init; }
    public bool IsClosed { get; init; }

    /// <summary>A polyline path through <paramref name="points"/>.</summary>
    public static PathDrawing Polyline(IEnumerable<Point> points, bool isClosed = false)
    {
        var list = points.ToList();
        if (list.Count < 2) throw new ArgumentException("A path needs at least two points.", nameof(points));
        var steps = list.Skip(1).Select(p => (PathStep)new LineStep(p)).ToList();
        if (isClosed && !list[^1].IsCloseTo(list[0])) steps.Add(new LineStep(list[0]));
        return new() { Start = list[0], Steps = steps, IsClosed = isClosed };
    }

    /// <summary>A drawing of <paramref name="path"/>.</summary>
    public static PathDrawing From(TrackPath path) => new()
    {
        Start = path.Start,
        Steps = [.. path.Segments.Select(PathStep.From)],
        IsClosed = path.IsClosed,
    };

    /// <summary>The geometry of the path. A closed path's last step is snapped to the start.</summary>
    public TrackPath ToPath()
    {
        var segments = new List<Segment>(Steps.Count);
        var current = Start;
        for (var i = 0; i < Steps.Count; i++)
        {
            var segment = Steps[i].ToSegment(current);
            if (IsClosed && i == Steps.Count - 1 && segment is LineSegment && !segment.End.Equals(Start))
            {
                segment = new LineSegment(current, Start);
            }
            segments.Add(segment);
            current = segment.End;
        }
        return new(segments, IsClosed);
    }

    public Polyline Tessellate(double chordTolerance = Tolerance.Chord) => ToPath().Tessellate(chordTolerance);

    /// <summary>The same path mirrored about the centre line; only for comparing end profiles.</summary>
    public PathDrawing MirroredX() => From(Mirror.AboutCentreLine(ToPath()));
}
