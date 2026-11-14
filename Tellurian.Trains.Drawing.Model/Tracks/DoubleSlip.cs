using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Tracks;

/// <summary>
/// A double slip: a <see cref="Crossing"/> with two slip routes that connect the tracks.
/// Each slip route is an arc tangent to both tracks at their ends; its radius follows from the
/// length and angle: (Length / 2) / tan(Angle / 2).
/// </summary>
public sealed record DoubleSlip : TrackElement
{
    public required Pose Center { get; init; }
    public required double Length { get; init; }
    public required double Angle { get; init; }

    [JsonIgnore]
    public double SlipRadius => Length / 2 / Math.Tan(Math.Abs(Angle) * Math.PI / 360);

    public override IReadOnlyList<SegmentPath> Routes()
    {
        var crossing = AsCrossing();
        var second = crossing.SecondCenter;
        return
        [
            .. crossing.Routes(),
            new PathBuilder(Center.Advanced(-Length / 2)).Curve(SlipRadius, Angle).Build(),
            new PathBuilder(second.Advanced(-Length / 2)).Curve(SlipRadius, -Angle).Build(),
        ];
    }

    public override IReadOnlyList<Pose> ConnectionPoints() => AsCrossing().ConnectionPoints();

    private Crossing AsCrossing() => new() { Center = Center, Length = Length, Angle = Angle };
}
