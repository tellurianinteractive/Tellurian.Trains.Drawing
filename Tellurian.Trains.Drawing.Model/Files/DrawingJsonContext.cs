using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.HallPlans;
using Tellurian.Trains.Drawing.Model.Modules;
using Tellurian.Trains.Drawing.Model.Tracks;
using Shared = Tellurian.Trains.Schedules.Model.Layouts;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// Source-generated JSON metadata for the drawing files; trim and AOT safe.
/// Enum values are written as in the code, e.g. <c>Left</c>; values in another case, as earlier versions wrote them, are read.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true,
    UseStringEnumConverter = true,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true,
    Converters =
    [
        typeof(PointJsonConverter),
        typeof(AngleJsonConverter),
        typeof(PoseJsonConverter),
    ])]
[JsonSerializable(typeof(DrawingFile<EndProfile>))]
[JsonSerializable(typeof(DrawingFile<ModuleFileContent>))]
[JsonSerializable(typeof(DrawingFile<DrawingCatalogue>))]
[JsonSerializable(typeof(DrawingFile<Hall>))]
[JsonSerializable(typeof(DrawingFile<HallPlan>))]
[JsonSerializable(typeof(Shared.ModuleStandard), TypeInfoPropertyName = "SharedModuleStandard")]
[JsonSerializable(typeof(Shared.Scale), TypeInfoPropertyName = "SharedScale")]
[JsonSerializable(typeof(DrawingFileKind))]
[JsonSerializable(typeof(IReadOnlyList<DrawingRevision>))]
public sealed partial class DrawingJsonContext : JsonSerializerContext;
