using System.Text.Json;
using System.Text.Json.Nodes;
using Tellurian.Trains.Schedules.Model.Layouts;

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
                DrawingFileKind.Module => new(header.Kind, ReadId(drawing), ModuleTitle(drawing), ReadRevision(drawing)),
                _ => new(
                    header.Kind,
                    ReadId(drawing),
                    Text(drawing, header.Kind == DrawingFileKind.EndProfile ? "designation" : "name") ?? string.Empty,
                    ReadRevision(drawing)),
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or JsonException)
        {
            throw new DrawingFileException(DrawingFileError.InvalidContent, $"The {header.Kind} file content is invalid: {ex.Message}", ex);
        }
    }

    private static Guid? ReadId(JsonObject? drawing) =>
        Text(drawing, "id") is { } id ? Guid.Parse(id) : null;

    private static int ReadRevision(JsonObject? drawing) => (int?)drawing?["revision"] ?? 0;

    /// <summary>
    /// The module's title in the shared model (<see cref="ModuleExtensions"/>): the designation and the name. A file
    /// that isn't upgraded yet has the number as <c>fremoNumber</c> and the prefix on the first of its <c>owners</c>.
    /// </summary>
    private static string ModuleTitle(JsonObject? module) =>
        new Module
        {
            Name = Text(module, "name") ?? string.Empty,
            Prefix = Text(module, "prefix") ?? Text((module?["owners"] as JsonArray)?.FirstOrDefault() as JsonObject, "prefix"),
            Number = (int?)(module?["number"] ?? module?["fremoNumber"]),
            ConfigurationLabel = Text(module, "configurationLabel"),
            OperationLocation = module?["operationLocation"] is JsonObject location && Text(location, "name") is { } locationName
                ? Named(locationName)
                : null,
        }.Title;

    /// <summary>A location with only a name, which is all a title needs.</summary>
    private static OperationLocation Named(string name)
    {
        var location = OperationLocation.Create(OperationLocationKind.Station);
        location.Name = name;
        return location;
    }

    private static string? Text(JsonObject? node, string name) => (string?)node?[name];
}
