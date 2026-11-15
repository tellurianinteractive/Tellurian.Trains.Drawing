using System.Text.Json;
using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Sandbox;

/// <summary>Keeps the sandbox's test document in the workbench, so autosave and restore can be tried there.</summary>
public sealed class SandboxStore(IWorkbench workbench)
{
    public const string Key = "sandbox";

    public async Task<SandboxDocument?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var json = await workbench.GetValueAsync(Key, cancellationToken);
        if (json is null) return null;
        try
        {
            return JsonSerializer.Deserialize(json, SandboxJsonContext.Default.SandboxDocument);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public Task SaveAsync(SandboxDocument document, CancellationToken cancellationToken = default) =>
        workbench.SetValueAsync(Key, JsonSerializer.Serialize(document, SandboxJsonContext.Default.SandboxDocument), cancellationToken);
}

/// <summary>Source-generated JSON metadata for the sandbox document; trim and AOT safe.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(PointJsonConverter), typeof(AngleJsonConverter), typeof(PoseJsonConverter), typeof(CamelCaseEnumConverter<DimensionKind>)])]
[JsonSerializable(typeof(SandboxDocument))]
internal sealed partial class SandboxJsonContext : JsonSerializerContext;
