namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>
/// An angle in degrees, counter-clockwise from the positive X axis. The value is not normalised;
/// use <see cref="Normalized"/> or <see cref="Signed"/> when a canonical form is needed.
/// </summary>
public readonly record struct Angle(double Degrees)
{
    public static Angle Zero => new(0);
    public static Angle Half => new(180);

    public double Radians => Degrees * Math.PI / 180.0;

    /// <summary>The angle in the range [0, 360).</summary>
    public Angle Normalized
    {
        get
        {
            var d = Degrees % 360.0;
            if (d < 0) d += 360.0;
            if (d >= 360.0 - Tolerance.Angle) d = 0;
            return new(d);
        }
    }

    /// <summary>The angle in the range (-180, 180].</summary>
    public Angle Signed
    {
        get
        {
            var d = Normalized.Degrees;
            return new(d > 180.0 ? d - 360.0 : d);
        }
    }

    public static Angle FromRadians(double radians) => new(radians * 180.0 / Math.PI);

    /// <summary>True if the two angles point in the same direction, within <paramref name="tolerance"/> degrees.</summary>
    public bool IsCloseTo(Angle other, double tolerance = Tolerance.Angle) =>
        Math.Abs((this - other).Signed.Degrees) <= tolerance;

    public static Angle operator +(Angle a, Angle b) => new(a.Degrees + b.Degrees);
    public static Angle operator -(Angle a, Angle b) => new(a.Degrees - b.Degrees);
    public static Angle operator -(Angle a) => new(-a.Degrees);
    public static Angle operator *(Angle a, double factor) => new(a.Degrees * factor);

    public override string ToString() => $"{Degrees:0.###}°";
}
