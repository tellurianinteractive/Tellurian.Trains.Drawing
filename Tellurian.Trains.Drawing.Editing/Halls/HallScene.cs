using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Editing.Snapping;
using Tellurian.Trains.Drawing.Editing.Tools;
using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.Halls;

namespace Tellurian.Trains.Drawing.Editing.Halls;

/// <summary>
/// What the select and move tool needs for a hall (Phase 8): hit testing with the small items on top (corners,
/// doors, obstacles, no-go zones, then walls), snap targets and moves. A wall's snap targets move with the wall
/// and with its two corners, so dragging a corner doesn't snap it to its own walls.
/// </summary>
public sealed class HallScene : ISelectableScene<Hall, HallItem>
{
    public const double IndexCellSize = 1000;

    /// <summary>The items whose handles can be grabbed: the items of the current selection (<see cref="ShowHandlesFor"/>).</summary>
    public IReadOnlySet<HallItem> HandleOwners { get; private set; } = new HashSet<HallItem>();

    /// <summary>Makes the handles of the selected items, or of the items of selected handles, grabbable.</summary>
    public void ShowHandlesFor(IEnumerable<HallItem> selection) => HandleOwners = selection.Select(i => i.Owner).ToHashSet();

    public bool TryHitTest(Hall document, Point point, double tolerance, out HallItem id)
    {
        // The handles of selected items are on top of everything.
        foreach (var owner in HandleOwners)
            foreach (var (handle, position) in HallHandles.Of(document, owner))
                if (point.DistanceTo(position) <= tolerance) return Found(handle, out id);
        var corners = PathCorners.From(document.Outline);
        for (var i = corners.Corners.Count - 1; i >= 0; i--)
            if (point.DistanceTo(corners.Corners[i]) <= tolerance) return Found(HallItem.Corner(i), out id);
        for (var i = document.Doors.Count - 1; i >= 0; i--)
        {
            var (a, b) = Opening(document.Doors[i]);
            if (Measure.PointToSegment(point, a, b) <= tolerance || document.Doors[i].KeepFreeZone().Tessellate().Contains(point)) return Found(HallItem.Door(i), out id);
        }
        for (var i = document.Obstacles.Count - 1; i >= 0; i--)
            if (IsOnOrInside(document.Obstacles[i].Area, point, tolerance)) return Found(HallItem.Obstacle(i), out id);
        for (var i = document.NoGoZones.Count - 1; i >= 0; i--)
            if (IsOnOrInside(document.NoGoZones[i].Area, point, tolerance)) return Found(HallItem.NoGoZone(i), out id);
        var walls = document.Outline.ToPath().Segments;
        for (var i = walls.Count - 1; i >= 0; i--)
            if (walls[i].PointAt(walls[i].ClosestDistance(point)).DistanceTo(point) <= tolerance) return Found(HallItem.Wall(i), out id);
        id = default;
        return false;
    }

    public BoundingBox BoundsOf(Hall document, HallItem id)
    {
        switch (id.Kind)
        {
            case HallItemKind.Corner when PathCorners.From(document.Outline).Corners is var corners && id.Index < corners.Count:
                return new(corners[id.Index], corners[id.Index]);
            case HallItemKind.Wall when id.Index < document.WallCount:
                return document.Wall(id.Index).Bounds;
            case HallItemKind.Obstacle when id.Index < document.Obstacles.Count:
                return document.Obstacles[id.Index].Area.ToPath().Bounds;
            case HallItemKind.Door when id.Index < document.Doors.Count:
                return document.Doors[id.Index].KeepFreeZone().ToPath().Bounds;
            case HallItemKind.NoGoZone when id.Index < document.NoGoZones.Count:
                return document.NoGoZones[id.Index].Area.ToPath().Bounds;
            case HallItemKind.ObstacleHandle or HallItemKind.NoGoZoneHandle or HallItemKind.DoorHandle when HallHandles.PositionOf(document, id) is { } handle:
                return new(handle, handle);
            default:
                return BoundingBox.Empty;
        }
    }

    public SpatialIndex<HallItem> Index(Hall document)
    {
        var index = new SpatialIndex<HallItem>(IndexCellSize);
        foreach (var item in Items(document))
        {
            var bounds = BoundsOf(document, item);
            if (!bounds.IsEmpty) index.Set(item, bounds);
        }
        return index;
    }

