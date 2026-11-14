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

    /// <summary>The file name extension, including the dot, e.g. <c>.tdmodule</c>.</summary>
    public static string Extension(DrawingFileKind kind) => kind switch
    {
        DrawingFileKind.EndProfile => ".tdendprofile",
        DrawingFileKind.Module => ".tdmodule",
        DrawingFileKind.Hall => ".tdhall",
        DrawingFileKind.HallPlan => ".tdhallplan",
        DrawingFileKind.Catalogue => ".tdcatalogue",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown file kind."),
    };

    /// <summary>The kind of file with <paramref name="extension"/> (with or without the dot), or null.</summary>
    public static DrawingFileKind? KindOf(string extension)
    {
        var normalized = extension.StartsWith('.') ? extension : $".{extension}";
        foreach (var kind in Enum.GetValues<DrawingFileKind>())
        {
            if (string.Equals(Extension(kind), normalized, StringComparison.OrdinalIgnoreCase)) return kind;
        }
        return null;
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
