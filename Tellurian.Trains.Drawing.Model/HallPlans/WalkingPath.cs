namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>
/// A path for train drivers to walk along while following their trains (HP-18). It may pass under
/// duck-under areas, but not through other modules (VA-13).
/// </summary>
public sealed record WalkingPath
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>An open path in hall coordinates.</summary>
    public required PathDrawing CentreLine { get; init; }

    /// <summary>The width, mm; null uses <see cref="HallPlanSettings.WalkingPathWidth"/>.</summary>
    public double? Width { get; init; }

    public string? Label { get; init; }

    public double EffectiveWidth(HallPlanSettings settings) => Width ?? settings.WalkingPathWidth;
}
