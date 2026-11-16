using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Localization;
using Tellurian.Utilities.Web;

namespace Tellurian.Trains.Drawing.App.Translations;

/// <summary>
/// The app's longer texts: Markdown documents in <c>wwwroot/Content</c>, written in English (<c>Name.md</c>)
/// and translated into every other UI language (<c>Name.sv.md</c>, <c>Name.de.md</c>, …), returned as HTML.
/// They are read through the Tellurian.Localization Markdown provider, which falls back to English.
/// </summary>
public sealed class ContentDocuments([FromKeyedServices(ContentDocuments.MarkdownProvider)] IResourceProvider markdown)
{
    public const string MarkdownProvider = "Markdown";

    /// <summary>Where the documents are served from, relative to the app's base address.</summary>
    public const string BasePath = "_content/Tellurian.Trains.Drawing.App.Translations/Content";

    public const string Information = "Information";
    public const string ReleaseNotes = "ReleaseNotes";

    private readonly ConcurrentDictionary<string, string> cache = new();

    /// <summary>
    /// The document in the language of <paramref name="culture"/> as HTML, without its leading level-one
    /// heading (the page shows its own, translated title). Empty when the document doesn't exist.
    /// </summary>
    public async ValueTask<string> HtmlAsync(string name, CultureInfo culture)
    {
        var key = $"{culture.TwoLetterISOLanguageName}:{name}";
        if (cache.TryGetValue(key, out var html)) return html;
        var content = await markdown.GetTranslationAsync(name, culture);
        // The provider returns the name itself when no file is found.
        html = string.IsNullOrWhiteSpace(content.Text) || content.Text == name ? "" : WithoutTitle(content.Text).HtmlFromMarkdown();
        return cache.GetOrAdd(key, html);
    }

    private static string WithoutTitle(string markdown)
    {
        var text = markdown.TrimStart('﻿').TrimStart();
        if (!text.StartsWith("# ", StringComparison.Ordinal)) return text;
        var endOfLine = text.IndexOf('\n');
        return endOfLine < 0 ? "" : text[(endOfLine + 1)..];
    }
}
