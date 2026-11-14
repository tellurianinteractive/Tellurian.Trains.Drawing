using System.Text.Json.Nodes;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// Upgrades the content of a <paramref name="Kind"/> file from schema version <paramref name="FromVersion"/>
/// to the next version (spec §6). Migrations work on the JSON, so old model types need not be kept.
/// </summary>
public sealed record DrawingFileMigration(DrawingFileKind Kind, int FromVersion, Func<JsonNode, JsonNode> Migrate);
