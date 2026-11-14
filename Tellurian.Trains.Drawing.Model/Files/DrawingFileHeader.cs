namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>The envelope of a drawing file without its content, to decide how to open it.</summary>
public sealed record DrawingFileHeader(DrawingFileKind Kind, int SchemaVersion, string? AppVersion)
{
    /// <summary>True if this library can read the file, possibly after migrating it.</summary>
    public bool IsSupported => SchemaVersion <= DrawingFileFormat.CurrentSchemaVersion(Kind);
}
