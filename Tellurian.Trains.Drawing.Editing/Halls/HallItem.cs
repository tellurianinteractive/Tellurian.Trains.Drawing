namespace Tellurian.Trains.Drawing.Editing.Halls;

/// <summary>
/// An item of a hall drawing by its kind and index in the hall's list of that kind; for a handle, <see cref="Part"/>
/// tells which of its item's handles it is. Indexes refer to the current document; a selection is cleared when items
/// are added or removed.
/// </summary>
public readonly record struct HallItem(HallItemKind Kind, int Index, int Part = 0)
{
    public static HallItem Corner(int index) => new(HallItemKind.Corner, index);
    public static HallItem Wall(int index) => new(HallItemKind.Wall, index);
    public static HallItem Obstacle(int index) => new(HallItemKind.Obstacle, index);
    public static HallItem Door(int index) => new(HallItemKind.Door, index);
    public static HallItem NoGoZone(int index) => new(HallItemKind.NoGoZone, index);
    public static HallItem ObstacleHandle(int index, int part) => new(HallItemKind.ObstacleHandle, index, part);
    public static HallItem NoGoZoneHandle(int index, int part) => new(HallItemKind.NoGoZoneHandle, index, part);
    public static HallItem DoorHandle(int index, int part) => new(HallItemKind.DoorHandle, index, part);

    /// <summary>True for a handle that resizes its item.</summary>
    public bool IsHandle => Kind is HallItemKind.ObstacleHandle or HallItemKind.NoGoZoneHandle or HallItemKind.DoorHandle;

    /// <summary>The item a handle belongs to; the item itself for anything else.</summary>
    public HallItem Owner => Kind switch
    {
        HallItemKind.ObstacleHandle => Obstacle(Index),
        HallItemKind.NoGoZoneHandle => NoGoZone(Index),
        HallItemKind.DoorHandle => Door(Index),
        _ => this,
    };

    public override string ToString() => IsHandle ? $"{Kind}:{Index}:{Part}" : $"{Kind}:{Index}";

    /// <summary>Reads <see cref="ToString"/>'s text back, e.g. from a stored editor state.</summary>
    public static bool TryParse(string? text, out HallItem item)
    {
        item = default;
        var parts = text?.Split(':');
        if (parts is not { Length: 2 or 3 } || !Enum.TryParse<HallItemKind>(parts[0], out var kind) || !int.TryParse(parts[1], out var index)) return false;
        var part = 0;
        if (parts.Length == 3 && !int.TryParse(parts[2], out part)) return false;
        item = new(kind, index, part);
        return true;
    }
}
