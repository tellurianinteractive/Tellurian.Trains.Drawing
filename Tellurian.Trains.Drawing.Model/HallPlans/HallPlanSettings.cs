using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>Tolerances and defaults of a hall plan (HP-1, HP-5, HP-18, VA-3, VA-7, VA-10).</summary>
public sealed record HallPlanSettings
{
    public const double DefaultWalkingPathWidth = 600;

    /// <summary>Tolerances for comparing end profiles at joints (spec §3.2).</summary>
    public FitTolerances Fit { get; init; } = FitTolerances.Default;

    /// <summary>Largest position gap of a rejoining or closing joint that is still accepted, mm (VA-3).</summary>
    public double JointGap { get; init; } = 2;

    /// <summary>Largest angle error of a rejoining or closing joint that is still accepted, degrees (VA-3).</summary>
    public double JointAngle { get; init; } = 0.5;

    /// <summary>Largest rail-top height step at a joint, mm (VA-10).</summary>
    public double Height { get; init; } = 1;

    /// <summary>Minimum clearance between modules, and to walls and obstacles, mm; null uses the primary standard's (VA-7).</summary>
    public double? MinimumClearance { get; init; }

    /// <summary>The clearance check can be switched off (VA-7).</summary>
    public bool IsClearanceChecked { get; init; } = true;

    /// <summary>Width of walking paths that have none of their own, mm (HP-18).</summary>
    public double WalkingPathWidth { get; init; } = DefaultWalkingPathWidth;

    /// <summary>The step when rotating an anchor by dragging, degrees (HP-5).</summary>
    public double RotationStep { get; init; } = 15;
}
