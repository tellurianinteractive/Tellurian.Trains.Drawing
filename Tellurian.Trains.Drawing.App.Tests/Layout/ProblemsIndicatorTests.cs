using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Catalogue;
using Tellurian.Trains.Drawing.App.Layout;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.App.Tests.Layout;

[TestClass]
public sealed class ProblemsIndicatorTests
{
    private static readonly Guid Missing = Guid.NewGuid();

    private static EndProfile Known => CatalogueService.BuiltIn.EndProfiles[0];

    private static EndProfileCompatibilityOverride BrokenOverride => new()
    {
        FirstEndProfileId = Known.Id,
        SecondEndProfileId = Missing,
        Fit = JointFit.Fits,
    };

    private static void SaveUser(AppTestContext context, DrawingCatalogue user) =>
        context.Services.GetRequiredService<CatalogueService>().SaveUserAsync(user).Wait();

    [TestMethod]
    public void WithoutProblemsTheIndicatorIsAGreenCheck()
    {
        using var context = new AppTestContext();
        var indicator = context.Render<ProblemsIndicator>();

        Assert.IsNotNull(indicator.Find(".problems-indicator.no-problems i.fa-circle-check"));
        Assert.IsEmpty(indicator.FindAll(".badge"));
        indicator.Find("#problems-button").Click();
        Assert.AreEqual("No problems found.", indicator.Find(".problems-panel .empty").TextContent);
    }

    [TestMethod]
    public void ProblemsAreCountedAndLeadToTheEntryToCorrect()
    {
        using var context = new AppTestContext();
        SaveUser(context, DrawingCatalogue.Empty.With(BrokenOverride));
        var indicator = context.Render<ProblemsIndicator>();

        Assert.IsNotNull(indicator.Find(".problems-indicator.has-problems i.fa-circle-exclamation"));
        Assert.AreEqual("1", indicator.Find(".badge").TextContent);
        indicator.Find("#problems-button").Click();
        var link = indicator.Find(".problems-panel li a");
        Assert.AreEqual($"catalogue/override?first={Known.Id}&second={Missing}", link.GetAttribute("href"));
        StringAssert.StartsWith(link.TextContent, $"Compatibility override {Known.Designation} – unknown end profile:");

        link.Click();
        Assert.IsEmpty(indicator.FindAll(".problems-panel"));
    }

    [TestMethod]
    public void TheIndicatorFollowsChangesToTheCatalogue()
    {
        using var context = new AppTestContext();
        var indicator = context.Render<ProblemsIndicator>();

        SaveUser(context, DrawingCatalogue.Empty.With(BrokenOverride));
        indicator.WaitForAssertion(() => Assert.AreEqual("1", indicator.Find(".badge").TextContent));

        SaveUser(context, DrawingCatalogue.Empty);
        indicator.WaitForAssertion(() => Assert.IsEmpty(indicator.FindAll(".badge")));
    }
}
