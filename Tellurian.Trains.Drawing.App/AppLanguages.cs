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

    /// <summary>The flag image for the language selector, e.g. <c>images/flags/se.png</c>; English shows the British flag.</summary>
    public static string FlagOf(Language language) =>
        $"images/flags/{(language.TwoLetterCode == Neutral ? "gb" : (language.CultureCode ?? language.TwoLetterCode).ToLowerInvariant())}.png";

    /// <summary>The language's name in itself, capitalised, e.g. "Svenska".</summary>
    public static string NativeNameOf(Language language) =>
        System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(System.Globalization.CultureInfo.GetCultureInfo(language.TwoLetterCode).NativeName);
}
