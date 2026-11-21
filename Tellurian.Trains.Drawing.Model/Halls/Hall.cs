using System.Text.Json;
using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Halls;

/// <summary>
/// A room where a hall plan is built (HA-1..HA-5), in hall coordinates: millimetres, X to the right, Y up.
/// Halls are stored in their own files and reused by hall plans, which keep an embedded copy.
/// </summary>
public sealed record Hall : ExtensibleRecord
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

    /// <summary>Who saved the drawing, and the last date each of them did, newest first (<see cref="Files.DrawingRevisionLog"/>).</summary>
    public IReadOnlyList<Files.DrawingRevision> RevisionLog { get; init; } = [];

    /// <summary>What other systems keep about the hall, with one entry per system; read and written back unchanged (<see cref="Files.ExternalReferences"/>).</summary>
    public IReadOnlyDictionary<string, JsonElement>? ExternalReferences { get; init; }

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

    /// <summary>The floor area inside the walls, mm².</summary>
    [JsonIgnore]
    public double FloorArea => Math.Abs(Outline.Tessellate().SignedArea);

    /// <summary>
    /// The floor area that can be used, mm² (HA-8): the floor area inside the walls without pillars, furniture, the
    /// areas kept free in front of doors and emergency exits, and no-go zones. Overlaps are counted once, and parts
    /// outside the walls don't count.
    /// </summary>
    [JsonIgnore]
    public double EffectiveFloorArea
    {
        get
        {
            var removed = Obstacles.Select(o => o.Area)
                .Concat(Doors.Select(d => d.KeepFreeZone()))
                .Concat(NoGoZones.Select(z => z.Area))
                .Select(a => a.Tessellate(Tolerance.AreaChord))
                .ToList();
            return AreaMeasure.Remaining(Outline.Tessellate(Tolerance.AreaChord), removed);
        }
    }

    /// <summary>
    /// Where a door at <paramref name="point"/> sits: the closest point on the walls, heading into the hall (HA-2).
    /// </summary>
    public Pose PoseOnWall(Point point) => PoseOnWall(Outline, point);

    /// <summary>The closest point on the walls of <paramref name="outline"/> to <paramref name="point"/>, heading into the hall.</summary>
    public static Pose PoseOnWall(PathDrawing outline, Point point)
    {
        ArgumentNullException.ThrowIfNull(outline);
        var along = outline.ToPath().Project(point).Pose;
        return new(along.Position, (along.Heading + Inward(outline)).Normalized);
    }

    /// <summary>
    /// Where a door is along the walls: the wall closest to <paramref name="point"/> and the distance along it from its
    /// starting corner (corner <c>Wall</c>) to the closest point (HA-2).
    /// </summary>
    public (int Wall, double Distance) WallPositionOf(Point point)
    {
        var walls = Outline.ToPath().Segments;
        var best = (Wall: 0, Distance: 0.0, Gap: double.PositiveInfinity);
        for (var i = 0; i < walls.Count; i++)
        {
            var distance = walls[i].ClosestDistance(point);
            var gap = walls[i].PointAt(distance).DistanceTo(point);
            if (gap < best.Gap) best = (i, distance, gap);
        }
        return (best.Wall, best.Distance);
    }

    /// <summary>The pose <paramref name="distance"/> along wall <paramref name="wall"/> from its starting corner, heading into the hall.</summary>
    /// <exception cref="ArgumentOutOfRangeException">There is no such wall.</exception>
    public Pose PoseOnWall(int wall, double distance)
    {
        var segment = Wall(wall);
        var pose = segment.PoseAt(Math.Clamp(distance, 0, segment.Length));
        return new(pose.Position, (pose.Heading + Inward(Outline)).Normalized);
    }

    /// <summary>The turn from the walls' direction into the hall: left of a counter-clockwise outline, right of a clockwise one.</summary>
    private static Angle Inward(PathDrawing outline) => outline.Tessellate().IsCounterClockwise ? Angle.Quarter : -Angle.Quarter;

    /// <summary>The hall with new walls; each door moves to the closest point on them, so doors stay in a wall.</summary>
    public Hall WithOutline(PathDrawing outline)
    {
        ArgumentNullException.ThrowIfNull(outline);
        if (!outline.IsClosed) throw new ArgumentException("The walls of a hall must be a closed path.", nameof(outline));
        return this with
        {
            Outline = outline,
            Doors = [.. Doors.Select(d => d with { Position = PoseOnWall(outline, d.Position.Position) })],
        };
    }

    public override string ToString() => Name;
}
