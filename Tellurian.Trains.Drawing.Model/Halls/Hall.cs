using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Halls;

/// <summary>
/// A room where a hall plan is built (HA-1..HA-5), in hall coordinates: millimetres, X to the right, Y up.
/// Halls are stored in their own files and reused by hall plans, which keep an embedded copy.
/// </summary>
public sealed record Hall
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Address { get; init; }
    public string? Note { get; init; }

    /// <summary>The walls as a closed path; wall <c>i</c> is step <c>i</c>.</summary>
    public required PathDrawing Outline { get; init; }

    /// <summary>The minimum aisle width, mm; null uses the minimum clearance of the hall plan's standard (HA-3).</summary>
    public double? MinimumAisleWidth { get; init; }

    public IReadOnlyList<Obstacle> Obstacles { get; init; } = [];
    public IReadOnlyList<Door> Doors { get; init; } = [];
    public IReadOnlyList<NoGoZone> NoGoZones { get; init; } = [];

    /// <summary>Incremented on every save.</summary>
    public int Revision { get; init; }

    /// <summary>The number of walls.</summary>
    [JsonIgnore]
    public int WallCount => Outline.Steps.Count;

    /// <summary>A rectangular hall of <paramref name="width"/> × <paramref name="depth"/> with a corner at the origin (HA-1).</summary>
    public static Hall Rectangle(string name, double width, double depth, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = name,
        Outline = PathDrawing.Rectangle(width, depth, Transform.Identity),
    };

    /// <summary>The geometry of wall <paramref name="index"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">There is no such wall.</exception>
    public Segment Wall(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, WallCount);
        return Outline.ToPath().Segments[index];
    }

    public Obstacle? FindObstacle(Guid id) => Obstacles.FirstOrDefault(o => o.Id == id);

    public override string ToString() => Name;
}
