using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Schedules.Model.Geometry;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.Halls;

namespace Tellurian.Trains.Drawing.Editing.Halls;

/// <summary>
/// The commands of the hall editor (Phase 8, HA-1…HA-3); each is one undoable change. Whenever the walls change,
/// doors move to the closest point on the new walls, so a door is always in a wall.
/// </summary>
public static class HallCommands
{
    public const double DefaultPillarDiameter = 400;
    public const double DefaultPillarSide = 400;
    public const double DefaultDoorWidth = 1000;
    public const double DefaultEmergencyExitWidth = 1200;

    /// <summary>Replaces the walls, e.g. with a newly drawn outline; refused unless the path is closed.</summary>
    public static IEditCommand<Hall> SetOutline(PathDrawing outline) =>
        new EditCommand<Hall>("Draw walls", hall => outline.IsClosed && outline.Steps.Count >= 3 ? hall.WithOutline(outline) : hall);

    /// <summary>Makes the walls a rectangle of <paramref name="width"/> × <paramref name="depth"/> at the current lower left corner.</summary>
    public static IEditCommand<Hall> SetRectangle(double width, double depth) =>
        new EditCommand<Hall>("Change hall size", hall =>
        {
            if (width <= 0 || depth <= 0) return hall;
            var corner = hall.Outline.ToPath().Bounds.Min;
            return hall.WithOutline(PathDrawing.Rectangle(width, depth, new Transform(Angle.Zero, corner.AsVector)));
        });

    public static IEditCommand<Hall> SetCorner(int index, Point point) =>
        ChangeCorners("Move corner", corners => index < corners.Corners.Count ? corners.WithCorner(index, point) : null);

    /// <summary>Adds a corner in the middle of wall <paramref name="wall"/>.</summary>
    public static IEditCommand<Hall> InsertCorner(int wall) =>
        ChangeCorners("Add corner", corners => wall < corners.StepCount ? corners.WithCornerInserted(wall) : null);

    /// <summary>Makes wall <paramref name="wall"/> an arc of <paramref name="sweep"/> degrees, or straight with 0.</summary>
    public static IEditCommand<Hall> SetWallSweep(int wall, double sweep) =>
        ChangeCorners("Change wall", corners => wall < corners.StepCount && Math.Abs(sweep) < 360 ? corners.WithSweep(wall, sweep) : null);

    /// <summary>Sets the length of a straight wall by moving the corner where it ends.</summary>
    public static IEditCommand<Hall> SetWallLength(int wall, double length) =>
        ChangeCorners("Change wall length", corners => wall < corners.StepCount ? corners.WithLength(wall, length) : null);

    /// <summary>
    /// Moves items: corners and walls reshape the walls (a wall moves both its corners), obstacles and no-go zones
    /// move, doors slide to the closest point on the walls, and handles resize their items (<see cref="HallHandles"/>).
    /// </summary>
    public static IEditCommand<Hall> Move(IReadOnlyList<HallItem> ids, Vector offset) =>
        new EditCommand<Hall>("Move", hall =>
        {
            var corners = PathCorners.From(hall.Outline);
            var movedCorners = ids.Where(i => i.Kind == HallItemKind.Corner && i.Index < corners.Corners.Count).Select(i => i.Index)
                .Concat(ids.Where(i => i.Kind == HallItemKind.Wall && i.Index < corners.StepCount).SelectMany(i => new[] { i.Index, corners.EndOf(i.Index) }))
                .ToList();
            var moved = movedCorners.Count == 0 ? hall : hall.WithOutline(corners.WithCornersMoved(movedCorners, offset).ToDrawing());
            moved = moved with
            {
                Obstacles = Map(moved.Obstacles, HallItemKind.Obstacle, ids, o => o with { Area = Moved(o.Area, offset) }),
                NoGoZones = Map(moved.NoGoZones, HallItemKind.NoGoZone, ids, z => z with { Area = Moved(z.Area, offset) }),
                Doors = Map(moved.Doors, HallItemKind.Door, ids, d => d with { Position = moved.PoseOnWall(d.Position.Position + offset) }),
            };
            foreach (var handle in ids.Where(i => i.IsHandle))
                if (HallHandles.PositionOf(moved, handle) is { } position) moved = HallHandles.Drag(moved, handle, position + offset);
            return moved;
        });

    /// <summary>
    /// Deletes obstacles, doors, no-go zones and corners; a handle deletes its item. The walls keep at least three
    /// corners. Walls aren't deleted.
    /// </summary>
    public static IEditCommand<Hall> Delete(IReadOnlyList<HallItem> items) =>
        new EditCommand<Hall>("Delete", hall =>
        {
            var ids = items.Select(i => i.Owner).Distinct().ToList();
            var deleted = hall with
            {
                Obstacles = Keep(hall.Obstacles, HallItemKind.Obstacle, ids),
                Doors = Keep(hall.Doors, HallItemKind.Door, ids),
                NoGoZones = Keep(hall.NoGoZones, HallItemKind.NoGoZone, ids),
            };
            var corners = PathCorners.From(hall.Outline);
            var changed = corners;
            // From the last corner backwards, so the indexes of the remaining ones stay valid.
            foreach (var index in ids.Where(i => i.Kind == HallItemKind.Corner).Select(i => i.Index).Distinct().OrderDescending())
                if (changed.WithoutCorner(index) is { } without) changed = without;
            return ReferenceEquals(changed, corners) ? deleted : deleted.WithOutline(changed.ToDrawing());
        });

