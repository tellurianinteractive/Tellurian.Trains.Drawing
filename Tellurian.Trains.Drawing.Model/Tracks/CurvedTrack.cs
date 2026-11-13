using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Tracks;

/// <summary>A curved track from <see cref="Start"/>, turning <see cref="Angle"/> degrees (positive left).</summary>
public sealed record CurvedTrack : TrackElement
{
    public required Pose Start { get; init; }
    public required double Radius { get; init; }
    public required double Angle { get; init; }

    public override IReadOnlyList<SegmentPath> Routes() => [new PathBuilder(Start).Curve(Radius, Angle).Build()];

    public override IReadOnlyList<Pose> ConnectionPoints() => [Start.Reversed(), Routes()[0].EndPose];
}
