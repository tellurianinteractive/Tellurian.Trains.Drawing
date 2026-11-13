namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>A 2D vector in millimetres.</summary>
public readonly record struct Vector2(double X, double Y)
{
    public static Vector2 Zero => new(0, 0);

    public double Length => Math.Sqrt(X * X + Y * Y);
    public double LengthSquared => X * X + Y * Y;

    /// <summary>The direction of the vector. Undefined (zero) for the zero vector.</summary>
    public Angle Direction => Angle.FromRadians(Math.Atan2(Y, X));

    /// <summary>The vector rotated 90° counter-clockwise (to the left).</summary>
    public Vector2 LeftNormal => new(-Y, X);

    public Vector2 Normalized
    {
        get
        {
            var length = Length;
            return length < Tolerance.Length ? Zero : new(X / length, Y / length);
        }
    }

    public static Vector2 FromAngle(Angle angle, double length = 1.0) =>
        new(Math.Cos(angle.Radians) * length, Math.Sin(angle.Radians) * length);

    public double Dot(Vector2 other) => X * other.X + Y * other.Y;

    /// <summary>The z component of the 3D cross product; positive when <paramref name="other"/> is to the left.</summary>
    public double Cross(Vector2 other) => X * other.Y - Y * other.X;

    public Vector2 Rotated(Angle angle)
    {
        var (sin, cos) = Math.SinCos(angle.Radians);
        return new(X * cos - Y * sin, X * sin + Y * cos);
    }

    public bool IsCloseTo(Vector2 other, double tolerance = Tolerance.Length) => (this - other).Length <= tolerance;

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator -(Vector2 a) => new(-a.X, -a.Y);
    public static Vector2 operator *(Vector2 a, double factor) => new(a.X * factor, a.Y * factor);
    public static Vector2 operator *(double factor, Vector2 a) => new(a.X * factor, a.Y * factor);
    public static Vector2 operator /(Vector2 a, double divisor) => new(a.X / divisor, a.Y / divisor);

    public override string ToString() => $"({X:0.###}, {Y:0.###})";
}
