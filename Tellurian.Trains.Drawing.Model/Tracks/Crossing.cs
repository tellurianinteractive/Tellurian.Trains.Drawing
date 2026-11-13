using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Tracks;

/// <summary>
/// A diamond crossing: two straight tracks of <see cref="Length"/> crossing at their middle,
/// <see cref="Center"/>. The second track is turned <see cref="Angle"/> degrees from the first.
/// </summary>
public sealed record Crossing : TrackElement
{
    public required Pose Center { get; init; }
    public required double Length { get; init; }
    public required double Angle { get; init; }

    public override IReadOnlyList<TrackPath> Routes() => [Line(Center), Line(SecondCenter)];

    public override IReadOnlyList<Pose> ConnectionPoints() =>
    [
        Center.Reversed().Advanced(Length / 2), Center.Advanced(Length / 2),
        SecondCenter.Reversed().Advanced(Length / 2), SecondCenter.Advanced(Length / 2),
    ];

    internal Pose SecondCenter => Center with { Heading = Center.Heading + new Angle(Angle) };

    private TrackPath Line(Pose middle) => new PathBuilder(middle.Advanced(-Length / 2)).Straight(Length).Build();
}
