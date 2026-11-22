using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Layout;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Drawing.App.Documents;
using Tellurian.Trains.Drawing.App.Translations;
using Tellurian.Trains.Drawing.Model.Catalogues;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Tests.Layout;

[TestClass]
public sealed class HelpTests
{
    [TestMethod]
    public void HelpOpensAsAPopoverAndCloses()
    {
        using var context = new AppTestContext();
        var help = context.Render<HelpButton>(p => p.Add(h => h.For, "HallEditor"));
        Assert.IsEmpty(help.FindAll(".help-popover"));

        help.Find("button.help-toggle").Click();

        help.WaitForAssertion(() => StringAssert.Contains(help.Find(".help-popover-body").InnerHtml, "Emergency exit"));
        Assert.AreEqual("About this page", help.Find(".help-popover-title").TextContent);
        help.Find(".help-backdrop").Click();
        help.WaitForAssertion(() => Assert.IsEmpty(help.FindAll(".help-popover")));
    }

    [TestMethod]
    public void HelpWithoutADocumentSaysSo()
    {
        using var context = new AppTestContext();
        var help = context.Render<HelpButton>(p => p.Add(h => h.For, "Nothing"));

        help.Find("button.help-toggle").Click();

        help.WaitForAssertion(() => Assert.AreEqual("There is no help for this page yet.", help.Find(".help-popover-status").TextContent));
    }

    [TestMethod]
    public async Task EveryHelpTopicHasADocument()
    {
        using var context = new AppTestContext();
        var content = context.Services.GetRequiredService<ContentDocuments>();
        foreach (var topic in ContentDocuments.HelpTopics)
            foreach (var language in new[] { "en", "sv", "de", "da", "nb" })
                Assert.IsGreaterThan(100, (await content.HtmlAsync(ContentDocuments.Help(topic), CultureInfo.GetCultureInfo(language))).Length, $"{topic} in {language}");
    }

    [TestMethod]
    public void PagesHaveHelp()
    {
        using var context = new AppTestContext();
        Assert.IsNotNull(context.Render<Home>().Find("#help-start"));
        Assert.IsNotNull(context.Render<SettingsPage>().Find("#help-settings"));
    }

    [TestMethod]
    public void EveryEndProfileEditorPaneHasItsOwnHelp()
    {
        using var context = new AppTestContext();
        var profile = SampleCatalogue.Instance.EndProfiles[0].Copy("HELP-ME");
        var draft = context.Services.GetRequiredService<DrawingDocuments>().CreateAsync(DrawingFileSerializer.Serialize(profile)).Result;
        var page = context.Render<DrawingPage>(p => p.Add(x => x.Id, draft.Id));
        page.WaitForElement("#symmetry");

        AssertPaneHelp(page, ("archive", "Archive"), ("endprofileeditor", "Edit"), ("endprofileoutline", "Outline"), ("endprofileproperties", "End profile"), ("endprofilederived", "Derived properties"));
    }

    /// <summary>Opens each pane's help by its button, <c>#help-{topic}</c>, and checks that it is titled with the pane's name.</summary>
    internal static void AssertPaneHelp(IRenderedComponent<DrawingPage> page, params (string Topic, string Title)[] panes)
    {
        foreach (var (topic, title) in panes)
        {
            page.Find($"#help-{topic}").Click();
            page.WaitForAssertion(() => Assert.AreEqual(title, page.Find(".help-popover-title").TextContent, topic));
            page.Find(".help-backdrop").Click();
        }
    }

    [TestMethod]
    public void FieldHelpIsOnlyGivenWhenThereIsAText()
    {
        using var context = new AppTestContext();
        var texts = context.Services.GetRequiredService<Texts>();
        Assert.AreEqual("How far into the hall the area in front of the door must be kept free.", texts.Help("KeepFreeDepth"));
        Assert.IsNull(texts.Help("Name"));
    }
}
