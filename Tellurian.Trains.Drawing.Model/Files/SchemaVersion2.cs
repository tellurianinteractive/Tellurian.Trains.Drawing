using System.Text.Json.Nodes;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// The migrations from schema version 1 to 2 (schemas/CHANGELOG.md) of what Module Draw still reads itself: the hall
/// plan. End profiles, catalogues, halls and modules are read by the shared importers (<c>Importers.Drawing</c>), and
/// the hall plan's embedded module, end profiles, standards and hall are migrated by their steps there
/// (<c>DrawingMigrations</c>). Here: a hall plan refers to exits by side and number, its lines are lists of modules that
/// are turned or not, and its references to obstacles follow their new numbers. The steps of the other kinds are here
/// only for tests that run every kind's migration.
/// <para>
/// Every step is idempotent: it finds the earlier shape by its properties and leaves a file that already has the
/// shape of today as it is, since the steps are also run on version 2 files written by development builds
/// (<see cref="DrawingFileMigrations.UpgradeDraft"/>).
/// </para>
/// </summary>
internal static class SchemaVersion2
{
    private const string Id = "id";
    private const string Direction = "direction";
    private const string Exits = "exits";
    private const string EndProfiles = "endProfiles";
    private const string Standards = "standards";

    private const string Drawing = "drawing";
    private const string Name = "name";
    private const string Number = "number";
    private const string ObstacleId = "obstacleId";

    private static readonly string Left = nameof(ExitSide.Left);
    private static readonly string Right = nameof(ExitSide.Right);
    private static readonly string Undefined = nameof(ExitSide.Undefined);


    public static IReadOnlyList<DrawingFileMigration> Migrations { get; } =
    [
        new(DrawingFileKind.EndProfile, 1, content =>
        {
            EndProfile(content);
            return content;
        }),
        // The shared model reads module files; this step is for tests that run every kind's migration.
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
        // The shared model reads hall files; this step is for tests that run every kind's migration.
        new(DrawingFileKind.Hall, 1, content =>
        {
            Tellurian.Trains.Schedules.Importers.Drawing.DrawingMigrations.HallToVersion2(content);
            return content;
        }),
        new(DrawingFileKind.HallPlan, 1, content =>
        {
            HallPlanHall(content);
            Each(content, "modules", Tellurian.Trains.Schedules.Importers.Drawing.DrawingMigrations.ModuleToVersion2);
            Each(content, EndProfiles, EndProfile);
            Each(content, Standards, Standard);
            HallPlanExits(content);
            return content;
        }),
    ];









