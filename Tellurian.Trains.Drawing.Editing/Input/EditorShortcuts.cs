using Tellurian.Trains.Schedules.Model.Geometry;

namespace Tellurian.Trains.Drawing.Editing.Input;

/// <summary>A key pressed in an editor, as the browser reports it (<c>KeyboardEvent.key</c>) with its modifiers.</summary>
/// <param name="Key">The key's value, e.g. "z", "Delete", "ArrowLeft", "1".</param>
public sealed record KeyPress(string Key, bool Ctrl = false, bool Shift = false, bool Alt = false, bool Meta = false)
{
    /// <summary>Ctrl, or ⌘ on a Mac.</summary>
    public bool Command => Ctrl || Meta;
}

/// <summary>What an editor does for a shortcut.</summary>
public enum EditorAction
{
    /// <summary>Undo the last change (Ctrl+Z).</summary>
    Undo,
    /// <summary>Redo the last undone change (Ctrl+Y, Ctrl+Shift+Z).</summary>
    Redo,
    /// <summary>Delete the selection (Delete, Backspace).</summary>
    Delete,
    /// <summary>Stop what is going on: the tool's action, then the tool, then the selection (Escape).</summary>
    Cancel,
    /// <summary>Finish what the tool is drawing (Enter).</summary>
    Finish,
    /// <summary>Choose the tool with <see cref="EditorShortcut.Tool"/>'s number in the toolbar (1–9).</summary>
    Tool,
    /// <summary>Show the whole drawing (0).</summary>
    Fit,
    /// <summary>Move the selection by <see cref="EditorShortcut.Nudge"/> (arrow keys).</summary>
    Nudge,
}

/// <summary>A shortcut: what to do, the key it came from, and the tool number or the move it carries.</summary>
public sealed record EditorShortcut(EditorAction Action, string Key, int Tool = 0, Vector Nudge = default);

/// <summary>
/// The keyboard shortcuts shared by the editors (end profile, module, hall). Tools are chosen by their number in the
/// toolbar, so the keys are the same in every language. Arrow keys move the selection in drawing coordinates, with Y
/// up: <see cref="NudgeStep"/>, or <see cref="LargeNudgeStep"/> with Shift.
/// </summary>
public static class EditorShortcuts
{
    /// <summary>How far an arrow key moves the selection, mm.</summary>
    public const double NudgeStep = 1;

    /// <summary>How far an arrow key with Shift moves the selection, mm.</summary>
    public const double LargeNudgeStep = 10;

    /// <summary>The highest tool number a key chooses.</summary>
    public const int MaxTool = 9;

    /// <summary>The shortcut of <paramref name="press"/>; null for a key that isn't one.</summary>
    public static EditorShortcut? Map(KeyPress press)
    {
        ArgumentNullException.ThrowIfNull(press);
        var key = press.Key;
        if (press.Command && !press.Alt)
        {
            if (Is(key, "z")) return new(press.Shift ? EditorAction.Redo : EditorAction.Undo, key);
            if (Is(key, "y") && !press.Shift) return new(EditorAction.Redo, key);
            return null;
        }
        if (press.Alt || press.Meta) return null;
        var step = press.Shift ? LargeNudgeStep : NudgeStep;
        return key switch
        {
            "Delete" or "Backspace" when !press.Shift => new(EditorAction.Delete, key),
            "Escape" => new(EditorAction.Cancel, key),
            "Enter" when !press.Shift => new(EditorAction.Finish, key),
            "ArrowLeft" => new(EditorAction.Nudge, key, Nudge: new(-step, 0)),
            "ArrowRight" => new(EditorAction.Nudge, key, Nudge: new(step, 0)),
            "ArrowUp" => new(EditorAction.Nudge, key, Nudge: new(0, step)),
            "ArrowDown" => new(EditorAction.Nudge, key, Nudge: new(0, -step)),
            "0" when !press.Shift => new(EditorAction.Fit, key),
            [var digit] when !press.Shift && digit is >= '1' and <= '9' => new(EditorAction.Tool, key, Tool: digit - '0'),
            _ => null,
        };
    }

    /// <summary>The text that names a tool's key in its tooltip, e.g. "Select (1)"; the name alone beyond <see cref="MaxTool"/>.</summary>
    public static string WithKey(string name, int number) => number is >= 1 and <= MaxTool ? $"{name} ({number})" : name;

    private static bool Is(string key, string letter) => string.Equals(key, letter, StringComparison.OrdinalIgnoreCase);
}
