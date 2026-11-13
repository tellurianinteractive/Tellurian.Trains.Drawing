using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Tracks;

/// <summary>A curved track from <see cref="Start"/>, turning <see cref="Angle"/> degrees (positive left).</summary>
public sealed record CurvedTrack : TrackElement
{
    public required Pose2 Start { get; init; }
    public required double Radius { get; init; }
    public required double Angle { get; init; }

    public override IReadOnlyList<Path2> Routes() => [new PathBuilder(Start).Curve(Radius, Angle).Build()];

    public override IReadOnlyList<Pose2> ConnectionPoints() => [Start.Reversed(), Routes()[0].EndPose];
}