    public static IEditCommand<Hall> AddRoundPillar(Point center, double diameter = DefaultPillarDiameter) =>
        new EditCommand<Hall>("Add pillar", hall => hall with { Obstacles = [.. hall.Obstacles, Obstacle.RoundPillar(center, diameter)] });

    public static IEditCommand<Hall> AddRectangularPillar(Point center, double width = DefaultPillarSide, double depth = DefaultPillarSide) =>
        new EditCommand<Hall>("Add pillar", hall => hall with { Obstacles = [.. hall.Obstacles, Obstacle.RectangularPillar(center, width, depth)] });

    /// <summary>Adds fixed furniture with the drawn closed outline.</summary>
    public static IEditCommand<Hall> AddFurniture(PathDrawing area) =>
        new EditCommand<Hall>("Add furniture", hall => area.IsClosed ? hall with { Obstacles = [.. hall.Obstacles, new() { Kind = ObstacleKind.Furniture, Area = area }] } : hall);

    public static IEditCommand<Hall> AddNoGoZone(PathDrawing area, string reason) =>
        new EditCommand<Hall>("Add no-go zone", hall => area.IsClosed ? hall with { NoGoZones = [.. hall.NoGoZones, new() { Area = area, Reason = reason }] } : hall);

    /// <summary>Adds a door or emergency exit in the wall closest to <paramref name="point"/>, heading into the hall.</summary>
    public static IEditCommand<Hall> AddDoor(Point point, DoorKind kind) =>
        new EditCommand<Hall>(kind == DoorKind.EmergencyExit ? "Add emergency exit" : "Add door", hall => hall with
        {
            Doors = [.. hall.Doors, new() { Kind = kind, Position = hall.PoseOnWall(point), Width = kind == DoorKind.EmergencyExit ? DefaultEmergencyExitWidth : DefaultDoorWidth }],
        });

    public static IEditCommand<Hall> SetObstacle(int index, Obstacle obstacle) =>
        new EditCommand<Hall>("Change obstacle", hall => hall with { Obstacles = Replace(hall.Obstacles, index, obstacle) });

    /// <summary>Changes a door; its position is kept in the wall.</summary>
    public static IEditCommand<Hall> SetDoor(int index, Door door) =>
        new EditCommand<Hall>("Change door", hall => hall with { Doors = Replace(hall.Doors, index, door with { Position = hall.PoseOnWall(door.Position.Position) }) });

    /// <summary>
    /// Moves door <paramref name="index"/> to wall <paramref name="wall"/>, with its centre <paramref name="distance"/>
    /// along the wall from the wall's starting corner; the opening is kept within the wall when it fits.
    /// </summary>
    public static IEditCommand<Hall> SetDoorPosition(int index, int wall, double distance) =>
        new EditCommand<Hall>("Move door", hall =>
        {
            if (index >= hall.Doors.Count || wall < 0 || wall >= hall.WallCount) return hall;
            var door = hall.Doors[index];
            var length = hall.Wall(wall).Length;
            var centre = length >= door.Width ? Math.Clamp(distance, door.Width / 2, length - door.Width / 2) : length / 2;
            return hall with { Doors = Replace(hall.Doors, index, door with { Position = hall.PoseOnWall(wall, centre) }) };
        });

    public static IEditCommand<Hall> SetNoGoZone(int index, NoGoZone zone) =>
        new EditCommand<Hall>("Change no-go zone", hall => hall with { NoGoZones = Replace(hall.NoGoZones, index, zone) });

    /// <summary>Changes the hall's own properties: name, address, note, aisle width.</summary>
    public static IEditCommand<Hall> SetProperties(Func<Hall, Hall> change) =>
        new EditCommand<Hall>("Change properties", change);

    private static EditCommand<Hall> ChangeCorners(string description, Func<PathCorners, PathCorners?> change) =>
        new(description, hall => change(PathCorners.From(hall.Outline)) is { } changed ? hall.WithOutline(changed.ToDrawing()) : hall);

    private static IReadOnlyList<T> Map<T>(IReadOnlyList<T> items, HallItemKind kind, IReadOnlyList<HallItem> ids, Func<T, T> change) =>
        ids.Any(i => i.Kind == kind) ? [.. items.Select((item, i) => ids.Contains(new(kind, i)) ? change(item) : item)] : items;

    private static IReadOnlyList<T> Keep<T>(IReadOnlyList<T> items, HallItemKind kind, IReadOnlyList<HallItem> ids) =>
        ids.Any(i => i.Kind == kind) ? [.. items.Where((_, i) => !ids.Contains(new(kind, i)))] : items;

    private static IReadOnlyList<T> Replace<T>(IReadOnlyList<T> items, int index, T item) =>
        [.. items.Select((existing, i) => i == index ? item : existing)];

    private static PathDrawing Moved(PathDrawing path, Vector offset) =>
        PathDrawing.From(path.ToPath().Transformed(new Transform(Angle.Zero, offset)));
}
