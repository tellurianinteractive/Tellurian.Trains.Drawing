using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// Writes and reads drawing files (spec §6). Reading checks the envelope, refuses newer schema versions
/// and upgrades older ones through <see cref="DrawingFileMigrations"/>.
/// </summary>
public static class DrawingFileSerializer
{
    private const string FormatProperty = "format";
    private const string KindProperty = "kind";
    private const string SchemaVersionProperty = "schemaVersion";
    private const string AppVersionProperty = "appVersion";
    private const string ContentProperty = "content";

    private static DrawingJsonContext Context => DrawingJsonContext.Default;

    public static string Serialize(EndProfile endProfile, string? appVersion = null) =>
        JsonSerializer.Serialize(Envelope(DrawingFileKind.EndProfile, endProfile, appVersion), Context.DrawingFileEndProfile);

    public static string Serialize(ModuleFileContent module, string? appVersion = null) =>
        JsonSerializer.Serialize(Envelope(DrawingFileKind.Module, module, appVersion), Context.DrawingFileModuleFileContent);

    public static string Serialize(DrawingCatalogue catalogue, string? appVersion = null) =>
        JsonSerializer.Serialize(Envelope(DrawingFileKind.Catalogue, catalogue, appVersion), Context.DrawingFileDrawingCatalogue);

    /// <exception cref="DrawingFileException">The file isn't a drawing file.</exception>
    public static DrawingFileHeader ReadHeader(string json) => Header(Parse(json));

    /// <exception cref="DrawingFileException">The file isn't a drawing file.</exception>
    public static DrawingFileHeader ReadHeader(Stream stream) => Header(Parse(stream));

    /// <exception cref="DrawingFileException">The file can't be read as an end profile file.</exception>
    public static EndProfile DeserializeEndProfile(string json) => Read(Parse(json), DrawingFileKind.EndProfile, Context.DrawingFileEndProfile);

    /// <exception cref="DrawingFileException">The file can't be read as an end profile file.</exception>
    public static EndProfile DeserializeEndProfile(Stream stream) => Read(Parse(stream), DrawingFileKind.EndProfile, Context.DrawingFileEndProfile);

    /// <exception cref="DrawingFileException">The file can't be read as a module file.</exception>
    public static ModuleFileContent DeserializeModule(string json) => Read(Parse(json), DrawingFileKind.Module, Context.DrawingFileModuleFileContent);

    /// <exception cref="DrawingFileException">The file can't be read as a module file.</exception>
    public static ModuleFileContent DeserializeModule(Stream stream) => Read(Parse(stream), DrawingFileKind.Module, Context.DrawingFileModuleFileContent);

    /// <exception cref="DrawingFileException">The file can't be read as a catalogue file.</exception>
    public static DrawingCatalogue DeserializeCatalogue(string json) => Read(Parse(json), DrawingFileKind.Catalogue, Context.DrawingFileDrawingCatalogue);

    /// <exception cref="DrawingFileException">The file can't be read as a catalogue file.</exception>
    public static DrawingCatalogue DeserializeCatalogue(Stream stream) => Read(Parse(stream), DrawingFileKind.Catalogue, Context.DrawingFileDrawingCatalogue);

    /// <summary>
    /// Reads <paramref name="root"/> as a <paramref name="kind"/> file, migrating with <paramref name="migrations"/>
    /// up to <paramref name="currentVersion"/>; tests pass their own migrations.
    /// </summary>
    internal static T Read<T>(JsonNode root, DrawingFileKind kind, JsonTypeInfo<DrawingFile<T>> typeInfo, IReadOnlyList<DrawingFileMigration>? migrations = null, int? currentVersion = null)
    {
        var header = Header(root);
        if (header.Kind != kind)
            throw new DrawingFileException(DrawingFileError.WrongKind, $"The file is a {header.Kind} file, not a {kind} file.");
        var current = currentVersion ?? DrawingFileFormat.CurrentSchemaVersion(kind);
        if (header.SchemaVersion > current)
            throw new DrawingFileException(DrawingFileError.NewerSchemaVersion,
                $"The file has {kind} schema version {header.SchemaVersion}, but this version of the app reads up to version {current}. Update the app to open it.");

        var content = root[ContentProperty] ?? throw new DrawingFileException(DrawingFileError.NotADrawingFile, "The file has no content.");
        if (header.SchemaVersion < current)
        {
            root[ContentProperty] = DrawingFileMigrations.Upgrade(kind, content.DeepClone(), header.SchemaVersion, current, migrations ?? DrawingFileMigrations.All);
            root[SchemaVersionProperty] = current;
        }

        try
        {
            var file = root.Deserialize(typeInfo) ?? throw new DrawingFileException(DrawingFileError.InvalidContent, "The file is empty.");
            return file.Content;
        }
        catch (JsonException ex)
        {
            throw new DrawingFileException(DrawingFileError.InvalidContent, $"The {kind} file content is invalid: {ex.Message}", ex);
        }
    }

    private static DrawingFile<T> Envelope<T>(DrawingFileKind kind, T content, string? appVersion)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new()
        {
            Format = DrawingFileFormat.Format,
            Kind = kind,
            SchemaVersion = DrawingFileFormat.CurrentSchemaVersion(kind),
            AppVersion = appVersion ?? DrawingFileFormat.LibraryVersion,
            Content = content,
        };
    }

    private static DrawingFileHeader Header(JsonNode root)
    {
        if (root is not JsonObject envelope || (string?)Value(envelope, FormatProperty) != DrawingFileFormat.Format)
            throw new DrawingFileException(DrawingFileError.NotADrawingFile, $"The file isn't a Tellurian drawing file (\"{FormatProperty}\" must be \"{DrawingFileFormat.Format}\").");
        try
        {
            var kind = envelope[KindProperty].Deserialize(Context.DrawingFileKind);
            var version = (int?)Value(envelope, SchemaVersionProperty) ?? throw new DrawingFileException(DrawingFileError.NotADrawingFile, "The file has no schema version.");
            if (version < 1) throw new DrawingFileException(DrawingFileError.NotADrawingFile, $"The schema version {version} is invalid.");
            return new(kind, version, (string?)Value(envelope, AppVersionProperty));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            throw new DrawingFileException(DrawingFileError.NotADrawingFile, $"The file envelope is invalid: {ex.Message}", ex);
        }
    }

    private static JsonValue? Value(JsonObject envelope, string name) => envelope[name] as JsonValue;

    private static JsonNode Parse(string json) => Parse(() => JsonNode.Parse(json));

    private static JsonNode Parse(Stream stream) => Parse(() => JsonNode.Parse(stream));

    private static JsonNode Parse(Func<JsonNode?> parse)
    {
        try
        {
            return parse() ?? throw new DrawingFileException(DrawingFileError.NotADrawingFile, "The file is empty.");
        }
        catch (JsonException ex)
        {
            throw new DrawingFileException(DrawingFileError.InvalidJson, $"The file isn't valid JSON: {ex.Message}", ex);
        }
    }
}
