namespace Tellurian.Trains.Drawing.Editing.Snapping;

/// <summary>What a point snapped to (MD-12), from the weakest to the strongest.</summary>
public enum SnapKind
{
    /// <summary>Nothing within reach; the point is used as it is.</summary>
    None,

    Grid,

    /// <summary>A point on an edge, e.g. a module side or a wall.</summary>
    Edge,

    /// <summary>A significant point, e.g. a corner or the end of a segment.</summary>
    Point,

    /// <summary>A connection point with a heading, e.g. an exit or a track end.</summary>
    Connection,
}
