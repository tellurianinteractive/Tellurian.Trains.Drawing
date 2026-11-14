using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Components.Canvas;

/// <summary>A pointer event on the canvas, with the position in world millimetres and in canvas pixels.</summary>
public sealed record CanvasPointerEventArgs
{
    public required Point World { get; init; }
    public required Point Screen { get; init; }

    /// <summary>The button that changed: 0 primary, 1 middle, 2 secondary; -1 for moves.</summary>
    public int Button { get; init; }

    /// <summary>The buttons held down, as a bit mask (1 primary, 2 secondary, 4 middle).</summary>
    public int Buttons { get; init; }

    public bool ShiftKey { get; init; }
    public bool CtrlKey { get; init; }
    public bool AltKey { get; init; }
    public bool MetaKey { get; init; }

    /// <summary>"mouse", "pen" or "touch".</summary>
    public string PointerType { get; init; } = "mouse";

    /// <summary>Ctrl on Windows and Linux, Cmd on macOS: adds to a selection.</summary>
    public bool IsAdditive => CtrlKey || MetaKey;
}
