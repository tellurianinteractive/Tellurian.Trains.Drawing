namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>
/// A sequence of points joined by straight edges. A closed polyline (polygon) has an implicit
/// edge from the last point back to the first.
/// </summary>
public sealed class Polyline
{
    public Polyline(IEnumerable<Point> points, bool isClosed)
    {
        Points = [.. points];
        if (Points.Count < 2) throw new ArgumentException("A polyline needs at least two points.", nameof(points));
        IsClosed = isClosed;
        Bounds = BoundingBox.FromPoints(Points);
    }

    public IReadOnlyList<Point> Points { get; }
    public bool IsClosed { get; }
    public BoundingBox Bounds { get; }
    public int EdgeCount => IsClosed ? Points.Count : Points.Count - 1;
    public double Length => Edges().Sum(e => e.Start.DistanceTo(e.End));

    /// <summary>The signed area of a closed polyline: positive when counter-clockwise.</summary>
    public double SignedArea
    {
        get
        {
            if (!IsClosed) return 0;
            var sum = 0.0;
            for (var i = 0; i < Points.Count; i++)
            {
                var a = Points[i];
                var b = Points[(i + 1) % Points.Count];
                sum += a.X * b.Y - b.X * a.Y;
            }
            return sum / 2;
        }
    }

    public bool IsCounterClockwise => SignedArea > 0;

    /// <summary>The centre of area of a closed polyline; the average of its points if it has no area.</summary>
    public Point Centroid
    {
        get
        {
            var area = SignedArea;
            if (!IsClosed || Math.Abs(area) < Tolerance.Length)
                return new(Points.Average(p => p.X), Points.Average(p => p.Y));
            double x = 0, y = 0;
            foreach (var (a, b) in Edges())
            {
                var cross = a.X * b.Y - b.X * a.Y;
                x += (a.X + b.X) * cross;
                y += (a.Y + b.Y) * cross;
            }
            return new(x / (6 * area), y / (6 * area));
        }
    }

    public (Point Start, Point End) Edge(int index) => (Points[index], Points[(index + 1) % Points.Count]);

    public IEnumerable<(Point Start, Point End)> Edges()
    {
        for (var i = 0; i < EdgeCount; i++) yield return Edge(i);
    }

    /// <summary>True if <paramref name="point"/> is inside or on the boundary of a closed polyline.</summary>
    public bool Contains(Point point)
    {
        if (!IsClosed || !Bounds.Inflated(Tolerance.Length).Contains(point)) return false;
        var inside = false;
        foreach (var (a, b) in Edges())
        {
            if (Measure.PointToSegment(point, a, b) <= Tolerance.Length) return true;
            if ((a.Y > point.Y) != (b.Y > point.Y))
            {
                var x = a.X + (point.Y - a.Y) * (b.X - a.X) / (b.Y - a.Y);
                if (point.X < x) inside = !inside;
            }
        }
        return inside;
    }
}
