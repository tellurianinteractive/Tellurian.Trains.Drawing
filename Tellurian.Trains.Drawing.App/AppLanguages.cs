using Tellurian.Localization;

namespace Tellurian.Trains.Drawing.App;

/// <summary>The UI languages (AP-2) and how they map to cultures.</summary>
public static class AppLanguages
{
    public const string Neutral = "en";

    public static IReadOnlyList<Language> All { get; } =
    [
        new(Neutral, true),
        new("sv", true) { CultureCode = "SE" },
        new("de", true) { CapitalizesNouns = true },
        new("da", true) { CultureCode = "DK" },
        new("nb", true) { CultureCode = "NO" },
    ];

    /// <summary>The culture name stored and passed to Blazor.start, e.g. <c>sv-SE</c>.</summary>
    public static string CultureOf(Language language) =>
        language.CultureCode is null ? language.TwoLetterCode : $"{language.TwoLetterCode}-{language.CultureCode}";
}
