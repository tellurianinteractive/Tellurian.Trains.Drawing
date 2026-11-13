namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>A 2D point in millimetres.</summary>
public readonly record struct Point(double X, double Y)
{
    public static Point Origin => new(0, 0);

    public Vector AsVector => new(X, Y);

    public double DistanceTo(Point other) => (other - this).Length;

    public bool IsCloseTo(Point other, double tolerance = Tolerance.Length) => DistanceTo(other) <= tolerance;

    /// <summary>
    /// The point mirrored about the Y axis (X → −X). Used only for comparing end profiles,
    /// where two facing ends are each seen from outside their own module. Modules are never mirrored.
    /// </summary>
    public Point MirroredX() => new(-X, Y);

    public static Point Lerp(Point a, Point b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    public static Vector operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    public static Point operator +(Point p, Vector v) => new(p.X + v.X, p.Y + v.Y);
    public static Point operator -(Point p, Vector v) => new(p.X - v.X, p.Y - v.Y);

    public override string ToString() => $"({X:0.###}, {Y:0.###})";
}
