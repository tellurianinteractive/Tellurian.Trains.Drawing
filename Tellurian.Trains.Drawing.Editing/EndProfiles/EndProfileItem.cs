namespace Tellurian.Trains.Drawing.Editing.EndProfiles;

/// <summary>
/// An item of an end profile drawing: a track, screw hole or dowel by its index, or the outline or ground line.
/// Indexes refer to the current document; a selection is cleared when items are added or removed.
/// </summary>
public readonly record struct EndProfileItem(EndProfileItemKind Kind, int Index = 0)
{
    public static EndProfileItem Outline => new(EndProfileItemKind.Outline);
    public static EndProfileItem GroundLine => new(EndProfileItemKind.GroundLine);
    public static EndProfileItem Track(int index) => new(EndProfileItemKind.Track, index);
    public static EndProfileItem ScrewHole(int index) => new(EndProfileItemKind.ScrewHole, index);
    public static EndProfileItem Dowel(int index) => new(EndProfileItemKind.Dowel, index);

    public override string ToString() => $"{Kind}:{Index}";

    /// <summary>Reads <see cref="ToString"/>'s text back, e.g. from a stored editor state.</summary>
    public static bool TryParse(string? text, out EndProfileItem item)
    {
        item = default;
        var parts = text?.Split(':');
        if (parts is not { Length: 2 } || !Enum.TryParse<EndProfileItemKind>(parts[0], out var kind) || !int.TryParse(parts[1], out var index)) return false;
        item = new(kind, index);
        return true;
    }
}
