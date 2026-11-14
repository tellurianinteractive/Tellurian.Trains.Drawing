using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>
/// The properties the planner has set for the joint between two placed exits (HP-11). The fit and the
/// rule-based join method are evaluated from the end profiles, not stored; only the planner's choices are.
/// </summary>
public sealed record Joint
{
    public const int DefaultNumberOfClamps = 2;

    public required ExitReference First { get; init; }
    public required ExitReference Second { get; init; }

    /// <summary>The join method chosen against the rules, e.g. clamps when the screw-hole data is missing.</summary>
    public JoinMethod? JoinMethodOverride { get; init; }

    /// <summary>The number of clamps of a clamped joint.</summary>
    public int NumberOfClamps { get; init; } = DefaultNumberOfClamps;

    public string? Note { get; init; }

    /// <summary>True if this joint connects <paramref name="a"/> and <paramref name="b"/>, in either order.</summary>
    public bool Connects(ExitReference a, ExitReference b) =>
        (First == a && Second == b) || (First == b && Second == a);
}
