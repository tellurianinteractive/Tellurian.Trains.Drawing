using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.Model.AutoDraw;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.HallPlans;
using Tellurian.Trains.Drawing.Model.Halls;
using Tellurian.Trains.Drawing.Model.Modules;
using Tellurian.Trains.Drawing.Model.Tracks;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// Source-generated JSON metadata for the drawing files; trim and AOT safe.
/// Every enum that occurs in a file needs a <see cref="CamelCaseEnumConverter{TEnum}"/> here.
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
        typeof(CamelCaseEnumConverter<DrawingFileKind>),
        typeof(CamelCaseEnumConverter<RailSystem>),
        typeof(CamelCaseEnumConverter<JointFit>),
        typeof(CamelCaseEnumConverter<ProfileOrientation>),
        typeof(CamelCaseEnumConverter<ExitDirection>),
        typeof(CamelCaseEnumConverter<SideMode>),
        typeof(CamelCaseEnumConverter<LabelKind>),
        typeof(CamelCaseEnumConverter<TurnoutKind>),
        typeof(CamelCaseEnumConverter<CurveDirection>),
        typeof(CamelCaseEnumConverter<StraightPosition>),
        typeof(CamelCaseEnumConverter<WideningSide>),
        typeof(CamelCaseEnumConverter<ObstacleKind>),
        typeof(CamelCaseEnumConverter<DoorKind>),
        typeof(CamelCaseEnumConverter<JoinMethod>),
        typeof(CamelCaseEnumConverter<ReferencePointKind>),
        typeof(CamelCaseEnumConverter<PlateSide>),
        typeof(CamelCaseEnumConverter<HallElementKind>),
        typeof(CamelCaseEnumConverter<PlateSegmentKind>),
        typeof(CamelCaseEnumConverter<FilletSide>),
    ])]
[JsonSerializable(typeof(DrawingFile<EndProfile>))]
[JsonSerializable(typeof(DrawingFile<ModuleFileContent>))]
[JsonSerializable(typeof(DrawingFile<DrawingCatalogue>))]
[JsonSerializable(typeof(DrawingFile<Hall>))]
[JsonSerializable(typeof(DrawingFile<HallPlan>))]
[JsonSerializable(typeof(DrawingFileKind))]
public sealed partial class DrawingJsonContext : JsonSerializerContext;
