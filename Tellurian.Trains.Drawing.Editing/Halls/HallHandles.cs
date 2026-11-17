using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.Halls;

namespace Tellurian.Trains.Drawing.Editing.Halls;

/// <summary>
/// The handles that resize a selected obstacle, no-go zone or door, and what dragging them does:
/// <list type="bullet">
/// <item>a rectangle's corner keeps the opposite corner and the rectangle's rotation, so it stays a rectangle;</item>
/// <item>another polygon's corner moves on its own;</item>
/// <item>a round pillar's handle, on its edge, sets the diameter;</item>
/// <item>a door's handles at the ends of the opening set its width along the wall, and the one at the far edge of the
/// area kept free in front of it sets that depth.</item>
/// </list>
/// </summary>
public static class HallHandles
{
    /// <summary>The smallest width or depth a handle can make an item, mm.</summary>
    public const double MinimumSize = 50;

    private const int DoorDepthHandle = 2;

    /// <summary>The handles of <paramref name="owner"/> and where they are; none for items without handles.</summary>
    public static IReadOnlyList<(HallItem Handle, Point Position)> Of(Hall hall, HallItem owner)
    {
        switch (owner.Kind)
        {
            case HallItemKind.Obstacle when owner.Index < hall.Obstacles.Count:
                var obstacle = hall.Obstacles[owner.Index];
                if (obstacle.TryGetCircle(out var center, out var diameter)) return [(HallItem.ObstacleHandle(owner.Index, 0), center + new Vector(diameter / 2, 0))];
                return Corners(obstacle.Area, part => HallItem.ObstacleHandle(owner.Index, part));
            case HallItemKind.NoGoZone when owner.Index < hall.NoGoZones.Count:
                return Corners(hall.NoGoZones[owner.Index].Area, part => HallItem.NoGoZoneHandle(owner.Index, part));
            case HallItemKind.Door when owner.Index < hall.Doors.Count:
                var door = hall.Doors[owner.Index];
                var (a, b) = HallScene.Opening(door);
                return
                [
                    (HallItem.DoorHandle(owner.Index, 0), a),
                    (HallItem.DoorHandle(owner.Index, 1), b),
                    (HallItem.DoorHandle(owner.Index, DoorDepthHandle), door.Position.Advanced(door.KeepFreeDepth).Position),
                ];
            default:
                return [];
        }
    }

    /// <summary>Where <paramref name="handle"/> is; null if it doesn't exist (any more).</summary>
    public static Point? PositionOf(Hall hall, HallItem handle)
    {
        foreach (var (item, position) in Of(hall, handle.Owner))
            if (item == handle) return position;
        return null;
    }

    /// <summary>True if <paramref name="item"/> exists in <paramref name="hall"/>: for a handle, its item and the handle.</summary>
    public static bool Exists(Hall hall, HallItem item) =>
        item.IsHandle ? PositionOf(hall, item) is not null : HallScene.Items(hall).Contains(item);

    /// <summary>The hall with <paramref name="handle"/> dragged to <paramref name="point"/>; unchanged if that would make its item too small.</summary>
    public static Hall Drag(Hall hall, HallItem handle, Point point)
    {
        switch (handle.Kind)
        {
            case HallItemKind.ObstacleHandle when handle.Index < hall.Obstacles.Count:
                var obstacle = hall.Obstacles[handle.Index];
                var area = obstacle.TryGetCircle(out var center, out _)
                    ? (2 * point.DistanceTo(center) >= MinimumSize ? PathDrawing.Circle(center, 2 * point.DistanceTo(center)) : null)
                    : DragCorner(obstacle.Area, handle.Part, point);
                return area is null ? hall : hall with { Obstacles = Replace(hall.Obstacles, handle.Index, obstacle with { Area = area }) };
            case HallItemKind.NoGoZoneHandle when handle.Index < hall.NoGoZones.Count:
                var zone = hall.NoGoZones[handle.Index];
                return DragCorner(zone.Area, handle.Part, point) is { } zoneArea ? hall with { NoGoZones = Replace(hall.NoGoZones, handle.Index, zone with { Area = zoneArea }) } : hall;
            case HallItemKind.DoorHandle when handle.Index < hall.Doors.Count:
                return DragDoor(hall, handle, point);
            default:
                return hall;
        }
    }

    /// <summary>
    /// Moves corner <paramref name="corner"/> of a closed area to <paramref name="point"/>. A rectangle keeps its
    /// opposite corner and its rotation; null if it would be narrower than <see cref="MinimumSize"/>.
    /// </summary>
    private static PathDrawing? DragCorner(PathDrawing area, int corner, Point point)
    {
        var corners = PathCorners.From(area);
        if (corner >= corners.Corners.Count) return null;
        if (!corners.TryGetRectangle(out _, out _, out _, out var rotation)) return corners.WithCorner(corner, point).ToDrawing();
        var opposite = corners.Corners[(corner + 2) % 4];
        var along = Vector.FromAngle(rotation);
        var across = along.LeftNormal;
        var diagonal = point - opposite;
        double width = diagonal.Dot(along), depth = diagonal.Dot(across);
        if (Math.Abs(width) < MinimumSize || Math.Abs(depth) < MinimumSize) return null;
        return PathDrawing.Rectangle(opposite + diagonal / 2, Math.Abs(width), Math.Abs(depth), rotation);
    }

    /// <summary>
    /// Moves an end of a door's opening along its wall, keeping the other end, or sets the depth kept free in front of
    /// it from the far edge's handle.
    /// </summary>
    private static Hall DragDoor(Hall hall, HallItem handle, Point point)
    {
        var door = hall.Doors[handle.Index];
        if (handle.Part == DoorDepthHandle)
        {
            var depth = (point - door.Position.Position).Dot(door.Position.Forward);
            return depth < MinimumSize ? hall : hall with { Doors = Replace(hall.Doors, handle.Index, door with { KeepFreeDepth = depth }) };
        }
        var (wall, _) = hall.WallPositionOf(door.Position.Position);
        var segment = hall.Wall(wall);
        var (a, b) = HallScene.Opening(door);
        var kept = segment.ClosestDistance(handle.Part == 0 ? b : a);
        var dragged = segment.ClosestDistance(point);
        var width = Math.Abs(dragged - kept);
        if (width < MinimumSize) return hall;
        return hall with { Doors = Replace(hall.Doors, handle.Index, door with { Width = width, Position = hall.PoseOnWall(wall, (dragged + kept) / 2) }) };
    }

    private static List<(HallItem, Point)> Corners(PathDrawing area, Func<int, HallItem> handle) =>
        [.. PathCorners.From(area).Corners.Select((corner, i) => (handle(i), corner))];

    private static IReadOnlyList<T> Replace<T>(IReadOnlyList<T> items, int index, T item) =>
        [.. items.Select((existing, i) => i == index ? item : existing)];
}
