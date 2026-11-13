using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Tracks;

/// <summary>
/// A buffer stop at the end of a track. <see cref="Pose"/> is its connection point:
/// the track end, with the heading pointing from the stop back along the track.
/// </summary>
public sealed record BufferStop : TrackElement
{
    public required Pose2 Pose { get; init; }

    public override IReadOnlyList<Path2> Routes() => [];

    public override IReadOnlyList<Pose2> ConnectionPoints() => [Pose];
}
