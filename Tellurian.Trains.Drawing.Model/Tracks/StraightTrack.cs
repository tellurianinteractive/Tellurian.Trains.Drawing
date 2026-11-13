using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Tracks;

/// <summary>A straight track from <see cref="Start"/>, in its heading.</summary>
public sealed record StraightTrack : TrackElement
{
    public required Pose Start { get; init; }
    public required double Length { get; init; }

    public override IReadOnlyList<SegmentPath> Routes() => [new PathBuilder(Start).Straight(Length).Build()];

    public override IReadOnlyList<Pose> ConnectionPoints() => [Start.Reversed(), Start.Advanced(Length)];
}
