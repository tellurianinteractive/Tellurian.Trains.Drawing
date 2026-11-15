namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>A file to import: its name (or path within an archive) and how to read its text.</summary>
public sealed record ImportSource(string Name, Func<string> ReadText)
{
    /// <summary>A file whose text is already read.</summary>
    public static ImportSource FromText(string name, string text) => new(name, () => text);
}
