using System.Collections.Concurrent;
using Tellurian.Utilities.Web;

namespace Tellurian.Trains.Drawing.App.Help;

/// <summary>
/// Markdown documents embedded in the app (release notes, later help texts), converted to HTML.
/// Embedded resources work offline and need no HTTP request.
/// </summary>
public static class MarkdownDocuments
{
    public const string ReleaseNotes = "RELEASENOTES.md";

    private static readonly ConcurrentDictionary<string, string> Cache = new();

    /// <summary>The document as HTML, without its leading level-one heading (the page shows its own, translated title).</summary>
    /// <exception cref="FileNotFoundException">The document isn't embedded in the app.</exception>
    public static string Html(string name) => Cache.GetOrAdd(name, static n => WithoutTitle(Markdown(n)).HtmlFromMarkdown());

    private static string Markdown(string name)
    {
        using var stream = typeof(MarkdownDocuments).Assembly.GetManifestResourceStream(name)
            ?? throw new FileNotFoundException($"The document '{name}' is not embedded in the app.", name);
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
