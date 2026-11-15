using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>
/// All modules of a hall plan that are joined to each other: a main line with its branches and rejoining
/// lines (HP-5). The poses of all modules are computed from the anchor module's pose.
/// </summary>
public sealed record Layout
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string? Name { get; init; }

    /// <summary>The module that the layout is positioned by; by default the first module added.</summary>
    public required Guid AnchorModuleId { get; init; }

    /// <summary>The anchor module's local origin and X axis in hall coordinates.</summary>
    public Pose AnchorPose { get; init; } = Pose.Origin;

    /// <summary>A locked anchor can't be moved or rotated (HP-5).</summary>
    public bool IsAnchorLocked { get; init; }

    /// <summary>How the people setting up find the anchor's position (HP-21).</summary>
    public AnchorDimensions? AnchorDimensions { get; init; }

    /// <summary>The first line is the main line; the others are branches from a module in another line.</summary>
    public IReadOnlyList<Line> Lines { get; init; } = [];

    public IEnumerable<LineEntry> Entries() => Lines.SelectMany(l => l.Entries);

    public override string? ToString() => Name;
}
