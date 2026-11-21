namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// An exit of a module by its side and its number on that side, e.g. Left 1 or Right 2: the n:th exit with that
/// <see cref="ModuleExit.Direction"/>, in the order of the drawing's exits. This is how a hall plan refers to an exit
/// (HP-7). It doesn't depend on exit labels, which are free text for the reader, so a module can be redrawn or
/// replaced in a plan as long as it has the same exits.
/// </summary>
/// <param name="Side">The side of the module the exit is on.</param>
/// <param name="Number">The number of the exit among those on the side, from 1.</param>
public sealed record ExitPlace(ExitDirection Side, int Number = 1) : ExtensibleRecord
{
    /// <summary>The first exit on the left.</summary>
    public static ExitPlace Left { get; } = new(ExitDirection.Left);

    /// <summary>The first exit on the right.</summary>
    public static ExitPlace Right { get; } = new(ExitDirection.Right);

    /// <summary>The place of <paramref name="exit"/> in <paramref name="drawing"/>; null when it isn't one of its exits.</summary>
    public static ExitPlace? Of(ModuleDrawing drawing, ModuleExit exit)
    {
        ArgumentNullException.ThrowIfNull(drawing);
        ArgumentNullException.ThrowIfNull(exit);
        var number = drawing.Exits.Where(e => e.Direction == exit.Direction).ToList().IndexOf(exit) + 1;
        return number > 0 ? new(exit.Direction, number) : null;
    }

    /// <summary>The exit of <paramref name="drawing"/> at this place; null when it has none there.</summary>
    public ModuleExit? In(ModuleDrawing drawing)
    {
        ArgumentNullException.ThrowIfNull(drawing);
        return Number < 1 ? null : drawing.Exits.Where(e => e.Direction == Side).ElementAtOrDefault(Number - 1);
    }

    public override string ToString() => $"{Side} {Number}";
}
