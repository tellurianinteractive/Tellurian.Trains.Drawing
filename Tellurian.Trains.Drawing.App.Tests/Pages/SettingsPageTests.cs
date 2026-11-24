using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Drawing.App.Settings;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class SettingsPageTests
{
    /// <summary>
    /// The page once the advice for the browser has arrived: it is loaded after the settings are shown, and until then
    /// a click on a busy machine could be handled after the test looks for its result.
    /// </summary>
    private static IRenderedComponent<SettingsPage> RenderPage(AppTestContext context)
    {
        var page = context.Render<SettingsPage>();
        page.WaitForAssertion(() => StringAssert.Contains(page.Find("#keep-data-help").InnerHtml, "<ol>"));
        return page;
    }

    [TestMethod]
    public void SettingsAreSavedInTheWorkbench()
    {
        using var context = new AppTestContext();
        var page = RenderPage(context);

        page.Find("#userName").Change("  Anna Andersson ");
        page.Find("#apiKey").Change("secret-key");
        page.Find("form").Submit();

        // The advice below the form arrives on its own; on a busy machine the submit may be handled a moment later.
        page.WaitForAssertion(() => Assert.AreEqual("Settings saved.", page.Find("#settings-saved").TextContent));
        var settings = context.Services.GetRequiredService<SettingsService>().GetAsync().Result;
        Assert.AreEqual(("Anna Andersson", "secret-key", (int?)null), (settings.UserName, settings.ApiKey, settings.CountryId));
        Assert.IsTrue(context.Workbench.Values.ContainsKey(SettingsService.StorageKey));
    }

    [TestMethod]
    public void SavedSettingsAreShown()
    {
        using var context = new AppTestContext();
        context.Services.GetRequiredService<SettingsService>().SaveAsync(new() { UserName = "Bo", ApiKey = "k" }).Wait();

        var page = RenderPage(context);

        Assert.AreEqual("Bo", page.Find("#userName").GetAttribute("value"));
        Assert.AreEqual("k", page.Find("#apiKey").GetAttribute("value"));
        Assert.AreEqual("(none)", page.Find("#country option").TextContent);
    }

    [TestMethod]
    public void TheScaleIsChosenAmongTheKnownScales()
    {
        using var context = new AppTestContext();
        var page = RenderPage(context);

        var options = page.FindAll("#scale option").Select(o => o.GetAttribute("value")).ToArray();
        Assert.AreEqual("", options[0]);
        CollectionAssert.IsSubsetOf(new[] { "H0", "N", "TT" }, options, "The suite's scales, also without a catalogue.");
        Assert.AreEqual("(all scales)", page.Find("#scale option").TextContent);

        page.Find("#scale").Change("N");
        page.Find("form").Submit();

        Assert.AreEqual("N", context.Services.GetRequiredService<SettingsService>().GetAsync().Result.Scale);
    }

    [TestMethod]
    public void TheScaleLimitsTheChoices()
    {
        using var context = new AppTestContext();
        context.Services.GetRequiredService<SettingsService>().SaveAsync(new() { Scale = "N" }).Wait();

        var choices = context.Services.GetRequiredService<global::Tellurian.Trains.Drawing.App.Catalogues.CatalogueService>().GetForChoicesAsync().Result;

        Assert.IsNotEmpty(choices.Standards);
        Assert.IsTrue(choices.Standards.All(s => s.ScaleName == "N"));
        Assert.IsTrue(choices.EndProfiles.All(p => p.Scale == "N"));
    }

    [TestMethod]
    public void TheApiKeyIsHiddenUntilShown()
    {
        using var context = new AppTestContext();
        var page = RenderPage(context);

        Assert.AreEqual("password", page.Find("#apiKey").GetAttribute("type"));
        page.Find("#toggle-api-key").Click();
        page.WaitForAssertion(() => Assert.AreEqual("text", page.Find("#apiKey").GetAttribute("type")));
        Assert.AreEqual("Hide", page.Find("#toggle-api-key").TextContent);
    }

    [TestMethod]
    public void BlankTextsAreNotSet()
    {
        using var context = new AppTestContext();
        var service = context.Services.GetRequiredService<SettingsService>();

        service.SaveAsync(new() { UserName = "  ", ApiKey = "" }).Wait();

        Assert.AreEqual(UserSettings.Empty, service.GetAsync().Result);
    }

    [TestMethod]
    public void StorageKeptByTheBrowserIsShown()
    {
        using var context = new AppTestContext();
        context.Workbench.IsPersisted = true;
        var page = RenderPage(context);

        Assert.IsNotNull(page.Find("#storage-persisted"));
        Assert.IsEmpty(page.FindAll("#request-persistence"));
    }

    [TestMethod]
    public void StorageThatMayBeClearedCanBeKept()
    {
        using var context = new AppTestContext();
        var page = RenderPage(context);
        Assert.IsNotNull(page.Find("#storage-not-persisted"));

        page.Find("#request-persistence").Click();

        page.WaitForElement("#storage-persisted");
    }

    [TestMethod]
    public void BrowserThatRefusesToKeepTheStorageIsExplained()
    {
        using var context = new AppTestContext();
        context.Workbench.GrantsPersistence = false;
        var page = RenderPage(context);

        page.Find("#request-persistence").Click();

        page.WaitForElement("#storage-refused");
        Assert.IsNotNull(page.Find("#storage-not-persisted"));
    }

    [TestMethod]
    public void AdviceIsForTheBrowserInUseWithTheSitesAddress()
    {
        using var context = new AppTestContext();
        context.Services.GetRequiredService<BrowserEnvironment>().Capabilities = new() { Browser = "Firefox" };
        var page = RenderPage(context);

        page.WaitForAssertion(() => StringAssert.Contains(page.Find("#keep-data-help").InnerHtml, "Manage Exceptions"));
        var help = page.Find("#keep-data-help");
        Assert.IsTrue(help.HasAttribute("open"), "Open while the browser doesn't keep the data.");
        StringAssert.Contains(help.QuerySelector("summary")!.TextContent, "Firefox");
        StringAssert.Contains(help.InnerHtml, "Manage Exceptions");
        Assert.AreEqual("http://localhost", page.Find("#site-address").TextContent);
    }

    [TestMethod]
    public void AdviceIsAtHandAlsoWhenTheBrowserKeepsTheData()
    {
        using var context = new AppTestContext();
        context.Workbench.IsPersisted = true;
        var page = RenderPage(context);

        var help = page.Find("#keep-data-help");
        Assert.IsFalse(help.HasAttribute("open"));
        StringAssert.Contains(help.QuerySelector("summary")!.TextContent, "your browser", "An unknown browser gets the general advice.");
    }

    [TestMethod]
    public void SettingsAreExportedWithTheLanguage()
    {
        using var context = new AppTestContext();
        var page = RenderPage(context);
        page.Find("#userName").Change("Anna");

        page.Find("#export-settings").Click();

        page.WaitForAssertion(() => Assert.HasCount(1, context.Files.Saves));
        var (text, name, _, saveAs) = context.Files.Saves.Single();
        Assert.AreEqual("Module Draw settings.json", name);
        Assert.IsTrue(saveAs);
        var file = SettingsFile.Read(text)!;
        Assert.AreEqual("Anna", file.Settings.UserName, "What is shown is saved and exported.");
        Assert.AreEqual(System.Globalization.CultureInfo.CurrentUICulture.Name, file.Culture);
        Assert.AreEqual("The settings were exported.", page.Find("#settings-saved").TextContent);
    }

    [TestMethod]
    public void ImportedSettingsAreSavedAndShown()
    {
        using var context = new AppTestContext();
        context.Files.FilesToOpen = [new("my settings.json", SettingsFile.Write(new() { UserName = "Bo", ApiKey = "k" }, System.Globalization.CultureInfo.CurrentUICulture.Name))];
        var page = RenderPage(context);

        page.Find("#import-settings").Click();

        page.WaitForAssertion(() => Assert.AreEqual("Bo", page.Find("#userName").GetAttribute("value")));
        Assert.AreEqual("Bo", context.Services.GetRequiredService<SettingsService>().GetAsync().Result.UserName);
        Assert.AreEqual("The settings were imported.", page.Find("#settings-saved").TextContent);
    }

    [TestMethod]
    public void ImportOfAnotherFileIsRefused()
    {
        using var context = new AppTestContext();
        context.Files.FilesToOpen = [new("station.module.json", """{ "format": "tellurian-drawing", "kind": "module" }""")];
        var page = RenderPage(context);

        page.Find("#import-settings").Click();

        page.WaitForAssertion(() => Assert.AreEqual("station.module.json isn't a Module Draw settings file.", page.Find("p.error").TextContent));
        Assert.IsFalse(context.Workbench.Values.ContainsKey(SettingsService.StorageKey));
    }
}
