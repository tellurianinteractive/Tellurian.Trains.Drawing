using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.EndProfiles;

/// <summary>
/// The shape of an end plate (EP-3, EP-4): its upper edge, where the track is, as points from left to right,
/// and its bottom. The side edges follow the first and last points, so the plate is as wide as the upper edge.
/// The upper edge is also the profile's ground line, the landscape surface compared at joints.
/// </summary>
public sealed record PlateShape
{
    /// <summary>Arcs of existing outlines are turned into points this close to the arc, mm.</summary>
    public const double ArcTolerance = 0.5;

    public PlateShape(IReadOnlyList<Point> upperEdge, double bottom)
    {
        ArgumentNullException.ThrowIfNull(upperEdge);
        if (upperEdge.Count < 2) throw new ArgumentException("The upper edge needs at least two points.", nameof(upperEdge));
        if (upperEdge[^1].X - upperEdge[0].X < Tolerance.Length) throw new ArgumentException("The upper edge must go from left to right.", nameof(upperEdge));
        if (upperEdge.Any(p => p.Y - bottom < Tolerance.Length)) throw new ArgumentException("The bottom must be below every point of the upper edge.", nameof(bottom));
        UpperEdge = upperEdge;
        Bottom = bottom;
    }

    /// <summary>The upper edge from left to right, in end-view coordinates (rail top at Y = 0).</summary>
    public IReadOnlyList<Point> UpperEdge { get; }

    /// <summary>The Y of the plate's bottom edge; negative, below rail top.</summary>
    public double Bottom { get; }

    public double Left => UpperEdge[0].X;
    public double Right => UpperEdge[^1].X;
    public double Width => Right - Left;
    public double LeftHeight => UpperEdge[0].Y - Bottom;
    public double RightHeight => UpperEdge[^1].Y - Bottom;

    /// <summary>The closed outline: the bottom edge, the right side, the upper edge right to left, and the left side.</summary>
    public PathDrawing Outline() =>
        PathDrawing.Polyline([new(Left, Bottom), new(Right, Bottom), .. UpperEdge.Reverse()], isClosed: true);

    /// <summary>The ground line: the upper edge.</summary>
    public PathDrawing GroundLine() => PathDrawing.Polyline(UpperEdge);

    /// <summary>A shape with valid points: the upper edge sorted from left to right and the bottom below it; null if impossible.</summary>
    public static PlateShape? TryCreate(IEnumerable<Point> upperEdge, double bottom)
    {
        var points = upperEdge.ToList();
        if (points.Count < 2 || points[^1].X - points[0].X < Tolerance.Length || points.Any(p => p.Y - bottom < Tolerance.Length)) return null;
        return new(points, bottom);
    }

    /// <summary>
    /// The plate shape of a profile's outline: the lowest edge is the bottom, and the outline between its two
    /// corners is the upper edge. Arcs are turned into points. Null if the profile has no outline.
    /// </summary>
    public static PlateShape? From(EndProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.Outline is not { } outline) return null;
        var points = Distinct(outline.Tessellate(ArcTolerance).Points);
        if (points.Count < 3) return null;
        var bottom = points.Min(p => p.Y);
        var onBottom = points.Select((p, i) => (p, i)).Where(x => Math.Abs(x.p.Y - bottom) <= Tolerance.Length).ToList();
        var leftCorner = onBottom.MinBy(x => x.p.X).i;
        var rightCorner = onBottom.MaxBy(x => x.p.X).i;
        if (leftCorner == rightCorner) return null;

        // Two ways round from the right corner to the left one; the upper edge is the way that isn't the bottom edge.
        var forward = Walk(points, rightCorner, leftCorner, 1);
        var backward = Walk(points, rightCorner, leftCorner, -1);
        var upper = forward.Skip(1).SkipLast(1).All(p => Math.Abs(p.Y - bottom) <= Tolerance.Length) ? backward : forward;
        // The walk includes the bottom corners; the upper edge starts above them, at the tops of the side edges.
        var edge = upper.Skip(1).SkipLast(1).Reverse().ToList();
        return TryCreate(edge, bottom);
    }

    private static List<Point> Walk(IReadOnlyList<Point> points, int from, int to, int step)
    {
        var result = new List<Point>();
        for (var i = from; ; i = (i + step + points.Count) % points.Count)
        {
            result.Add(points[i]);
            if (i == to) return result;
        }
    }

    /// <summary>The closed polyline's points without the repeated start and consecutive duplicates.</summary>
    private static List<Point> Distinct(IReadOnlyList<Point> points)
    {
        var result = new List<Point>();
        foreach (var point in points)
            if (result.Count == 0 || !result[^1].IsCloseTo(point)) result.Add(point);
        if (result.Count > 1 && result[0].IsCloseTo(result[^1])) result.RemoveAt(result.Count - 1);
        return result;
    }
}
