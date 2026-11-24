using System.Text.Json;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>External references for tests, written as JSON.</summary>
internal static class TestReferences
{
    /// <summary>A section with the Module Registry's entry <paramref name="entryJson"/>.</summary>
    public static Dictionary<string, JsonElement> Registry(string entryJson) =>
        new Dictionary<string, JsonElement> { [ExternalReferences.ModuleRegistry] = Element(entryJson) };

    public static JsonElement Element(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    /// <summary>The Module Registry's entry of a section, as compact JSON; null when there is none.</summary>
    public static string? RegistryEntry(IReadOnlyDictionary<string, JsonElement>? references) =>
        references is not null && references.TryGetValue(ExternalReferences.ModuleRegistry, out var entry) ? System.Text.Json.Nodes.JsonNode.Parse(entry.GetRawText())!.ToJsonString() : null;
}
