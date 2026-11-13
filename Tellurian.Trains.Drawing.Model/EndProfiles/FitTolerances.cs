namespace Tellurian.Trains.Drawing.Model.EndProfiles;

/// <summary>Tolerances for comparing end profiles (spec §3.2).</summary>
public sealed record FitTolerances
{
    public static FitTolerances Default { get; } = new();

    /// <summary>Track positions, gauges and screw holes, mm.</summary>
    public double Joint { get; init; } = 0.5;

    /// <summary>Ground line, mm.</summary>
    public double Landscape { get; init; } = 2.0;
}
