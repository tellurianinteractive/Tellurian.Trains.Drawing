using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Layout;
using Tellurian.Trains.Drawing.App.Translations;

namespace Tellurian.Trains.Drawing.App.Tests.Layout;

[TestClass]
public sealed class ReleaseNotesTests
{
    private static IRenderedComponent<MainLayout> RenderLayout(AppTestContext context) =>
        context.Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b => b.AddContent(0, "body"))));

    [TestMethod]
    public void BullhornNextToTheVersionOpensAndClosesTheReleaseNotes()
    {
        using var context = new AppTestContext();
        var layout = RenderLayout(context);
        Assert.IsEmpty(layout.FindAll(".release-notes"));

        var button = layout.Find("#release-notes-button");
        Assert.AreEqual("Release notes", button.GetAttribute("title"));
        Assert.IsNotNull(button.QuerySelector("i.fa-bullhorn"));
        button.Click();

        layout.WaitForAssertion(() => Assert.IsNotEmpty(layout.FindAll(".release-notes li")));
        var notes = layout.Find(".modal-panel.release-notes");
        Assert.AreEqual("Release notes", notes.QuerySelector("h1")!.TextContent);
        Assert.AreEqual($"Release {AppVersion.Current}", notes.QuerySelector("h2")!.TextContent);

        layout.Find(".modal-panel .close").Click();
        Assert.IsEmpty(layout.FindAll(".release-notes"));
    }

    [TestMethod]
    public void TheLayoutAsksTheBrowserToKeepTheWorkbench()
    {
        using var context = new AppTestContext();
        RenderLayout(context);
        Assert.AreEqual(1, context.Workbench.PersistenceRequests);
        Assert.IsTrue(context.Workbench.IsPersisted);
    }

    [TestMethod]
    public void ClickingOutsideClosesTheReleaseNotes()
    {
        using var context = new AppTestContext();
        var layout = RenderLayout(context);
        layout.Find("#release-notes-button").Click();
        layout.WaitForElement(".modal-backdrop").Click();
        Assert.IsEmpty(layout.FindAll(".release-notes"));
    }

    [TestMethod]
    public async Task MarkdownIsConvertedToHtmlWithoutItsTitle()
    {
        var html = await Html(ContentDocuments.ReleaseNotes, "en");

        StringAssert.Contains(html, "<strong>Module Draw</strong>");
        Assert.DoesNotContain("Release Notes</h1>", html);
        Assert.DoesNotContain("# Release Notes", html);
    }

    [TestMethod]
    public async Task AMissingDocumentIsEmpty() =>
        Assert.AreEqual("", await Html("Missing", "en"));

    /// <summary>Release notes are translated into every UI language (CLAUDE.md): same versions and dates as the English notes.</summary>
    [TestMethod]
    public void ReleaseNotesAreTranslatedIntoEveryLanguage()
    {
        var english = Releases(File.ReadAllText(ContentFile(ContentDocuments.ReleaseNotes, AppLanguages.Neutral)));
        Assert.IsNotEmpty(english);
        foreach (var language in AppLanguages.All.Where(l => l.TwoLetterCode != AppLanguages.Neutral))
        {
            var path = ContentFile(ContentDocuments.ReleaseNotes, language.TwoLetterCode);
            Assert.IsTrue(File.Exists(path), $"{Path.GetFileName(path)} is missing.");
            CollectionAssert.AreEqual(english, Releases(File.ReadAllText(path)), $"{Path.GetFileName(path)} must have the same releases and dates as ReleaseNotes.md.");
        }
    }

    /// <summary>Every longer text is written in English and translated into every other UI language.</summary>
    [TestMethod]
    public void EveryContentDocumentIsTranslatedIntoEveryLanguage()
    {
        var documents = Directory.GetFiles(RepositoryPaths.Content, "*.md", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(RepositoryPaths.Content, f).Split('.')[0])
            .Distinct()
            .ToList();
        CollectionAssert.IsSubsetOf(new[] { ContentDocuments.Information, ContentDocuments.ReleaseNotes }, documents);
        foreach (var document in documents)
            foreach (var language in AppLanguages.All)
                Assert.IsTrue(File.Exists(ContentFile(document, language.TwoLetterCode)), $"{document} has no {language.TwoLetterCode} translation.");
    }

    [TestMethod]
    public async Task ReleaseNotesAreShownInTheUiLanguageWithEnglishAsFallback()
    {
        StringAssert.Contains(await Html(ContentDocuments.ReleaseNotes, "de-DE"), "Version 0.1.0");
        StringAssert.Contains(await Html(ContentDocuments.ReleaseNotes, "sv-SE"), "Utgiven");
        StringAssert.Contains(await Html(ContentDocuments.ReleaseNotes, "fr"), "Release 0.1.0");
    }

    private static async Task<string> Html(string document, string culture)
    {
        using var context = new AppTestContext();
        return await context.Services.GetRequiredService<ContentDocuments>().HtmlAsync(document, CultureInfo.GetCultureInfo(culture));
    }

    /// <summary>The source file of a document, e.g. <c>ReleaseNotes.md</c> in English and <c>ReleaseNotes.sv.md</c> in Swedish.</summary>
    private static string ContentFile(string document, string language) =>
        Path.Combine(RepositoryPaths.Content, language == AppLanguages.Neutral ? $"{document}.md" : $"{document}.{language}.md");

    /// <summary>Each release as "version date", from its level-two heading and the date that follows.</summary>
    private static string[] Releases(string markdown) =>
        [.. Regex.Matches(markdown, @"^## \D*(\d+\.\d+\.\d+)\s*\r?\n\D*(\d{4}-\d{2}-\d{2})", RegexOptions.Multiline).Select(m => $"{m.Groups[1].Value} {m.Groups[2].Value}")];
}
