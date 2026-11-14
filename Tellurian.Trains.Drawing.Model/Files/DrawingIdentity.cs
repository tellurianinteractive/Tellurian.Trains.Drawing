using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// The identity of a drawing in a file, read without reading the whole content: its kind, id, name and
/// revision (spec §6). Lists of drafts, recent files and collections use it. Catalogue files have no id.
/// </summary>
public sealed record DrawingIdentity(DrawingFileKind Kind, Guid? Id, string Name, int Revision)
{
    private const string Content = "content";

    /// <exception cref="DrawingFileException">The file isn't a drawing file.</exception>
    public static DrawingIdentity Read(string json)
    {
        var header = DrawingFileSerializer.ReadHeader(json);
        var content = JsonNode.Parse(json)?[Content] as JsonObject
            ?? throw new DrawingFileException(DrawingFileError.NotADrawingFile, "The file has no content.");
        var drawing = header.Kind == DrawingFileKind.Module ? content["module"] as JsonObject : content;
        try
        {
            return header.Kind switch
            {
                DrawingFileKind.Catalogue => new(header.Kind, null, Text(content, "version") ?? string.Empty, 0),
                _ => new(
                    header.Kind,
                    ReadId(drawing),
                    Text(drawing, header.Kind == DrawingFileKind.EndProfile ? "designation" : "name") ?? string.Empty,
                    (int?)drawing?["revision"] ?? 0),
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or JsonException)
        {
            throw new DrawingFileException(DrawingFileError.InvalidContent, $"The {header.Kind} file content is invalid: {ex.Message}", ex);
        }
    }

    private static Guid? ReadId(JsonObject? drawing) =>
        Text(drawing, "id") is { } id ? Guid.Parse(id) : null;

    private static string? Text(JsonObject? node, string name) => (string?)node?[name];
}
