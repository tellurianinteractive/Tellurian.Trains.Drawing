using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Tracks;

/// <summary>
/// A track along any path of lines and arcs, e.g. a generated through track with
/// concentric arcs or reverse curves.
/// </summary>
public sealed record FlexTrack : TrackElement
{
    public required PathDrawing Path { get; init; }

    public override IReadOnlyList<SegmentPath> Routes() => [Path.ToPath()];

    public override IReadOnlyList<Pose> ConnectionPoints()
    {
        var path = Path.ToPath();
        return [path.StartPose.Reversed(), path.EndPose];
    }
}
