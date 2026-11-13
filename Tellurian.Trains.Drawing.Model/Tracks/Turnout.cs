using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Tracks;

/// <summary>
/// A turnout with its points at <see cref="Start"/>, heading into the turnout.
/// A diverging route is an arc of <see cref="Radius"/> turning <see cref="Angle"/>, followed by a
/// straight that ends level with the straight route (<see cref="Length"/>), if there is room for one.
/// A curved turnout has an outer route (<see cref="Radius"/>, <see cref="Angle"/>) and an inner
/// route (<see cref="InnerRadius"/>, <see cref="InnerAngle"/>).
/// </summary>
public sealed record Turnout : TrackElement
{
    public required Pose2 Start { get; init; }
    public required TurnoutKind Kind { get; init; }

    /// <summary>Length of the straight route, mm.</summary>
    public required double Length { get; init; }

    /// <summary>Radius of the diverging route, or of the outer route of a curved turnout, mm.</summary>
    public required double Radius { get; init; }

    /// <summary>
    /// Angle of the diverging route (degrees, positive). For a wye, the angle between its two routes;
    /// for a curved turnout, the angle of the outer route.
    /// </summary>
    public required double Angle { get; init; }

    public double? InnerRadius { get; init; }
    public double? InnerAngle { get; init; }

    public override IReadOnlyList<Path2> Routes() => Kind switch
    {
        TurnoutKind.Left => [Straight(), Diverging(Angle)],
        TurnoutKind.Right => [Straight(), Diverging(-Angle)],
        TurnoutKind.Wye => [Diverging(Angle / 2), Diverging(-Angle / 2)],
        TurnoutKind.ThreeWay => [Straight(), Diverging(Angle), Diverging(-Angle)],
        TurnoutKind.CurvedLeft => [Curve(Radius, Angle), Curve(Inner(nameof(InnerRadius), InnerRadius), Inner(nameof(InnerAngle), InnerAngle))],
        TurnoutKind.CurvedRight => [Curve(Radius, -Angle), Curve(Inner(nameof(InnerRadius), InnerRadius), -Inner(nameof(InnerAngle), InnerAngle))],
        _ => throw new NotSupportedException($"Unknown turnout kind {Kind}."),
    };

    public override IReadOnlyList<Pose2> ConnectionPoints() => [Start.Reversed(), .. Routes().Select(r => r.EndPose)];

    private Path2 Straight() => new PathBuilder(Start).Straight(Length).Build();

    private Path2 Curve(double radius, double angle) => new PathBuilder(Start).Curve(radius, angle).Build();

    private Path2 Diverging(double angle)
    {
        var builder = new PathBuilder(Start).Curve(Radius, angle);
        var radians = Math.Abs(angle) * Math.PI / 180;
        var extension = (Length - Radius * Math.Sin(radians)) / Math.Cos(radians);
        return builder.Straight(Math.Max(0, extension)).Build();
    }

    private static double Inner(string name, double? value) =>
        value ?? throw new InvalidOperationException($"A curved turnout needs {name}.");
}
