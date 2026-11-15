using System.Text.Json;
using Tellurian.Trains.Drawing.App.Storage;

namespace Tellurian.Trains.Drawing.App.Settings;

/// <summary>
/// The countries of the Module Registry, from an excerpt of its database embedded in the app
/// (<c>Settings/countries.json</c>) until the Registry has an API for them.
/// </summary>
public static class Countries
{
    public const string ResourceName = "countries.json";

    private static readonly Lazy<IReadOnlyList<Country>> List = new(Load);

    /// <summary>All countries, by English name.</summary>
    public static IReadOnlyList<Country> All => List.Value;

    public static Country? Find(int? id) => id is { } value ? All.FirstOrDefault(c => c.Id == value) : null;

    private static IReadOnlyList<Country> Load()
    {
        using var stream = typeof(Countries).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new FileNotFoundException($"The country list '{ResourceName}' is not embedded in the app.", ResourceName);
        var countries = JsonSerializer.Deserialize(stream, WorkbenchJsonContext.Default.ListCountry) ?? [];
        return [.. countries.OrderBy(c => c.EnglishName, StringComparer.CurrentCultureIgnoreCase)];
    }
}
