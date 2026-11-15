using Bunit;
using Tellurian.Trains.Drawing.App.Help;
using Tellurian.Trains.Drawing.App.Layout;
using Tellurian.Trains.Drawing.App.Pages;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class ReleaseNotesPageTests
{
    [TestMethod]
    public void InformationPageLinksToTheReleaseNotes()
    {
        using var context = new AppTestContext();
        var page = context.Render<InformationPage>();

        var link = page.Find("#release-notes");
        Assert.AreEqual(ReleaseNotesPage.Url, link.GetAttribute("href"));
        Assert.AreEqual("Release notes", link.TextContent);
    }

    [TestMethod]
    public void ReleaseNotesShowTheCurrentVersionUnderTheTranslatedTitle()
    {
        using var context = new AppTestContext();
        var page = context.Render<ReleaseNotesPage>();

        var title = Assert.ContainsSingle(page.FindAll("h1"));
        Assert.AreEqual("Release notes", title.TextContent);
        var releases = page.FindAll("h2").Select(h => h.TextContent).ToArray();
        Assert.AreEqual($"Release {AppVersion.Current}", releases[0]);
        Assert.IsNotEmpty(page.FindAll("li"));
        Assert.AreEqual("", page.Find("#get-started").GetAttribute("href"));
    }

    [TestMethod]
    public void MarkdownIsConvertedToHtml()
    {
        var html = MarkdownDocuments.Html(MarkdownDocuments.ReleaseNotes);

        StringAssert.Contains(html, "<strong>Module Draw</strong>");
        Assert.DoesNotContain("# Release Notes", html);
    }

    [TestMethod]
    public void AMissingDocumentThrows() =>
        Assert.ThrowsExactly<FileNotFoundException>(() => MarkdownDocuments.Html("MISSING.md"));
}
