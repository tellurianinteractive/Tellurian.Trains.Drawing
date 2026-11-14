namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>A wall (by index) or an obstacle (by id) of the plan's hall, as a reference for dimensions (HP-21).</summary>
public sealed record HallElementReference
{
    public required HallElementKind Kind { get; init; }

    /// <summary>For <see cref="HallElementKind.Wall"/>: the index of the wall in the hall outline.</summary>
    public int? WallIndex { get; init; }

    /// <summary>For <see cref="HallElementKind.Obstacle"/>.</summary>
    public Guid? ObstacleId { get; init; }

    public static HallElementReference Wall(int index) => new() { Kind = HallElementKind.Wall, WallIndex = index };

    public static HallElementReference Obstacle(Guid id) => new() { Kind = HallElementKind.Obstacle, ObstacleId = id };
}
