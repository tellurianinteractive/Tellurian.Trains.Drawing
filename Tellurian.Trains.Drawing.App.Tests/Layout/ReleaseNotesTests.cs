using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Tellurian.Trains.Drawing.App.Help;
using Tellurian.Trains.Drawing.App.Layout;

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

        var notes = layout.Find(".modal-panel.release-notes");
        Assert.AreEqual("Release notes", notes.QuerySelector("h1")!.TextContent);
        Assert.AreEqual($"Release {AppVersion.Current}", notes.QuerySelector("h2")!.TextContent);
        Assert.IsNotEmpty(notes.QuerySelectorAll("li"));

        layout.Find(".modal-panel .close").Click();
        Assert.IsEmpty(layout.FindAll(".release-notes"));
    }

    [TestMethod]
    public void ClickingOutsideClosesTheReleaseNotes()
    {
        using var context = new AppTestContext();
        var layout = RenderLayout(context);
        layout.Find("#release-notes-button").Click();
        layout.Find(".modal-backdrop").Click();
        Assert.IsEmpty(layout.FindAll(".release-notes"));
    }

    [TestMethod]
    public void MarkdownIsConvertedToHtmlWithoutItsTitle()
    {
        var html = MarkdownDocuments.Html(MarkdownDocuments.ReleaseNotes);

        StringAssert.Contains(html, "<strong>Module Draw</strong>");
        Assert.DoesNotContain("Release Notes</h1>", html);
        Assert.DoesNotContain("# Release Notes", html);
    }

    [TestMethod]
    public void AMissingDocumentThrows() =>
        Assert.ThrowsExactly<FileNotFoundException>(() => MarkdownDocuments.Html("MISSING.md"));

    /// <summary>Release notes are translated into every UI language (CLAUDE.md): same versions and dates as the English notes.</summary>
    [TestMethod]
    public void ReleaseNotesAreTranslatedIntoEveryLanguage()
    {
        var english = Releases(MarkdownDocuments.Markdown(MarkdownDocuments.ReleaseNotes));
        Assert.IsNotEmpty(english);
        foreach (var language in AppLanguages.All.Where(l => l.TwoLetterCode != AppLanguages.Neutral))
        {
            var name = MarkdownDocuments.TranslatedName(MarkdownDocuments.ReleaseNotes, language.TwoLetterCode);
            Assert.IsTrue(MarkdownDocuments.Exists(name), $"{name} is missing.");
            CollectionAssert.AreEqual(english, Releases(MarkdownDocuments.Markdown(name)), $"{name} must have the same releases and dates as RELEASENOTES.md.");
        }
    }

    [TestMethod]
    public void ReleaseNotesAreShownInTheUiLanguageWithEnglishAsFallback()
    {
        Assert.AreEqual("RELEASENOTES.sv.md", MarkdownDocuments.ResourceName(MarkdownDocuments.ReleaseNotes, CultureInfo.GetCultureInfo("sv-SE")));
        Assert.AreEqual("RELEASENOTES.md", MarkdownDocuments.ResourceName(MarkdownDocuments.ReleaseNotes, CultureInfo.GetCultureInfo("en")));
        Assert.AreEqual("RELEASENOTES.md", MarkdownDocuments.ResourceName(MarkdownDocuments.ReleaseNotes, CultureInfo.GetCultureInfo("fr")));
        StringAssert.Contains(MarkdownDocuments.Html(MarkdownDocuments.ReleaseNotes, CultureInfo.GetCultureInfo("de-DE")), "Version 0.1.0");
    }

    /// <summary>Each release as "version date", from its level-two heading and the date that follows.</summary>
    private static string[] Releases(string markdown) =>
        [.. Regex.Matches(markdown, @"^## \D*(\d+\.\d+\.\d+)\s*\r?\n\D*(\d{4}-\d{2}-\d{2})", RegexOptions.Multiline).Select(m => $"{m.Groups[1].Value} {m.Groups[2].Value}")];
}
