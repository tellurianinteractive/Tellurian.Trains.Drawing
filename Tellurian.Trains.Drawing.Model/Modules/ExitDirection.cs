namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// The side of a module an exit is on, as seen by a viewer standing on the module's viewing side
/// (ModuleDrawing §4.1). East and west were never geography: a module is turned any way in a hall.
/// </summary>
public enum ExitDirection
{
    /// <summary>Not yet decided, e.g. for an exit that isn't placed.</summary>
    Undefined,

    /// <summary>On the viewer's left (West in schema version 1).</summary>
    Left,

    /// <summary>On the viewer's right (East in schema version 1).</summary>
    Right,

    /// <summary>On the side away from the viewer; for an exit whose track leaves across the left–right direction.</summary>
    Up,

    /// <summary>On the side towards the viewer; for an exit whose track leaves across the left–right direction.</summary>
    Down,
}
