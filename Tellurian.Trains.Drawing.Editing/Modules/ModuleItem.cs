namespace Tellurian.Trains.Drawing.Editing.Modules;

/// <summary>The kinds of selectable items in the module editor.</summary>
public enum ModuleItemKind
{
    /// <summary>A section joint as a whole.</summary>
    Joint,

    /// <summary>A point of a section joint: 0 its start, the last its end, the others its bends.</summary>
    JointPoint,
}

/// <summary>
/// An item of the module editor: a section joint by its index in the edited drawing, or one of its points. Indexes
/// refer to the current document; a selection is cleared when items are added or removed.
/// </summary>
public readonly record struct ModuleItem(ModuleItemKind Kind, int Joint, int Point = 0)
{
    public static ModuleItem OfJoint(int joint) => new(ModuleItemKind.Joint, joint);
    public static ModuleItem OfPoint(int joint, int point) => new(ModuleItemKind.JointPoint, joint, point);

    public override string ToString() => Kind == ModuleItemKind.JointPoint ? $"{Kind}:{Joint}:{Point}" : $"{Kind}:{Joint}";

    /// <summary>Reads <see cref="ToString"/>'s text back, e.g. from a stored editor state.</summary>
    public static bool TryParse(string? text, out ModuleItem item)
    {
        item = default;
        var parts = text?.Split(':');
        if (parts is not { Length: 2 or 3 } || !Enum.TryParse<ModuleItemKind>(parts[0], out var kind) || !int.TryParse(parts[1], out var joint)) return false;
        var point = 0;
        if (parts.Length == 3 && !int.TryParse(parts[2], out point)) return false;
        item = new(kind, joint, point);
        return true;
    }
}
