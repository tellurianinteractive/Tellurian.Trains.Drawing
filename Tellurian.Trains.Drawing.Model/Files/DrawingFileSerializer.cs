using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Tellurian.Trains.Drawing.Model.Catalogues;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Schedules.Exporters.Drawing;
using Tellurian.Trains.Schedules.Exporters.Interfaces;
using Tellurian.Trains.Schedules.Importers.Drawing;
using Tellurian.Trains.Schedules.Importers.Interfaces;

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

    /// <summary>
    /// Why <paramref name="json"/> can't be read as the drawing file its header says it is, or null when it can: e.g. a
    /// version 2 file that a development build wrote in an earlier shape.
    /// </summary>
    public static string? ProblemOf(string json)
    {
        try
        {
            _ = Written(json, appVersion: null);
            return null;
        }
        catch (DrawingFileException ex)
        {
            return ex.Message;
        }
    }

    /// <summary>
    /// <paramref name="json"/> at the current schema version: a file of an earlier version is read, migrated and written
    /// again in today's shape; a file of the current version is returned as it is.
    /// </summary>
    /// <exception cref="DrawingFileException">The file can't be read.</exception>
    public static string AtCurrentVersion(string json, string? appVersion = null)
    {
        var header = ReadHeader(json);
        return header.SchemaVersion < DrawingFileFormat.CurrentSchemaVersion(header.Kind) ? Written(json, appVersion) : json;
    }

    /// <summary><paramref name="json"/> read as the kind its header says and written again.</summary>
    private static string Written(string json, string? appVersion) => ReadHeader(json).Kind switch
    {
        DrawingFileKind.EndProfile => Serialize(DeserializeEndProfile(json), appVersion),
        DrawingFileKind.Module => Serialize(DeserializeModule(json), appVersion),
        DrawingFileKind.Catalogue => Serialize(DeserializeCatalogue(json), appVersion),
        DrawingFileKind.Hall => Serialize(DeserializeHall(json), appVersion),
        DrawingFileKind.HallPlan => Serialize(DeserializeHallPlan(json), appVersion),
        var kind => throw new DrawingFileException(DrawingFileError.WrongKind, $"There is no {kind} file."),
    };

    /// <summary>The end profile file of <paramref name="endProfile"/>, written by the shared <see cref="EndProfileExporter"/>.</summary>
    public static string Serialize(EndProfile endProfile, string? appVersion = null) =>
        EndProfiles.Write(endProfile, new ExportOptions(appVersion ?? ModuleDrawVersion.Current));

    /// <summary>The module file of <paramref name="module"/>, written by the shared <see cref="ModuleExporter"/>.</summary>
    public static string Serialize(ModuleFileContent module, string? appVersion = null) =>
        new ModuleExporter().Write(module, new ExportOptions(appVersion ?? ModuleDrawVersion.Current));

    /// <summary>The catalogue file of <paramref name="catalogue"/>, written by the shared <see cref="CatalogueExporter"/>.</summary>
    public static string Serialize(Catalogue catalogue, string? appVersion = null) =>
        new CatalogueExporter().Write(catalogue, new ExportOptions(appVersion ?? ModuleDrawVersion.Current));

    /// <summary>The hall file of <paramref name="hall"/>, written by the shared <see cref="HallExporter"/>.</summary>
    public static string Serialize(Hall hall, string? appVersion = null) =>
        new HallExporter().Write(hall, new ExportOptions(appVersion ?? ModuleDrawVersion.Current));

    /// <summary>The hall plan file of <paramref name="hallPlan"/>, written by the shared <see cref="HallPlanExporter"/>.</summary>
    public static string Serialize(HallPlan hallPlan, string? appVersion = null) =>
        new HallPlanExporter().Write(hallPlan, new ExportOptions(appVersion ?? ModuleDrawVersion.Current));

    /// <exception cref="DrawingFileException">The file isn't a drawing file.</exception>
    public static DrawingFileHeader ReadHeader(string json) => Header(Parse(json));

    /// <exception cref="DrawingFileException">The file isn't a drawing file.</exception>
    public static DrawingFileHeader ReadHeader(Stream stream) => Header(Parse(stream));

    /// <exception cref="DrawingFileException">The file can't be read as an end profile file.</exception>
    public static EndProfile DeserializeEndProfile(string json) => Imported(new EndProfileImporter().Import(json));

    /// <exception cref="DrawingFileException">The file can't be read as an end profile file.</exception>
    public static EndProfile DeserializeEndProfile(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream, leaveOpen: true);
        return DeserializeEndProfile(reader.ReadToEnd());
    }

    /// <exception cref="DrawingFileException">The file can't be read as a module file.</exception>
    public static ModuleFileContent DeserializeModule(string json) => Imported(new ModuleImporter().Import(json));

    /// <exception cref="DrawingFileException">The file can't be read as a module file.</exception>
    public static ModuleFileContent DeserializeModule(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream, leaveOpen: true);
        return DeserializeModule(reader.ReadToEnd());
    }

    /// <exception cref="DrawingFileException">The file can't be read as a catalogue file.</exception>
    public static Catalogue DeserializeCatalogue(string json) => Imported(new CatalogueImporter().Import(json));

    /// <exception cref="DrawingFileException">The file can't be read as a catalogue file.</exception>
    public static Catalogue DeserializeCatalogue(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream, leaveOpen: true);
        return DeserializeCatalogue(reader.ReadToEnd());
    }

    /// <exception cref="DrawingFileException">The file can't be read as a hall file.</exception>
    public static Hall DeserializeHall(string json) => Imported(new HallImporter().Import(json));

    /// <exception cref="DrawingFileException">The file can't be read as a hall file.</exception>
    public static Hall DeserializeHall(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream, leaveOpen: true);
        return DeserializeHall(reader.ReadToEnd());
    }

    /// <exception cref="DrawingFileException">The file can't be read as a hall plan file.</exception>
    public static HallPlan DeserializeHallPlan(string json) => Imported(new HallPlanImporter().Import(json));

    /// <exception cref="DrawingFileException">The file can't be read as a hall plan file.</exception>
    public static HallPlan DeserializeHallPlan(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream, leaveOpen: true);
        return DeserializeHallPlan(reader.ReadToEnd());
    }

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
        try
        {
            if (header.SchemaVersion < current)
            {
                root[ContentProperty] = DrawingFileMigrations.Upgrade(kind, content.DeepClone(), header.SchemaVersion, current, migrations ?? DrawingFileMigrations.All);
                root[SchemaVersionProperty] = current;
            }

            // Operation locations of earlier files: the kind first and named as in the code, as the shared model reads it.
            Tellurian.Trains.Schedules.Model.Layouts.OperationLocationJson.Normalise(root[ContentProperty]);
            var file = root.Deserialize(typeInfo) ?? throw new DrawingFileException(DrawingFileError.InvalidContent, "The file is empty.");
            return file.Content;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            throw new DrawingFileException(DrawingFileError.InvalidContent, $"The {kind} file content is invalid: {ex.Message}", ex);
        }
    }

    private static readonly EndProfileExporter EndProfiles = new();

    /// <summary>The item of a successful import, or the failure as a <see cref="DrawingFileException"/>, as callers here expect.</summary>
    private static T Imported<T>(ImportResult<T> result)
    {
        if (result.IsSuccess) return result.Item;
        var message = result.Messages.FirstOrDefault();
        throw new DrawingFileException(message?.Code switch
        {
            FileErrorCode.InvalidJson => DrawingFileError.InvalidJson,
            FileErrorCode.WrongKind => DrawingFileError.WrongKind,
            FileErrorCode.NewerVersion => DrawingFileError.NewerSchemaVersion,
            FileErrorCode.InvalidContent or FileErrorCode.MigrationMissing => DrawingFileError.InvalidContent,
            _ => DrawingFileError.NotADrawingFile,
        }, message?.Text ?? "The file can't be read.");
    }

    private static DrawingFile<T> Envelope<T>(DrawingFileKind kind, T content, string? appVersion)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new()
        {
            Format = DrawingFileFormat.Format,
            Kind = kind,
            SchemaVersion = DrawingFileFormat.CurrentSchemaVersion(kind),
            AppVersion = appVersion ?? ModuleDrawVersion.Current,
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
