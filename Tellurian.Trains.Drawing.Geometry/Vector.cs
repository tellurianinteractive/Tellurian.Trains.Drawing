namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>A 2D vector in millimetres.</summary>
public readonly record struct Vector(double X, double Y)
{
    public static Vector Zero => new(0, 0);

    public double Length => Math.Sqrt(X * X + Y * Y);
    public double LengthSquared => X * X + Y * Y;

    /// <summary>The direction of the vector. Undefined (zero) for the zero vector.</summary>
    public Angle Direction => Angle.FromRadians(Math.Atan2(Y, X));

    /// <summary>The vector rotated 90° counter-clockwise (to the left).</summary>
    public Vector LeftNormal => new(-Y, X);

    public Vector Normalized
    {
        get
        {
            var length = Length;
            return length < Tolerance.Length ? Zero : new(X / length, Y / length);
        }
    }

    public static Vector FromAngle(Angle angle, double length = 1.0) =>
        new(Math.Cos(angle.Radians) * length, Math.Sin(angle.Radians) * length);

    public double Dot(Vector other) => X * other.X + Y * other.Y;

    /// <summary>The z component of the 3D cross product; positive when <paramref name="other"/> is to the left.</summary>
    public double Cross(Vector other) => X * other.Y - Y * other.X;

    public Vector Rotated(Angle angle)
    {
        var (sin, cos) = Math.SinCos(angle.Radians);
        return new(X * cos - Y * sin, X * sin + Y * cos);
    }

    public bool IsCloseTo(Vector other, double tolerance = Tolerance.Length) => (this - other).Length <= tolerance;

    public static Vector operator +(Vector a, Vector b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector operator -(Vector a, Vector b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector operator -(Vector a) => new(-a.X, -a.Y);
    public static Vector operator *(Vector a, double factor) => new(a.X * factor, a.Y * factor);
    public static Vector operator *(double factor, Vector a) => new(a.X * factor, a.Y * factor);
    public static Vector operator /(Vector a, double divisor) => new(a.X / divisor, a.Y / divisor);

    public override string ToString() => $"({X:0.###}, {Y:0.###})";
}
