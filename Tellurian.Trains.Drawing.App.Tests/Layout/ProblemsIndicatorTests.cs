using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Catalogues;
using Tellurian.Trains.Drawing.App.Layout;
using Tellurian.Trains.Drawing.Model.Catalogues;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using SettingsService = Tellurian.Trains.Drawing.App.Settings.SettingsService;

namespace Tellurian.Trains.Drawing.App.Tests.Layout;

[TestClass]
public sealed class ProblemsIndicatorTests
{
    private static readonly Guid Missing = Guid.NewGuid();

    private static EndProfile Known => SampleCatalogue.Instance.EndProfiles[0];

    private static EndProfileCompatibilityOverride BrokenOverride => new()
    {
        FirstEndProfileId = Known.Id,
        SecondEndProfileId = Missing,
        Fit = JointFit.Fits,
    };

    private static void SaveUser(AppTestContext context, Catalogue user) =>
        context.Services.GetRequiredService<CatalogueService>().SaveUserAsync(user).Wait();

    private static void SetUserName(AppTestContext context, string? name) =>
        context.Services.GetRequiredService<SettingsService>().SaveAsync(new() { UserName = name }).Wait();

    [TestMethod]
    public void WithoutProblemsTheIndicatorIsAGreenCheck()
    {
        using var context = new AppTestContext();
        SetUserName(context, "Anna");
        var indicator = context.Render<ProblemsIndicator>();

        Assert.IsNotNull(indicator.Find(".problems-indicator.no-problems i.fa-circle-check"));
        Assert.IsEmpty(indicator.FindAll(".badge"));
        indicator.Find("#problems-button").Click();
        Assert.AreEqual("No problems found.", indicator.Find(".problems-panel .empty").TextContent);
    }

    [TestMethod]
    public void WithoutAUserNameThereIsAWarningThatLeadsToTheSettings()
    {
        using var context = new AppTestContext();
        var indicator = context.Render<ProblemsIndicator>();

        Assert.IsNotNull(indicator.Find(".problems-indicator.has-warnings i.fa-triangle-exclamation"));
        Assert.AreEqual("1", indicator.Find(".badge").TextContent);
        indicator.Find("#problems-button").Click();
        var warning = indicator.Find("li.warning a");
        Assert.AreEqual("settings", warning.GetAttribute("href"));
        StringAssert.StartsWith(warning.TextContent, "No user name in the settings");
    }

    [TestMethod]
    public void SavingAUserNameRemovesTheWarning()
    {
        using var context = new AppTestContext();
        var indicator = context.Render<ProblemsIndicator>();
        Assert.IsNotNull(indicator.Find(".has-warnings"));

        SetUserName(context, "Anna");

        indicator.WaitForAssertion(() => Assert.IsNotNull(indicator.Find(".no-problems")));
    }

    [TestMethod]
    public void ProblemsAreCountedAndLeadToTheEntryToCorrect()
    {
        using var context = new AppTestContext();
        SetUserName(context, "Anna");
        SaveUser(context, SampleCatalogue.Instance.With(BrokenOverride));
        var indicator = context.Render<ProblemsIndicator>();

        Assert.IsNotNull(indicator.Find(".problems-indicator.has-problems i.fa-circle-exclamation"));
        Assert.AreEqual("1", indicator.Find(".badge").TextContent);
        indicator.Find("#problems-button").Click();
        var link = indicator.Find(".problems-panel li.error a");
        Assert.AreEqual($"catalogue/override?first={Known.Id}&second={Missing}", link.GetAttribute("href"));
        StringAssert.StartsWith(link.TextContent, $"Compatibility override {Known.Designation} – unknown end profile:");

        link.Click();
        Assert.IsEmpty(indicator.FindAll(".problems-panel"));
    }

    [TestMethod]
    public void ErrorsComeBeforeWarningsAndBothAreCounted()
    {
        using var context = new AppTestContext();
        SaveUser(context, SampleCatalogue.Instance.With(BrokenOverride));
        var indicator = context.Render<ProblemsIndicator>();

        Assert.IsNotNull(indicator.Find(".has-problems"));
        Assert.AreEqual("2", indicator.Find(".badge").TextContent);
        indicator.Find("#problems-button").Click();
        CollectionAssert.AreEqual(new[] { "error", "warning" }, indicator.FindAll(".problems-panel li").Select(li => li.ClassName).ToArray());
    }

    [TestMethod]
    public void TheIndicatorFollowsChangesToTheCatalogue()
    {
        using var context = new AppTestContext();
        SetUserName(context, "Anna");
        var indicator = context.Render<ProblemsIndicator>();

        SaveUser(context, SampleCatalogue.Instance.With(BrokenOverride));
        indicator.WaitForAssertion(() => Assert.AreEqual("1", indicator.Find(".badge").TextContent));

        SaveUser(context, Catalogue.Empty);
        indicator.WaitForAssertion(() => Assert.IsEmpty(indicator.FindAll(".badge")));
    }
}
