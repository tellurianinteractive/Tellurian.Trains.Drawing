using System.Text.Json.Nodes;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>The migration pipeline: older schema versions are upgraded step by step on load (spec §6).</summary>
public static class DrawingFileMigrations
{
    /// <summary>All migrations of this library: from version 1 to 2 for every kind.</summary>
    public static IReadOnlyList<DrawingFileMigration> All { get; } = [.. SchemaVersion2.Migrations];

    /// <summary>
    /// Brings a version 2 file written by a development build to the shape version 2 has now, by running the step
    /// from version 1 again; the steps leave what is already changed as it is. Schema version 2 isn't released, and its
    /// shape has changed while it was made. To be removed when version 2 is released.
    /// </summary>
    public static JsonNode UpgradeDraft(DrawingFileKind kind, JsonNode content, int version) =>
        version == DraftVersion && All.FirstOrDefault(m => m.Kind == kind && m.FromVersion == DraftVersion - 1) is { } step ? step.Migrate(content) : content;

    private const int DraftVersion = 2;

    /// <summary>
    /// Upgrades <paramref name="content"/> from <paramref name="fromVersion"/> to <paramref name="toVersion"/>
    /// using <paramref name="migrations"/>.
    /// </summary>
    /// <exception cref="DrawingFileException">A migration step is missing.</exception>
    public static JsonNode Upgrade(DrawingFileKind kind, JsonNode content, int fromVersion, int toVersion, IReadOnlyList<DrawingFileMigration> migrations)
    {
        for (var version = fromVersion; version < toVersion; version++)
        {
            var step = migrations.FirstOrDefault(m => m.Kind == kind && m.FromVersion == version)
                ?? throw new DrawingFileException(DrawingFileError.InvalidContent, $"There is no migration of {kind} files from schema version {version}.");
            content = step.Migrate(content);
        }
        return content;
    }
}
