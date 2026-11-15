using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// JSON Schemas of the drawing files (EX-7), generated from the same metadata the serializer uses,
/// so they can't drift from the code. The published copies are in <c>docs/schema/</c>.
/// </summary>
public static class DrawingFileSchema
{
    public const string SchemaDialect = "https://json-schema.org/draft/2020-12/schema";

    private const string Number = "number";
    private const string Object = "object";
    private const string Null = "null";
    private const string Definitions = "$defs";
    private const string ModelNamespace = "Tellurian.Trains.Drawing";

    /// <summary>The kinds that have a schema: all of them.</summary>
    public static IReadOnlyList<DrawingFileKind> Kinds { get; } = Enum.GetValues<DrawingFileKind>();

    /// <summary>The published file name, e.g. <c>module.v1.json</c>.</summary>
    public static string FileName(DrawingFileKind kind) =>
        $"{kind.ToString().ToLowerInvariant()}.v{DrawingFileFormat.CurrentSchemaVersion(kind)}.json";

    /// <exception cref="NotSupportedException">The kind is unknown.</exception>
    public static JsonObject Create(DrawingFileKind kind)
    {
        var context = DrawingJsonContext.Default;
        JsonTypeInfo typeInfo = kind switch
        {
            DrawingFileKind.EndProfile => context.DrawingFileEndProfile,
            DrawingFileKind.Module => context.DrawingFileModuleFileContent,
            DrawingFileKind.Catalogue => context.DrawingFileDrawingCatalogue,
            DrawingFileKind.Hall => context.DrawingFileHall,
            DrawingFileKind.HallPlan => context.DrawingFileHallPlan,
            _ => throw new NotSupportedException($"There is no schema for {kind} files."),
        };
        var definitions = new SortedDictionary<string, JsonNode>(StringComparer.Ordinal);
        var exporterOptions = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = (exporterContext, node) => Reference(exporterContext, Transform(exporterContext, node), definitions),
        };
        var generated = typeInfo.GetJsonSchemaAsNode(exporterOptions).AsObject();

        var properties = generated["properties"]!.AsObject();
        properties["format"] = new JsonObject { ["const"] = DrawingFileFormat.Format };
        properties["kind"] = new JsonObject { ["const"] = JsonNamingPolicy.CamelCase.ConvertName(kind.ToString()) };
        properties["schemaVersion"] = new JsonObject { ["const"] = DrawingFileFormat.CurrentSchemaVersion(kind) };
        properties["content"] = NonNullable(properties["content"]!);

        var schema = new JsonObject
        {
            ["$schema"] = SchemaDialect,
            ["title"] = $"Tellurian Trains Drawing {kind} file",
            ["description"] = $"A {DrawingFileFormat.Extension(kind)} file, schema version {DrawingFileFormat.CurrentSchemaVersion(kind)}. Lengths in millimetres, angles in degrees.",
        };
        foreach (var (name, value) in generated.ToList())
        {
            generated.Remove(name);
            schema[name] = value;
        }
        schema[Definitions] = new JsonObject(definitions.Select(d => KeyValuePair.Create(d.Key, (JsonNode?)d.Value)));
        return schema;
    }

    private static JsonNode Transform(JsonSchemaExporterContext context, JsonNode schema)
    {
        var type = context.TypeInfo.Type;
        if (type == typeof(Angle)) return new JsonObject { ["type"] = Number, ["description"] = "Degrees, counter-clockwise from the positive X axis." };
        if (type == typeof(Point)) return Coordinates(PoseJsonConverter.X, PoseJsonConverter.Y);
        if (type == typeof(Pose)) return Coordinates(PoseJsonConverter.X, PoseJsonConverter.Y, PoseJsonConverter.Heading);
        return schema;
    }

    /// <summary>
    /// Moves the schema of a named model type to <c>$defs</c> and refers to it, so that shared types
    /// are described once. The envelope and derived types of polymorphic hierarchies stay inline.
    /// </summary>
    private static JsonNode Reference(JsonSchemaExporterContext context, JsonNode schema, IDictionary<string, JsonNode> definitions)
    {
        var type = context.TypeInfo.Type;
        var isModelType = type.Namespace?.StartsWith(ModelNamespace, StringComparison.Ordinal) == true && !type.IsGenericType && !type.IsEnum;
        if (!isModelType || context.BaseTypeInfo is not null || schema is not JsonObject body) return schema;

        var isNullable = IsNullable(body);
        definitions.TryAdd(type.Name, NonNullable(body));
        var reference = new JsonObject { ["$ref"] = $"#/{Definitions}/{type.Name}" };
        return isNullable ? new JsonObject { ["anyOf"] = new JsonArray(reference, new JsonObject { ["type"] = Null }) } : reference;
    }

    private static bool IsNullable(JsonObject schema) =>
        schema["type"] is JsonArray types && types.Any(t => (string?)t == Null);

    /// <summary>The schema without null as an allowed value; returns a copy.</summary>
    private static JsonNode NonNullable(JsonNode schema)
    {
        if (schema["anyOf"] is JsonArray choices && choices.Count == 2 && choices.Any(c => (string?)c?["type"] == Null))
            return choices.First(c => (string?)c?["type"] != Null)!.DeepClone();
        schema = schema.DeepClone();
        if (schema is JsonObject body && body["type"] is JsonArray types)
        {
            var remaining = types.Select(t => (string?)t).Where(t => t != Null).ToList();
            body["type"] = remaining.Count == 1 ? remaining[0] : new JsonArray([.. remaining.Select(t => (JsonNode?)t)]);
        }
        return schema;
    }

    private static JsonObject Coordinates(params string[] names) => new()
    {
        ["type"] = Object,
        ["properties"] = new JsonObject(names.Select(n => KeyValuePair.Create(n, (JsonNode?)new JsonObject { ["type"] = Number }))),
        ["required"] = new JsonArray([.. names.Select(n => (JsonNode)n)]),
    };
}
