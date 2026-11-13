using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Tracks;

/// <summary>A straight track from <see cref="Start"/>, in its heading.</summary>
public sealed record StraightTrack : TrackElement
{
    public required Pose2 Start { get; init; }
    public required double Length { get; init; }

    public override IReadOnlyList<Path2> Routes() => [new PathBuilder(Start).Straight(Length).Build()];

    public override IReadOnlyList<Pose2> ConnectionPoints() => [Start.Reversed(), Start.Advanced(Length)];
}
