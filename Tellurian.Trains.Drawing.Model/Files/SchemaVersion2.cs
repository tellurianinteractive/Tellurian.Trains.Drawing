using System.Text.Json.Nodes;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// The migrations from schema version 1 to 2 (schemas/CHANGELOG.md), for tests that run every kind's migration. Every
/// file kind is read by the shared importers (<c>Importers.Drawing</c>), whose steps these call
/// (<c>DrawingMigrations</c>). Hall plans have no step: no released version of Module Draw made a hall plan before the
/// shape of version 2, so earlier hall plan files are refused.
/// </summary>
internal static class SchemaVersion2
{
    private const string EndProfiles = "endProfiles";
    private const string Standards = "standards";

    public static IReadOnlyList<DrawingFileMigration> Migrations { get; } =
    [
        new(DrawingFileKind.EndProfile, 1, content =>
        {
            EndProfile(content);
            return content;
        }),
        new(DrawingFileKind.Module, 1, content =>
        {
            Tellurian.Trains.Schedules.Importers.Drawing.DrawingMigrations.ModuleFileToVersion2(content);
            return content;
        }),
        new(DrawingFileKind.Catalogue, 1, content =>
        {
            Each(content, Standards, Standard);
            Each(content, EndProfiles, EndProfile);
            return content;
        }),
        new(DrawingFileKind.Hall, 1, content =>
        {
            Tellurian.Trains.Schedules.Importers.Drawing.DrawingMigrations.HallToVersion2(content);
            return content;
        }),
    ];

    private static void EndProfile(JsonNode node) => Tellurian.Trains.Schedules.Importers.Drawing.DrawingMigrations.EndProfileToVersion2(node);

    private static void Standard(JsonNode node) => Tellurian.Trains.Schedules.Importers.Drawing.DrawingMigrations.StandardToVersion2(node);

    /// <summary>Applies <paramref name="migrate"/> to every object in the array <paramref name="name"/> of <paramref name="node"/>.</summary>
    private static void Each(JsonNode? node, string name, Action<JsonNode> migrate)
    {
        if ((node as JsonObject)?[name] is not JsonArray items) return;
        foreach (var item in items.OfType<JsonObject>().ToList()) migrate(item);
    }
}
