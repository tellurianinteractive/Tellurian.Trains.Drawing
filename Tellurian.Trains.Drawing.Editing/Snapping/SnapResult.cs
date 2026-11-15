using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Editing.Snapping;

/// <summary>Where a point snapped to; with <see cref="SnapKind.None"/>, the point itself.</summary>
public sealed record SnapResult(Point Point, SnapKind Kind, SnapTarget? Target = null)
{
    public bool IsSnapped => Kind != SnapKind.None;

    /// <summary>The heading of a connection target, if the point snapped to one.</summary>
    public Angle? Heading => (Target as ConnectionTarget)?.Pose.Heading;
}
