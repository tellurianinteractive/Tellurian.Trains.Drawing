using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// The revision log of a drawing: one entry per person who saved it, with the last date they did,
/// newest first. Recorded when a drawing is saved; catalogue files have no log.
/// </summary>
public static class DrawingRevisionLog
{
    private const string Content = "content";
    private const string ModuleProperty = "module";
    private const string LogProperty = "revisionLog";

    /// <summary>The log with <paramref name="name"/> saving on <paramref name="date"/>: the person's earlier entry is replaced.</summary>
    public static IReadOnlyList<DrawingRevision> Record(IReadOnlyList<DrawingRevision> log, string name, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(log);
        var trimmed = name.Trim();
        if (trimmed.Length == 0) return log;
        return
        [
            new DrawingRevision { Name = trimmed, Date = date },
            .. log.Where(r => !string.Equals(r.Name.Trim(), trimmed, StringComparison.OrdinalIgnoreCase)).OrderByDescending(r => r.Date),
        ];
    }

    /// <summary>
    /// The drawing file with the save recorded in its log, for any kind of drawing; catalogue files, and an empty
    /// <paramref name="name"/>, leave the file as it is.
    /// </summary>
    /// <exception cref="DrawingFileException">The file isn't a drawing file.</exception>
    public static string Record(string json, string name, DateOnly date)
    {
        var header = DrawingFileSerializer.ReadHeader(json);
        if (header.Kind == DrawingFileKind.Catalogue || string.IsNullOrWhiteSpace(name)) return json;
        var file = JsonNode.Parse(json)!.AsObject();
        var content = file[Content] as JsonObject ?? throw new DrawingFileException(DrawingFileError.NotADrawingFile, "The file has no content.");
        var drawing = header.Kind == DrawingFileKind.Module ? content[ModuleProperty] as JsonObject : content;
        if (drawing is null) throw new DrawingFileException(DrawingFileError.InvalidContent, $"The {header.Kind} file has no drawing.");
        var log = drawing[LogProperty]?.Deserialize(DrawingJsonContext.Default.IReadOnlyListDrawingRevision) ?? [];
        drawing[LogProperty] = JsonSerializer.SerializeToNode(Record(log, name, date), DrawingJsonContext.Default.IReadOnlyListDrawingRevision);
        return file.ToJsonString(DrawingJsonContext.Default.Options);
    }
}
