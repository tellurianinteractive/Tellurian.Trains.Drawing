using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Drawing.App.Settings;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class SettingsPageTests
{
    [TestMethod]
    public void SettingsAreSavedInTheWorkbench()
    {
        using var context = new AppTestContext();
        var page = context.Render<SettingsPage>();

        page.Find("#userName").Change("  Anna Andersson ");
        page.Find("#apiKey").Change("secret-key");
        page.Find("form").Submit();

        var settings = context.Services.GetRequiredService<SettingsService>().GetAsync().Result;
        Assert.AreEqual(("Anna Andersson", "secret-key", (int?)null), (settings.UserName, settings.ApiKey, settings.CountryId));
        Assert.IsTrue(context.Workbench.Values.ContainsKey(SettingsService.StorageKey));
        Assert.AreEqual("Settings saved.", page.Find("#settings-saved").TextContent);
    }

    [TestMethod]
    public void SavedSettingsAreShown()
    {
        using var context = new AppTestContext();
        context.Services.GetRequiredService<SettingsService>().SaveAsync(new() { UserName = "Bo", ApiKey = "k" }).Wait();

        var page = context.Render<SettingsPage>();

        Assert.AreEqual("Bo", page.Find("#userName").GetAttribute("value"));
        Assert.AreEqual("k", page.Find("#apiKey").GetAttribute("value"));
        Assert.AreEqual("(none)", page.Find("#country option").TextContent);
    }

    [TestMethod]
    public void TheScaleIsChosenAmongTheCatalogueScales()
    {
        using var context = new AppTestContext();
        var page = context.Render<SettingsPage>();

        var options = page.FindAll("#scale option").Select(o => o.GetAttribute("value")).ToArray();
        CollectionAssert.AreEqual(new[] { "", "H0", "N" }, options);
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

        var choices = context.Services.GetRequiredService<global::Tellurian.Trains.Drawing.App.Catalogue.CatalogueService>().GetForChoicesAsync().Result;

        Assert.IsNotEmpty(choices.Standards);
        Assert.IsTrue(choices.Standards.All(s => s.Scale == "N"));
        Assert.IsTrue(choices.EndProfiles.All(p => p.Scale == "N"));
    }

    [TestMethod]
    public void TheApiKeyIsHiddenUntilShown()
    {
        using var context = new AppTestContext();
        var page = context.Render<SettingsPage>();

        Assert.AreEqual("password", page.Find("#apiKey").GetAttribute("type"));
        page.Find("#toggle-api-key").Click();
        Assert.AreEqual("text", page.Find("#apiKey").GetAttribute("type"));
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
}