    public IEnumerable<SnapTarget> SnapTargetsOf(Hall document, HallItem id)
    {
        switch (id.Kind)
        {
            case HallItemKind.Corner when PathCorners.From(document.Outline).Corners is var corners && id.Index < corners.Count:
                yield return new PointTarget(corners[id.Index], id);
                break;
            case HallItemKind.Wall when id.Index < document.WallCount && document.Wall(id.Index) is LineSegment wall:
                yield return new EdgeTarget(wall.Start, wall.End, new WallSource(id.Index, (id.Index + 1) % document.WallCount));
                break;
            case HallItemKind.Obstacle when id.Index < document.Obstacles.Count:
                var obstacle = document.Obstacles[id.Index];
                if (obstacle.TryGetCircle(out var center, out _)) yield return new PointTarget(center, id);
                foreach (var target in PathTargets(obstacle.Area, id)) yield return target;
                break;
            case HallItemKind.Door when id.Index < document.Doors.Count:
                var door = document.Doors[id.Index];
                var (a, b) = Opening(door);
                yield return new PointTarget(door.Position.Position, id);
                yield return new PointTarget(a, id);
                yield return new PointTarget(b, id);
                break;
            case HallItemKind.NoGoZone when id.Index < document.NoGoZones.Count:
                foreach (var target in PathTargets(document.NoGoZones[id.Index].Area, id)) yield return target;
                break;
            case HallItemKind.ObstacleHandle or HallItemKind.NoGoZoneHandle or HallItemKind.DoorHandle when HallHandles.PositionOf(document, id) is { } handle:
                yield return new PointTarget(handle, id);
                break;
        }
    }

    /// <summary>Every snap target of the hall.</summary>
    public IEnumerable<SnapTarget> AllSnapTargets(Hall document) =>
        Items(document).SelectMany(item => SnapTargetsOf(document, item));

    /// <summary>A wall moves with itself and with its two corners; everything else only with itself.</summary>
    public bool MovesWith(SnapTarget target, IReadOnlySet<HallItem> dragged) => target.Source switch
    {
        WallSource wall => dragged.Contains(HallItem.Wall(wall.Wall)) || dragged.Contains(HallItem.Corner(wall.Wall)) || dragged.Contains(HallItem.Corner(wall.EndCorner)),
        // A dragged handle reshapes its item, so the item's own targets move too.
        HallItem item => dragged.Contains(item) || dragged.Any(d => d.IsHandle && d.Owner == item),
        _ => false,
    };

    public IEditCommand<Hall> Move(IReadOnlyList<HallItem> ids, Vector offset) => HallCommands.Move(ids, offset);

    public static IEnumerable<HallItem> Items(Hall document)
    {
        var corners = PathCorners.From(document.Outline);
        for (var i = 0; i < corners.Corners.Count; i++) yield return HallItem.Corner(i);
        for (var i = 0; i < corners.StepCount; i++) yield return HallItem.Wall(i);
        for (var i = 0; i < document.Obstacles.Count; i++) yield return HallItem.Obstacle(i);
        for (var i = 0; i < document.Doors.Count; i++) yield return HallItem.Door(i);
        for (var i = 0; i < document.NoGoZones.Count; i++) yield return HallItem.NoGoZone(i);
    }

    /// <summary>The ends of a door's opening, on the wall.</summary>
    public static (Point Start, Point End) Opening(Door door)
    {
        var half = door.Position.Left * (door.Width / 2);
        return (door.Position.Position + half, door.Position.Position - half);
    }

    /// <summary>
    /// The dimensions that place door <paramref name="index"/> in its wall: from the wall's starting corner to the
    /// nearer edge of the opening, and from the other edge to the wall's ending corner, measured along the wall and
    /// drawn <paramref name="offset"/> into the hall. None for a door in a curved wall, where a straight dimension
    /// wouldn't show the distance along the wall.
    /// </summary>
    public static IReadOnlyList<Dimension> DoorDimensions(Hall hall, int index, double offset)
    {
        if (index < 0 || index >= hall.Doors.Count) return [];
        var door = hall.Doors[index];
        var (wall, distance) = hall.WallPositionOf(door.Position.Position);
        if (hall.Wall(wall) is not LineSegment segment) return [];
        var inside = hall.Outline.Tessellate().IsCounterClockwise ? offset : -offset;
        var near = segment.PointAt(Math.Max(0, distance - door.Width / 2));
        var far = segment.PointAt(Math.Min(segment.Length, distance + door.Width / 2));
        return
        [
            new() { Start = segment.Start, End = near, Kind = DimensionKind.Aligned, Offset = inside },
            new() { Start = far, End = segment.End, Kind = DimensionKind.Aligned, Offset = inside },
        ];
    }

    /// <summary>
    /// The symbol of a module entrance (HA-7): a double arrow, in and out, from the door into the hall, as a closed
    /// outline. It takes most of its place in front of the door (<see cref="SymbolCells"/>).
    /// </summary>
    public static IReadOnlyList<Point> EntranceSymbol(Door door)
    {
        var cell = SymbolCells(door)[^1];
        var length = SymbolShare * Math.Min(cell.Width, cell.Depth);
        var forward = door.Position.Forward;
        var left = door.Position.Left;
        var start = cell.Center - forward * (length / 2);
        var end = start + forward * length;
        double head = length * 0.3, headHalf = length * 0.2, shaftHalf = length * 0.06;
        Point At(Point from, double along, double side) => from + forward * along + left * side;
        return
        [
            start, At(start, head, headHalf), At(start, head, shaftHalf), At(end, -head, shaftHalf), At(end, -head, headHalf),
            end, At(end, -head, -headHalf), At(end, -head, -shaftHalf), At(start, head, -shaftHalf), At(start, head, -headHalf),
        ];
    }