    /// <summary>
    /// A hall plan refers to an exit by its side and number, in place of its label, and a line is a list of modules
    /// that are turned or not, in place of an entry and an exit label per module. The embedded modules, already
    /// migrated, tell the side and number of each label. Not turned means that the left exit joins the previous
    /// module; the exit the line continues from is kept only where it isn't the first one on the other side.
    /// </summary>
    private static void HallPlanExits(JsonNode content)
    {
        var places = new Dictionary<(string Module, string Label), (string Side, int Number)>();
        Each(content, "modules", module =>
        {
            var counts = new Dictionary<string, int>();
            Each(module[Drawing], Exits, exit =>
            {
                var side = EnumName(Text(exit[Direction])) ?? Undefined;
                counts[side] = counts.GetValueOrDefault(side) + 1;
                if (Text(module[Id]) is { } id && Text(exit["label"]) is { } label) places.TryAdd((id, label), (side, counts[side]));
            });
        });

        (string Side, int Number)? Place(string? moduleId, JsonNode? label) =>
            moduleId is not null && Text(label) is { } text && places.TryGetValue((moduleId, text), out var place) ? place : null;

        static void Write(JsonObject target, (string Side, int Number) place)
        {
            target["side"] = place.Side;
            target["number"] = place.Number;
        }

        void Reference(JsonNode? node)
        {
            if (node is not JsonObject reference || !reference.Remove("exitLabel", out var label)) return;
            Write(reference, Place(Text(reference["moduleId"]), label) ?? (Undefined, 1));
        }

        Each(content, "joints", joint =>
        {
            Reference(joint["first"]);
            Reference(joint["second"]);
        });
        Each(content, "layouts", layout =>
        {
            if (Property(Property(layout, "anchorDimensions"), "referencePoint") is JsonObject point && point.Remove("exitLabel", out var anchorLabel)
                && Place(Text(layout["anchorModuleId"]), anchorLabel) is { } anchorExit)
            {
                var exit = new JsonObject();
                Write(exit, anchorExit);
                point["exit"] = exit;
            }
            Each(layout, "lines", line =>
            {
                Reference(line["startAt"]);
                Reference(line["endAt"]);
                Each(line, "entries", item =>
                {
                    var entry = (JsonObject)item;
                    entry.Remove("variantId");
                    var hadEntry = entry.Remove("entryExit", out var entryLabel);
                    var hadExit = entry.Remove("exitExit", out var exitLabel);
                    if (!hadEntry && !hadExit) return;
                    var moduleId = Text(entry["moduleId"]);
                    var joinsPrevious = Place(moduleId, entryLabel);
                    var joinsNext = Place(moduleId, exitLabel);
                    var isTurned = joinsPrevious is { } previous ? previous.Side == Right : joinsNext?.Side == Left;
                    if (isTurned) entry["isTurned"] = true;
                    if (joinsNext is { } next && next != (isTurned ? Left : Right, 1))
                    {
                        var continuesFrom = new JsonObject();
                        Write(continuesFrom, next);
                        entry["continuesFrom"] = continuesFrom;
                    }
                });
            });
        });
    }

    /// <summary>
    /// The embedded hall is migrated as a hall file is: its obstacles, doors and no-go zones get whole numbers as ids.
    /// The anchor dimensions that refer to an obstacle get its new id; a reference to an obstacle that isn't in the hall
    /// is removed from the reference, which then refers to nothing, as before.
    /// </summary>
    private static void HallPlanHall(JsonNode content)
    {
        var ids = Tellurian.Trains.Schedules.Importers.Drawing.DrawingMigrations.HallToVersion2(Property(content, "hall"));
        void Reference(JsonNode? node)
        {
            if (node is not JsonObject reference || reference[ObstacleId] is not JsonValue value || !value.TryGetValue<string>(out var id)) return;
            if (ids.TryGetValue(id, out var number)) reference[ObstacleId] = number;
            else reference.Remove(ObstacleId);
        }
        Each(content, "layouts", layout =>
        {
            var dimensions = Property(layout, "anchorDimensions");
            Reference(Property(dimensions, "first"));
            Reference(Property(dimensions, "second"));
        });
    }

    private static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static void EndProfile(JsonNode node) => Tellurian.Trains.Schedules.Importers.Drawing.DrawingMigrations.EndProfileToVersion2(node);

    private static void Standard(JsonNode node) => Tellurian.Trains.Schedules.Importers.Drawing.DrawingMigrations.StandardToVersion2(node);


    /// <summary>
    /// A one-word enum value as it is written now, with a capital first letter: <c>left</c> becomes <c>Left</c>.
    /// </summary>
    private static string? EnumName(string? value) =>
        string.IsNullOrEmpty(value) ? value : string.Concat(char.ToUpperInvariant(value[0]).ToString(), value.AsSpan(1));



    private static JsonNode? Property(JsonNode? node, string name) => (node as JsonObject)?[name];

    /// <summary>Applies <paramref name="migrate"/> to every object in the array <paramref name="name"/> of <paramref name="node"/>.</summary>
    private static void Each(JsonNode? node, string name, Action<JsonNode> migrate)
    {
        if (Property(node, name) is not JsonArray items) return;
        foreach (var item in items.OfType<JsonObject>().ToList()) migrate(item);
    }
}
