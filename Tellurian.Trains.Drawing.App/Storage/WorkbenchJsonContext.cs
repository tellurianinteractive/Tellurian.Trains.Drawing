using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Storage;

/// <summary>Source-generated JSON metadata for the workbench and file interop; trim and AOT safe.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(CamelCaseEnumConverter<DrawingFileKind>)])]
[JsonSerializable(typeof(Draft))]
[JsonSerializable(typeof(List<Draft>))]
[JsonSerializable(typeof(RecentFile))]
[JsonSerializable(typeof(List<RecentFile>))]
[JsonSerializable(typeof(StoredValue))]
[JsonSerializable(typeof(BrowserCapabilities))]
[JsonSerializable(typeof(OpenedFile))]
[JsonSerializable(typeof(List<OpenedFile>))]
[JsonSerializable(typeof(OpenedFolder))]
[JsonSerializable(typeof(SavedFile))]
internal sealed partial class WorkbenchJsonContext : JsonSerializerContext;
