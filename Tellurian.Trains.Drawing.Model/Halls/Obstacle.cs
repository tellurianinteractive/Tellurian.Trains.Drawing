using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Halls;

/// <summary>A pillar or fixed furniture in a hall (HA-2). Modules may not overlap it (VA-6).</summary>
public sealed record Obstacle
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ObstacleKind Kind { get; init; }

    /// <summary>A closed path in hall coordinates.</summary>
    public required PathDrawing Area { get; init; }

    public string? Label { get; init; }

    public static Obstacle RoundPillar(Point center, double diameter) =>
        new() { Kind = ObstacleKind.Pillar, Area = PathDrawing.Circle(center, diameter) };

    public static Obstacle RectangularPillar(Point center, double width, double depth, Angle rotation = default) =>
        new() { Kind = ObstacleKind.Pillar, Area = PathDrawing.Rectangle(center, width, depth, rotation) };

    public override string? ToString() => Label ?? Kind.ToString();
}
