namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// The envelope of every drawing file (spec §6):
/// <c>{ "format": "tellurian-drawing", "kind": ..., "schemaVersion": n, "appVersion": "...", "content": {...} }</c>.
/// </summary>
public sealed record DrawingFile<TContent>
{
    /// <summary>Always <see cref="DrawingFileFormat.Format"/>.</summary>
    public required string Format { get; init; }

    public required DrawingFileKind Kind { get; init; }

    /// <summary>The major schema version of the content; changes within a version are backward compatible (EX-7).</summary>
    public required int SchemaVersion { get; init; }

    /// <summary>The version of the app or library that wrote the file; for information only.</summary>
    public string? AppVersion { get; init; }

    public required TContent Content { get; init; }
}
