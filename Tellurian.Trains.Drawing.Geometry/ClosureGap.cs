namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>The misfit between two exits that should be joined; see <see cref="Join.Gap"/>.</summary>
public readonly record struct ClosureGap(double Dx, double Dy, Angle Dθ)
{
    /// <summary>The straight distance between the two exit positions.</summary>
    public double Distance => Math.Sqrt(Dx * Dx + Dy * Dy);

    public bool IsWithin(double lengthTolerance, double angleTolerance) =>
        Distance <= lengthTolerance && Math.Abs(Dθ.Signed.Degrees) <= angleTolerance;

    public override string ToString() => $"dx {Dx:0.###}, dy {Dy:0.###}, dθ {Dθ}";
}
