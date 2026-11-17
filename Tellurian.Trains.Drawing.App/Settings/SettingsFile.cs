using System.Text.Json;
using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.App.Storage;

namespace Tellurian.Trains.Drawing.App.Settings;

/// <summary>
/// The user's settings in a file of their own, to restore them in a browser that has forgotten them or to take them to
/// another browser: the settings and the UI culture, e.g. <c>sv-SE</c>. Not a drawing file; the app reads it only when
/// the user imports settings.
/// </summary>
public sealed record SettingsFile
{
    public const string FormatName = "tellurian-drawing-settings";
    public const int CurrentVersion = 1;

    public string Format { get; init; } = FormatName;
    public int Version { get; init; } = CurrentVersion;
    public required UserSettings Settings { get; init; }

    /// <summary>The UI culture, as stored by the language selector; null to keep the current one.</summary>
    public string? Culture { get; init; }

    public static string Write(UserSettings settings, string? culture) =>
        JsonSerializer.Serialize(new SettingsFile { Settings = settings, Culture = culture }, IndentedContext.SettingsFile);

    /// <summary>Reads a settings file; null if <paramref name="json"/> isn't one, or is of a newer version than this app reads.</summary>
    public static SettingsFile? Read(string json)
    {
        try
        {
            var file = JsonSerializer.Deserialize(json, WorkbenchJsonContext.Default.SettingsFile);
            return file is { Format: FormatName, Version: > 0 and <= CurrentVersion } ? file : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static readonly WorkbenchJsonContext IndentedContext = new(new JsonSerializerOptions(WorkbenchJsonContext.Default.Options) { WriteIndented = true });
}
