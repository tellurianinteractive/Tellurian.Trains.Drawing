using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Catalogue;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class CatalogueEditorPageTests
{
    private static DrawingCatalogue User(AppTestContext context) =>
        context.Services.GetRequiredService<CatalogueService>().GetUserAsync().Result;

    private static string Uri(AppTestContext context) => context.Services.GetRequiredService<NavigationManager>().Uri;

    [TestMethod]
    public void NewStandardIsSavedAsAUserEntry()
    {
        using var context = new AppTestContext();
        var page = context.Render<StandardEditorPage>();

        page.Find("#shortName").Change("MY-H0");
        page.Find("#normalGauge").Change("16.5");
        page.Find("form").Submit();

        var standard = User(context).Standards.Single();
        Assert.AreEqual(("MY-H0", "H0", 16.5), (standard.ShortName, standard.Scale, standard.NormalGauge));
        Assert.EndsWith("catalogue", Uri(context));
    }

    [TestMethod]
    public void InvalidStandardShowsTheProblemsWithFieldNames()
    {
        using var context = new AppTestContext();
        var page = context.Render<StandardEditorPage>();

        page.Find("#shortName").Change("FREMO/H0-RE");
        page.Find("form").Submit();

        var problems = page.FindAll(".problems li").Select(e => e.TextContent).ToList();
        CollectionAssert.Contains(problems, "Short name: the name is already used.");
        CollectionAssert.Contains(problems, "Normal gauge (mm) must be greater than 0.");
        Assert.IsEmpty(User(context).Standards);
    }

    [TestMethod]
    public void EditingABuiltInStandardCreatesAReplacement()
    {
        using var context = new AppTestContext();
        var builtIn = BuiltInCatalogue.Instance.Standards[0];
        var page = context.Render<StandardEditorPage>(p => p.Add(x => x.Id, builtIn.Id));
        Assert.IsNotNull(page.Find(".note"));
        Assert.AreEqual(builtIn.ShortName, page.Find("#shortName").GetAttribute("value"));

        page.Find("#railTopHeight").Change("1250");
        page.Find("form").Submit();

        var replacement = User(context).Standards.Single();
        Assert.AreEqual((builtIn.Id, 1250.0, builtIn.DefaultSingleTrackEndProfileId), (replacement.Id, replacement.RailTopHeight, replacement.DefaultSingleTrackEndProfileId));
    }

    [TestMethod]
    public void UnknownEntryIsReported()
    {
        using var context = new AppTestContext();
        var page = context.Render<StandardEditorPage>(p => p.Add(x => x.Id, Guid.NewGuid()));
        Assert.AreEqual("The catalogue entry doesn't exist.", page.Find(".error").TextContent);
        Assert.IsEmpty(page.FindAll("form"));
    }

    [TestMethod]
    public void NewAddOnIsSaved()
    {
        using var context = new AppTestContext();
        var page = context.Render<AddOnEditorPage>();

        page.Find("#shortName").Change("MY-ADDON");
        page.Find("form").Submit();

        Assert.AreEqual("MY-ADDON", User(context).AddOns.Single().ShortName);
    }

    [TestMethod]
    public void NewOverrideIsSavedAndTheSamePairIsRefused()
    {
        using var context = new AppTestContext();
        var page = context.Render<OverrideEditorPage>();
        var options = page.FindAll("#first option").Select(o => o.GetAttribute("value")).ToList();
        page.Find("#first").Change(options[0]);
        page.Find("#second").Change(options[1]);
        page.Find("#fit").Change(nameof(JointFit.Fits));
        page.Find("form").Submit();

        var entry = User(context).Overrides.Single();
        Assert.AreEqual(JointFit.Fits, entry.Fit);

        var again = context.Render<OverrideEditorPage>();
        again.Find("#first").Change(options[1]);
        again.Find("#second").Change(options[0]);
        again.Find("form").Submit();

        StringAssert.Contains(again.Find(".problems li").TextContent, "there already is an override");
    }

    [TestMethod]
    public void CataloguePageLinksToTheEditors()
    {
        using var context = new AppTestContext();
        var page = context.Render<CataloguePage>();
        Assert.AreEqual("catalogue/standard/new", page.Find("#new-standard").GetAttribute("href"));
        Assert.HasCount(BuiltInCatalogue.Instance.AddOns.Count, page.FindAll(".add-ons tbody tr"));
        Assert.IsTrue(page.FindAll(".standards .actions a").All(a => a.GetAttribute("href")!.StartsWith("catalogue/standard/", StringComparison.Ordinal)));
    }
}
