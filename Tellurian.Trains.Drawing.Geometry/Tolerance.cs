namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>
/// Numeric tolerances. All lengths are in millimetres and all angles in degrees.
/// </summary>
public static class Tolerance
{
    /// <summary>Two lengths or coordinates closer than this are equal.</summary>
    public const double Length = 1e-6;

    /// <summary>Two angles (degrees) closer than this are equal.</summary>
    public const double Angle = 1e-9;

    /// <summary>Two unit vectors whose cross product (the sine of the angle between them) is smaller than this are parallel.</summary>
    public const double Collinear = 1e-12;

    /// <summary>Default maximum distance between an arc and its tessellated chords.</summary>
    public const double Chord = 0.05;
}
