using System.Reflection;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>Constants of the drawing file format (spec §6).</summary>
public static class DrawingFileFormat
{
    /// <summary>The value of <c>format</c> in every drawing file.</summary>
    public const string Format = "tellurian-drawing";

    /// <summary>The media type the app serves drawing files with.</summary>
    public const string MediaType = "application/json";

    /// <summary>The schema version this library writes for <paramref name="kind"/>.</summary>
    public static int CurrentSchemaVersion(DrawingFileKind kind) => kind switch
    {
        DrawingFileKind.EndProfile => 1,
        DrawingFileKind.Module => 1,
        DrawingFileKind.Catalogue => 1,
        DrawingFileKind.Hall => 1,
        DrawingFileKind.HallPlan => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown file kind."),
    };

    /// <summary>The extension of every drawing file: they are JSON files, readable in any JSON editor.</summary>
    public const string JsonExtension = ".json";

    /// <summary>
    /// The end of the file names this library suggests, including the dot, e.g. <c>.module.json</c>: the kind
    /// before <see cref="JsonExtension"/>, so the kind shows in a folder listing. The kind of a file is always
    /// read from its content, so a file with any name ending in <c>.json</c> can be opened.
    /// </summary>
    public static string Extension(DrawingFileKind kind) => kind switch
    {
        DrawingFileKind.EndProfile => ".endprofile.json",
        DrawingFileKind.Module => ".module.json",
        DrawingFileKind.Hall => ".hall.json",
        DrawingFileKind.HallPlan => ".hallplan.json",
        DrawingFileKind.Catalogue => ".catalogue.json",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown file kind."),
    };

    /// <summary>The extension used up to version 0.4, e.g. <c>.tdmodule</c>. Such files can still be opened.</summary>
    public static string LegacyExtension(DrawingFileKind kind) => kind switch
    {
        DrawingFileKind.EndProfile => ".tdendprofile",
        DrawingFileKind.Module => ".tdmodule",
        DrawingFileKind.Hall => ".tdhall",
        DrawingFileKind.HallPlan => ".tdhallplan",
        DrawingFileKind.Catalogue => ".tdcatalogue",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown file kind."),
    };

    /// <summary>
    /// The kind a file name (or an extension, with or without the dot) says, by <see cref="Extension"/> or
    /// <see cref="LegacyExtension"/>; null when it doesn't say, e.g. a plain <c>.json</c> name.
    /// </summary>
    public static DrawingFileKind? KindOf(string name)
    {
        var normalized = name.StartsWith('.') ? name : $".{name}";
        foreach (var kind in Enum.GetValues<DrawingFileKind>())
        {
            if (normalized.EndsWith(Extension(kind), StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith(LegacyExtension(kind), StringComparison.OrdinalIgnoreCase)) return kind;
        }
        return null;
    }

    /// <summary>True if a file with this name may be a drawing file: a JSON file, or one with a legacy extension.</summary>
    public static bool MayBeDrawingFile(string fileName) =>
        fileName.EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase) || KindOf(fileName) is not null;

    /// <summary>The file name with a legacy extension replaced by <see cref="Extension"/>; other names are kept.</summary>
    public static string WithCurrentExtension(string fileName, DrawingFileKind kind)
    {
        var legacy = LegacyExtension(kind);
        return fileName.EndsWith(legacy, StringComparison.OrdinalIgnoreCase) ? $"{fileName[..^legacy.Length]}{Extension(kind)}" : fileName;
    }

    /// <summary>The version of this library, written as <c>appVersion</c> when the caller gives none.</summary>
    public static string LibraryVersion { get; } = ReadLibraryVersion();

    private static string ReadLibraryVersion()
    {
        var version = typeof(DrawingFileFormat).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        var metadata = version.IndexOf('+');
        return metadata < 0 ? version : version[..metadata];
    }
}
