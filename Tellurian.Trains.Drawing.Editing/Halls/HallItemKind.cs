namespace Tellurian.Trains.Drawing.Editing.Halls;

/// <summary>The kinds of selectable items in a hall drawing.</summary>
public enum HallItemKind
{
    /// <summary>A corner of the walls, where wall <c>i - 1</c> ends and wall <c>i</c> starts.</summary>
    Corner,

    Wall,

    /// <summary>A pillar or fixed furniture.</summary>
    Obstacle,

    /// <summary>A door or emergency exit.</summary>
    Door,

    NoGoZone,

    /// <summary>A handle of a selected obstacle: a corner, or the edge of a round pillar (<see cref="HallItem.Part"/>).</summary>
    ObstacleHandle,

    /// <summary>A corner handle of a selected no-go zone.</summary>
    NoGoZoneHandle,

    /// <summary>A handle of a selected door: 0 and 1 the ends of the opening, 2 the far edge of the area kept free.</summary>
    DoorHandle,
}
