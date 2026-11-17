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

    /// <summary>The centre and diameter if the area is a circle, as made by <see cref="RoundPillar"/>.</summary>
    public bool TryGetCircle(out Point center, out double diameter)
    {
        center = default;
        diameter = 0;
        if (!Area.IsClosed || Area.Steps.Count == 0 || Area.Steps.Any(s => s is not ArcStep a || !a.Center.IsCloseTo(((ArcStep)Area.Steps[0]).Center))) return false;
        if (Math.Abs(Math.Abs(Area.Steps.Sum(s => ((ArcStep)s).Sweep)) - 360) > Tolerance.Angle) return false;
        center = ((ArcStep)Area.Steps[0]).Center;
        diameter = 2 * Area.Start.DistanceTo(center);
        return true;
    }

    /// <summary>
    /// The centre, size and rotation if the area is a rectangle, as made by <see cref="RectangularPillar"/>: the
    /// width runs along the first side, turned <paramref name="rotation"/> from the X axis.
    /// </summary>
    public bool TryGetRectangle(out Point center, out double width, out double depth, out Angle rotation)
    {
        if (Area.IsClosed && Area.Steps.Count == 4) return PathCorners.From(Area).TryGetRectangle(out center, out width, out depth, out rotation);
        center = default;
        width = depth = 0;
        rotation = default;
        return false;
    }

    public override string? ToString() => Label ?? Kind.ToString();
}
