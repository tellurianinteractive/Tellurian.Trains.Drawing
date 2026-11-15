using System.Collections.Concurrent;
using System.Globalization;
using Tellurian.Utilities.Web;

namespace Tellurian.Trains.Drawing.App.Help;

/// <summary>
/// Markdown documents embedded in the app (release notes, later help texts), converted to HTML.
/// A document is written in English (<c>NAME.md</c>) and translated into the other UI languages
/// (<c>NAME.sv.md</c>, <c>NAME.de.md</c>, …). Embedded resources work offline and need no HTTP request.
/// </summary>
public static class MarkdownDocuments
{
    public const string ReleaseNotes = "RELEASENOTES.md";

    private static readonly ConcurrentDictionary<string, string> Cache = new();

    /// <summary>The document in the language of <paramref name="culture"/>, or in English when it isn't translated, as HTML.</summary>
    public static string Html(string name, CultureInfo culture) => Html(ResourceName(name, culture));

    /// <summary>The document as HTML, without its leading level-one heading (the page shows its own, translated title).</summary>
    /// <exception cref="FileNotFoundException">The document isn't embedded in the app.</exception>
    public static string Html(string resourceName) =>
        Cache.GetOrAdd(resourceName, static n => WithoutTitle(Markdown(n)).HtmlFromMarkdown());

    /// <summary>The embedded resource with the translation for <paramref name="culture"/>, or the English one.</summary>
    public static string ResourceName(string name, CultureInfo culture)
    {
        var translated = TranslatedName(name, culture.TwoLetterISOLanguageName);
        return Exists(translated) ? translated : name;
    }

    /// <summary>The name of a translation, e.g. <c>RELEASENOTES.sv.md</c> for <c>RELEASENOTES.md</c> and <c>sv</c>.</summary>
    public static string TranslatedName(string name, string language) =>
        $"{Path.GetFileNameWithoutExtension(name)}.{language}{Path.GetExtension(name)}";

    public static bool Exists(string resourceName) =>
        typeof(MarkdownDocuments).Assembly.GetManifestResourceInfo(resourceName) is not null;

    /// <summary>The document's Markdown text.</summary>
    /// <exception cref="FileNotFoundException">The document isn't embedded in the app.</exception>
    public static string Markdown(string resourceName)
    {
        using var stream = typeof(MarkdownDocuments).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"The document '{resourceName}' is not embedded in the app.", resourceName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string WithoutTitle(string markdown)
    {
        var text = markdown.TrimStart();
        if (!text.StartsWith("# ", StringComparison.Ordinal)) return text;
        var endOfLine = text.IndexOf('\n');
        return endOfLine < 0 ? "" : text[(endOfLine + 1)..];
    }
}
