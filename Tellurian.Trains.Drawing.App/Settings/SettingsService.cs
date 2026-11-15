using System.Text.Json;
using Tellurian.Trains.Drawing.App.Storage;

namespace Tellurian.Trains.Drawing.App.Settings;

/// <summary>Reads and saves the user's settings in the workbench (IndexedDB in this browser).</summary>
public sealed class SettingsService(IWorkbench workbench)
{
    public const string StorageKey = "settings";

    private UserSettings? _settings;

    /// <summary>Raised after the settings have been saved.</summary>
    public event Action? Changed;

    public async Task<UserSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_settings is not null) return _settings;
        var json = await workbench.GetValueAsync(StorageKey, cancellationToken);
        _settings = json is null ? UserSettings.Empty : JsonSerializer.Deserialize(json, WorkbenchJsonContext.Default.UserSettings) ?? UserSettings.Empty;
        return _settings;
    }

    /// <summary>Saves the settings, with blank texts as not set.</summary>
    public async Task SaveAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        var cleaned = settings with { UserName = Clean(settings.UserName), ApiKey = Clean(settings.ApiKey) };
        await workbench.SetValueAsync(StorageKey, JsonSerializer.Serialize(cleaned, WorkbenchJsonContext.Default.UserSettings), cancellationToken);
        _settings = cleaned;
        Changed?.Invoke();
    }

    /// <summary>The two-letter code of the user's country, or null when none is chosen.</summary>
    public async Task<string?> CountryCodeAsync(CancellationToken cancellationToken = default) =>
        Countries.Find((await GetAsync(cancellationToken)).CountryId)?.Code;

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
