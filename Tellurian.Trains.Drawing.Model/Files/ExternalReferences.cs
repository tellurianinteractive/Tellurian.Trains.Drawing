using System.Text.Json;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// The <c>externalReferences</c> section of a drawing (spec §6): what other systems keep about it, with one entry
/// per system. Each system owns its entry: it alone writes it and decides what is in it. This library reads an
/// entry as JSON and writes it back unchanged. A copy of a drawing loses the section, since it would otherwise
/// point to what the original is registered as.
/// </summary>
public static class ExternalReferences
{
    /// <summary>The name of the section in a file.</summary>
    public const string Section = "externalReferences";

    /// <summary>The entry of the Module Registry.</summary>
    public const string ModuleRegistry = "moduleRegistry";

    /// <summary>In the Registry's entry of a module: the Registry's id of it.</summary>
    public const string ModuleId = "moduleId";

    /// <summary>In the Registry's entry of an end profile: the Registry's id of it.</summary>
    public const string EndProfileId = "endProfileId";

    /// <summary>In the Registry's entry of a standard: the Registry's id of it.</summary>
    public const string StandardId = "standardId";

    /// <summary>Whether two sections have the same entries with the same content; a missing section equals an empty one.</summary>
    public static bool Same(IReadOnlyDictionary<string, JsonElement>? a, IReadOnlyDictionary<string, JsonElement>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if ((a?.Count ?? 0) != (b?.Count ?? 0)) return false;
        if (a is null || b is null) return true;
        return a.All(entry => b.TryGetValue(entry.Key, out var other) && JsonElement.DeepEquals(entry.Value, other));
    }
}