    /// <summary>
    /// Where the emergency exit sign of <paramref name="door"/> is drawn: a square in its place in front of the exit
    /// (<see cref="SymbolCells"/>), turned so that the running figure runs towards the exit. Where that would turn the
    /// sign upside down, it is <c>Mirrored</c> instead, running the other way in its own frame, as the left-hand
    /// variant of the sign does.
    /// </summary>
    public static (Point Center, double Size, Angle Rotation, bool Mirrored) EmergencySign(Door door)
    {
        var cell = SymbolCells(door)[0];
        var towardsExit = door.Position.Heading + Angle.Half;
        // The sign's up direction is its running direction turned a quarter left; it mustn't point down on screen.
        var mirrored = Math.Cos(towardsExit.Radians) < -Tolerance.Collinear;
        return (cell.Center, SymbolShare * Math.Min(cell.Width, cell.Depth), (mirrored ? towardsExit + Angle.Half : towardsExit).Normalized, mirrored);
    }

    /// <summary>
    /// Where a door's <paramref name="label"/> is centred: just beyond the area kept free in front of the door, so it
    /// doesn't cover the door's symbols. The text is upright, so on a wall at the side it reaches into the hall with
    /// half its width, and in a wall above or below with half its height. <paramref name="textSize"/> is its height, mm.
    /// </summary>
    public static Point LabelPosition(Door door, string label, double textSize)
    {
        var forward = door.Position.Forward;
        var halfWidth = label.Length * EndProfiles.EndProfileLabel.CharacterWidth * textSize / 2;
        var halfHeight = textSize / 2;
        var reach = Math.Abs(forward.X) * halfWidth + Math.Abs(forward.Y) * halfHeight;
        return door.Position.Advanced(FreeDepth(door) + LabelGap * textSize + reach).Position;
    }

    /// <summary>The gap between the area in front of a door and its label, as a share of the text height.</summary>
    private const double LabelGap = 0.4;

    /// <summary>How much of its place in front of a door a symbol takes.</summary>
    private const double SymbolShare = 0.8;

    /// <summary>The depth in front of a door that symbols are drawn in: its keep-free depth, or its width without one.</summary>
    private static double FreeDepth(Door door) => door.KeepFreeDepth > 0 ? door.KeepFreeDepth : door.Width;

    /// <summary>
    /// The places of a door's symbols in the area in front of it, the emergency exit sign first: the whole area for
    /// one symbol; for an emergency exit that is also a module entrance, two halves, side by side along the wall or
    /// stacked into the hall (the sign nearest the door), whichever lets the symbols be larger.
    /// </summary>
    private static IReadOnlyList<SymbolCell> SymbolCells(Door door)
    {
        var depth = FreeDepth(door);
        var forward = door.Position.Forward;
        var middle = door.Position.Position + forward * (depth / 2);
        if (door.Kind != DoorKind.EmergencyExit || !door.IsModuleEntrance) return [new(middle, door.Width, depth)];
        var sideBySide = Math.Min(door.Width / 2, depth);
        var stacked = Math.Min(door.Width, depth / 2);
        if (stacked > sideBySide)
            return [new(middle - forward * (depth / 4), door.Width, depth / 2), new(middle + forward * (depth / 4), door.Width, depth / 2)];
        var left = door.Position.Left * (door.Width / 4);
        return [new(middle + left, door.Width / 2, depth), new(middle - left, door.Width / 2, depth)];
    }

    /// <summary>A place for a symbol in front of a door: its centre, its width along the wall and its depth into the hall.</summary>
    private readonly record struct SymbolCell(Point Center, double Width, double Depth);

    private static bool IsOnOrInside(PathDrawing area, Point point, double tolerance)
    {
        var polyline = area.Tessellate();
        return polyline.Contains(point) || Measure.PointToPolyline(point, polyline) <= tolerance;
    }

    private static IEnumerable<SnapTarget> PathTargets(PathDrawing drawing, HallItem id)
    {
        var path = drawing.ToPath();
        foreach (var segment in path.Segments)
        {
            if (segment is LineSegment line)
            {
                yield return new PointTarget(line.Start, id);
                yield return new EdgeTarget(line.Start, line.End, id);
            }
        }
    }

    /// <summary>The source of a wall's snap target: the wall and the corner where it ends (it starts at the corner with its own index).</summary>
    private sealed record WallSource(int Wall, int EndCorner);

    private static bool Found(HallItem item, out HallItem id)
    {
        id = item;
        return true;
    }
}
