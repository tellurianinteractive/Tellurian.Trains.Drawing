using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Editing.Tools;

/// <summary>
/// A pointer event as tools see it, independent of the UI: the position in world millimetres, the button,
/// the modifiers, and how far in millimetres the pointer may be from something to hit or snap to it.
/// </summary>
public sealed record ToolPointer(Point World, double Tolerance)
{
    public PointerButton Button { get; init; } = PointerButton.Primary;

    /// <summary>Ctrl or Cmd: add to or toggle in the selection.</summary>
    public bool IsAdditive { get; init; }

    /// <summary>Shift: constrain, e.g. to horizontal or vertical moves.</summary>
    public bool IsConstrained { get; init; }

    /// <summary>Alt: don't snap.</summary>
    public bool IsSnapSuppressed { get; init; }
}
