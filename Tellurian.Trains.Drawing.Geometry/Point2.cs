namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>A 2D point in millimetres.</summary>
public readonly record struct Point2(double X, double Y)
{
    public static Point2 Origin => new(0, 0);

    public Vector2 AsVector => new(X, Y);

    public double DistanceTo(Point2 other) => (other - this).Length;

    public bool IsCloseTo(Point2 other, double tolerance = Tolerance.Length) => DistanceTo(other) <= tolerance;

    /// <summary>
    /// The point mirrored about the Y axis (X → −X). Used only for comparing end profiles,
    /// where two facing ends are each seen from outside their own module. Modules are never mirrored.
    /// </summary>
    public Point2 MirroredX() => new(-X, Y);

    public static Point2 Lerp(Point2 a, Point2 b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    public static Vector2 operator -(Point2 a, Point2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Point2 operator +(Point2 p, Vector2 v) => new(p.X + v.X, p.Y + v.Y);
    public static Point2 operator -(Point2 p, Vector2 v) => new(p.X - v.X, p.Y - v.Y);

    public override string ToString() => $"({X:0.###}, {Y:0.###})";
}
