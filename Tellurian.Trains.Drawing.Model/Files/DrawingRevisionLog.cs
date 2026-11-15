using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// The revision log of a drawing (EX-10): one entry per person who saved it, with the last date they did,
/// newest first. Recorded when a drawing is saved to a file; editors don't change it. Catalogue files have no log.
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
    public static string Record(string json, string name, DateOnly date) =>
        string.IsNullOrWhiteSpace(name) || DrawingFileSerializer.ReadHeader(json).Kind == DrawingFileKind.Catalogue
            ? json
            : WithLog(json, Record(Read(json), name, date));

    /// <summary>The log in a drawing file; empty for catalogue files and drawings without a log.</summary>
    /// <exception cref="DrawingFileException">The file isn't a drawing file.</exception>
    public static IReadOnlyList<DrawingRevision> Read(string json)
    {
        if (DrawingFileSerializer.ReadHeader(json).Kind == DrawingFileKind.Catalogue) return [];
        var (_, drawing) = Drawing(json);
        return drawing[LogProperty]?.Deserialize(DrawingJsonContext.Default.IReadOnlyListDrawingRevision) ?? [];
    }

    /// <summary>
    /// <paramref name="json"/> with the log of <paramref name="logSource"/>, so a changed drawing from an editor
    /// keeps what saving to a file has recorded. Catalogue files are returned as they are.
    /// </summary>
    /// <exception cref="DrawingFileException">A file isn't a drawing file.</exception>
    public static string KeepLog(string json, string logSource) =>
        DrawingFileSerializer.ReadHeader(json).Kind == DrawingFileKind.Catalogue ? json : WithLog(json, Read(logSource));

    private static string WithLog(string json, IReadOnlyList<DrawingRevision> log)
    {
        var (file, drawing) = Drawing(json);
        drawing[LogProperty] = JsonSerializer.SerializeToNode(log, DrawingJsonContext.Default.IReadOnlyListDrawingRevision);
        return file.ToJsonString(DrawingJsonContext.Default.Options);
    }

    private static (JsonObject File, JsonObject Drawing) Drawing(string json)
    {
        var header = DrawingFileSerializer.ReadHeader(json);
        var file = JsonNode.Parse(json)!.AsObject();
        var content = file[Content] as JsonObject ?? throw new DrawingFileException(DrawingFileError.NotADrawingFile, "The file has no content.");
        var drawing = header.Kind == DrawingFileKind.Module ? content[ModuleProperty] as JsonObject : content;
        return (file, drawing ?? throw new DrawingFileException(DrawingFileError.InvalidContent, $"The {header.Kind} file has no drawing."));
    }
}
