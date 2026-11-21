using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Schedules.Model.Geometry;
using Tellurian.Trains.Drawing.Model;

namespace Tellurian.Trains.Drawing.Editing.Halls;

/// <summary>
/// The commands of the hall editor (Phase 8, HA-1…HA-3); each is one undoable change, made to a copy of the hall
/// (<c>With</c>), so the earlier state is kept for undo. Whenever the walls change, doors move to the closest point on
/// the new walls, so a door is always in a wall.
/// </summary>
public static class HallCommands
{
    public const double DefaultPillarDiameter = 400;
    public const double DefaultPillarSide = 400;
    public const double DefaultDoorWidth = 1000;
    public const double DefaultEmergencyExitWidth = 1200;

    /// <summary>Replaces the walls, e.g. with a newly drawn outline; refused unless the path is closed.</summary>
    public static IEditCommand<Hall> SetOutline(PathDrawing outline) =>
        new EditCommand<Hall>("Draw walls", hall => outline.IsClosed && outline.Steps.Count >= 3 ? hall.With(h => h.SetOutline(outline)) : hall);

    /// <summary>Makes the walls a rectangle of <paramref name="width"/> × <paramref name="depth"/> at the current lower left corner.</summary>
    public static IEditCommand<Hall> SetRectangle(double width, double depth) =>
        new EditCommand<Hall>("Change hall size", hall =>
        {
            if (width <= 0 || depth <= 0) return hall;
            var corner = hall.Outline.ToPath().Bounds.Min;
            return hall.With(h => h.SetOutline(PathDrawing.Rectangle(width, depth, new Transform(Angle.Zero, corner.AsVector))));
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
        new EditCommand<Hall>("Move", hall => hall.With(moved =>
        {
            var corners = PathCorners.From(moved.Outline);
            var movedCorners = ids.Where(i => i.Kind == HallItemKind.Corner && i.Index < corners.Corners.Count).Select(i => i.Index)
                .Concat(ids.Where(i => i.Kind == HallItemKind.Wall && i.Index < corners.StepCount).SelectMany(i => new[] { i.Index, corners.EndOf(i.Index) }))
                .ToList();
            if (movedCorners.Count > 0) moved.SetOutline(corners.WithCornersMoved(movedCorners, offset).ToDrawing());
            foreach (var obstacle in Selected(moved.Obstacles, HallItemKind.Obstacle, ids)) obstacle.Area = Moved(obstacle.Area, offset);
            foreach (var zone in Selected(moved.NoGoZones, HallItemKind.NoGoZone, ids)) zone.Area = Moved(zone.Area, offset);
            foreach (var door in Selected(moved.Doors, HallItemKind.Door, ids)) door.Position = moved.PoseOnWall(door.Position.Position + offset);
            foreach (var handle in ids.Where(i => i.IsHandle))
                if (HallHandles.PositionOf(moved, handle) is { } position) HallHandles.DragIn(moved, handle, position + offset);
        }));

    /// <summary>
    /// Deletes obstacles, doors, no-go zones and corners; a handle deletes its item. The walls keep at least three
    /// corners. Walls aren't deleted.
    /// </summary>
    public static IEditCommand<Hall> Delete(IReadOnlyList<HallItem> items) =>
        new EditCommand<Hall>("Delete", hall => hall.With(deleted =>
        {
            var ids = items.Select(i => i.Owner).Distinct().ToList();
            deleted.Obstacles = Keep(deleted.Obstacles, HallItemKind.Obstacle, ids);
            deleted.Doors = Keep(deleted.Doors, HallItemKind.Door, ids);
            deleted.NoGoZones = Keep(deleted.NoGoZones, HallItemKind.NoGoZone, ids);
            var corners = PathCorners.From(deleted.Outline);
            var changed = corners;
            // From the last corner backwards, so the indexes of the remaining ones stay valid.
            foreach (var index in ids.Where(i => i.Kind == HallItemKind.Corner).Select(i => i.Index).Distinct().OrderDescending())
                if (changed.WithoutCorner(index) is { } without) changed = without;
            if (!ReferenceEquals(changed, corners)) deleted.SetOutline(changed.ToDrawing());
        }));

    public static IEditCommand<Hall> AddRoundPillar(Point center, double diameter = DefaultPillarDiameter) =>
        new EditCommand<Hall>("Add pillar", hall => hall.With(h => h.Add(Obstacle.RoundPillar(center, diameter))));

    public static IEditCommand<Hall> AddRectangularPillar(Point center, double width = DefaultPillarSide, double depth = DefaultPillarSide) =>
        new EditCommand<Hall>("Add pillar", hall => hall.With(h => h.Add(Obstacle.RectangularPillar(center, width, depth))));

    /// <summary>Adds fixed furniture with the drawn closed outline.</summary>
    public static IEditCommand<Hall> AddFurniture(PathDrawing area) =>
        new EditCommand<Hall>("Add furniture", hall => area.IsClosed ? hall.With(h => h.Add(new Obstacle { Kind = ObstacleKind.Furniture, Area = area })) : hall);

    public static IEditCommand<Hall> AddNoGoZone(PathDrawing area, string reason) =>
        new EditCommand<Hall>("Add no-go zone", hall => area.IsClosed ? hall.With(h => h.Add(new NoGoZone { Area = area, Reason = reason })) : hall);

    /// <summary>Adds a door or emergency exit in the wall closest to <paramref name="point"/>, heading into the hall.</summary>
    public static IEditCommand<Hall> AddDoor(Point point, DoorKind kind) =>
        new EditCommand<Hall>(kind == DoorKind.EmergencyExit ? "Add emergency exit" : "Add door", hall => hall.With(h => h.Add(new Door
        {
            Kind = kind,
            Position = h.PoseOnWall(point),
            Width = kind == DoorKind.EmergencyExit ? DefaultEmergencyExitWidth : DefaultDoorWidth,
        })));

    /// <summary>Replaces obstacle <paramref name="index"/> with <paramref name="obstacle"/>, which keeps the id of the one it replaces.</summary>
    public static IEditCommand<Hall> SetObstacle(int index, Obstacle obstacle) =>
        new EditCommand<Hall>("Change obstacle", hall => index < hall.Obstacles.Count
            ? hall.With(h => h.Obstacles[index] = obstacle.With(o => o.Id = hall.Obstacles[index].Id))
            : hall);

    /// <summary>Replaces door <paramref name="index"/> with <paramref name="door"/>, which keeps the id of the one it replaces and its place in the wall.</summary>
    public static IEditCommand<Hall> SetDoor(int index, Door door) =>
        new EditCommand<Hall>("Change door", hall => index < hall.Doors.Count
            ? hall.With(h => h.Doors[index] = door.With(d =>
            {
                d.Id = hall.Doors[index].Id;
                d.Position = hall.PoseOnWall(door.Position.Position);
            }))
            : hall);

    /// <summary>
    /// Moves door <paramref name="index"/> to wall <paramref name="wall"/>, with its centre <paramref name="distance"/>
    /// along the wall from the wall's starting corner; the opening is kept within the wall when it fits.
    /// </summary>
    public static IEditCommand<Hall> SetDoorPosition(int index, int wall, double distance) =>
        new EditCommand<Hall>("Move door", hall =>
        {
            if (index >= hall.Doors.Count || wall < 0 || wall >= hall.WallCount) return hall;
            var width = hall.Doors[index].Width;
            var length = hall.Wall(wall).Length;
            var centre = length >= width ? Math.Clamp(distance, width / 2, length - width / 2) : length / 2;
            return hall.With(h => h.Doors[index].Position = h.PoseOnWall(wall, centre));
        });

    /// <summary>Replaces no-go zone <paramref name="index"/> with <paramref name="zone"/>, which keeps the id of the one it replaces.</summary>
    public static IEditCommand<Hall> SetNoGoZone(int index, NoGoZone zone) =>
        new EditCommand<Hall>("Change no-go zone", hall => index < hall.NoGoZones.Count
            ? hall.With(h => h.NoGoZones[index] = zone.With(z => z.Id = hall.NoGoZones[index].Id))
            : hall);

    /// <summary>Changes the hall's own properties: name, address, note, aisle width; <paramref name="change"/> returns a changed copy (<c>With</c>).</summary>
    public static IEditCommand<Hall> SetProperties(Func<Hall, Hall> change) =>
        new EditCommand<Hall>("Change properties", change);

    private static EditCommand<Hall> ChangeCorners(string description, Func<PathCorners, PathCorners?> change) =>
        new(description, hall => change(PathCorners.From(hall.Outline)) is { } changed ? hall.With(h => h.SetOutline(changed.ToDrawing())) : hall);

    /// <summary>The items of <paramref name="kind"/> in <paramref name="items"/> that <paramref name="ids"/> select.</summary>
    private static IEnumerable<T> Selected<T>(List<T> items, HallItemKind kind, IReadOnlyList<HallItem> ids) =>
        items.Where((_, i) => ids.Contains(new(kind, i)));

    private static List<T> Keep<T>(List<T> items, HallItemKind kind, IReadOnlyList<HallItem> ids) =>
        ids.Any(i => i.Kind == kind) ? [.. items.Where((_, i) => !ids.Contains(new(kind, i)))] : items;

    private static PathDrawing Moved(PathDrawing path, Vector offset) =>
        PathDrawing.From(path.ToPath().Transformed(new Transform(Angle.Zero, offset)));
}
