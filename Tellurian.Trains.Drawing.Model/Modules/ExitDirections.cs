using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// Works out <see cref="ExitDirection.Up"/> and <see cref="ExitDirection.Down"/> (ModuleDrawing §4.1).
/// Left and right are given: by the Registry, by auto-draw or by the user. An exit whose track leaves the module
/// across the left–right direction, such as a branch at a right angle, is then up or down. The viewer has the left
/// exit on the left, so up, the side away from the viewer, is to the left of the direction from the left exit to the
/// right one.
/// </summary>
public static class ExitDirections
{
    /// <summary>
    /// An exit leaves across the left–right direction when its heading is closer to straight across than to along:
    /// more than this angle, in degrees, from the direction from the left exit to the right one, and from the opposite direction.
    /// </summary>
    public const double AcrossAngle = 45;

    /// <summary>
    /// The direction of an exit at <paramref name="exit"/> when the left exit is at <paramref name="left"/> and the
    /// right one at <paramref name="right"/>: up or down when it leaves across the left–right direction, otherwise
    /// undefined, since an exit that leaves along it may be on either side.
    /// </summary>
    public static ExitDirection Infer(Pose exit, Point left, Point right)
    {
        var across = right - left;
        if (across.Length < Tolerance.Length) return ExitDirection.Undefined;
        var turn = (exit.Heading - across.Direction).Signed.Degrees;
        if (Math.Abs(turn) <= AcrossAngle || Math.Abs(turn) >= Angle.Half.Degrees - AcrossAngle) return ExitDirection.Undefined;
        return turn > 0 ? ExitDirection.Up : ExitDirection.Down;
    }

    /// <summary>
    /// The drawing with up or down set for the exits that have no direction and leave across the left–right direction.
    /// Exits that have a direction keep it, so what the user has chosen isn't changed. Nothing is inferred until the
    /// drawing has both a left and a right exit.
    /// </summary>
    public static ModuleDrawing WithInferredExitDirections(this ModuleDrawing drawing)
    {
        ArgumentNullException.ThrowIfNull(drawing);
        var left = drawing.Exits.FirstOrDefault(e => e.Direction == ExitDirection.Left);
        var right = drawing.Exits.FirstOrDefault(e => e.Direction == ExitDirection.Right);
        if (left is null || right is null) return drawing;
        var exits = drawing.Exits
            .Select(e => e.Direction == ExitDirection.Undefined ? e with { Direction = Infer(e.Pose, left.Pose.Position, right.Pose.Position) } : e)
            .ToList();
        return exits.SequenceEqual(drawing.Exits) ? drawing : drawing with { Exits = exits };
    }
}
