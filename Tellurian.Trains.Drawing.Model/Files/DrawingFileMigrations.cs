using System.Text.Json.Nodes;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>The migration pipeline: older schema versions are upgraded step by step on load (spec §6).</summary>
public static class DrawingFileMigrations
{
    /// <summary>All migrations of this library. Version 1 is the first, so there are none yet.</summary>
    public static IReadOnlyList<DrawingFileMigration> All { get; } = [];

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
