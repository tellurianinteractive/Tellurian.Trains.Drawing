namespace Tellurian.Trains.Drawing.Model.EndProfiles;

/// <summary>
/// How a track is drawn in end view (EP-5): rails on a sleeper, standing on top of the end plate.
/// Rail top is at Y = 0, so the sleeper's underside, where the plate's top edge goes, is at −<see cref="Height"/>.
/// </summary>
public static class TrackCrossSection
{
    /// <summary>The height of a rail, mm.</summary>
    public const double RailHeight = 2;

    /// <summary>The height of a sleeper, mm.</summary>
    public const double SleeperHeight = 2;

    /// <summary>The height from the sleeper's underside to rail top, mm.</summary>
    public const double Height = RailHeight + SleeperHeight;

    /// <summary>The width of a rail head relative to the gauge.</summary>
    public const double RailWidthPerGauge = 0.1;

    /// <summary>The sleeper length relative to the gauge; about 30 mm in H0, as a 2.6 m sleeper at 1:87.</summary>
    public const double SleeperLengthPerGauge = 1.8;

    public static double RailWidth(double gauge) => gauge * RailWidthPerGauge;

    public static double SleeperLength(double gauge) => gauge * SleeperLengthPerGauge;
}
