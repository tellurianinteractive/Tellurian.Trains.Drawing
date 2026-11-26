using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Catalogues;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Drawing.Model.Catalogues;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class CatalogueEditorPageTests
{
    private static Catalogue User(AppTestContext context) =>
        context.Services.GetRequiredService<CatalogueService>().GetUserAsync().Result;

    private static string Uri(AppTestContext context) => context.Services.GetRequiredService<NavigationManager>().Uri;

    [TestMethod]
    public void NewStandardIsSavedInTheCatalogue()
    {
        using var context = new AppTestContext();
        var page = context.Render<StandardEditorPage>();

        page.Find("#shortName").Change("MY-H0");
        page.Find("#countries").Change("se, no");
        page.Find("form").Submit();

        var standard = User(context).Standards.Single(s => s.ShortName == "MY-H0");
        Assert.AreEqual(("MY-H0", "H0", "SE,NO"), (standard.ShortName, standard.ScaleName, string.Join(",", standard.Countries!)));
        Assert.EndsWith("catalogue", Uri(context));
    }

    [TestMethod]
    public void AnEditedStandardKeepsItsRegistryId()
    {
        using var context = new AppTestContext();
        var registry = SampleCatalogue.Instance.Standards[0].With(s =>
            s.ExternalReferences = Schedules.Model.Layouts.ExternalReferences.WithModuleRegistryId(null, Schedules.Model.Layouts.ExternalReferences.StandardId, 2));
        context.Services.GetRequiredService<CatalogueService>().SaveUserAsync(User(context).With(registry)).Wait();
        var page = context.Render<StandardEditorPage>(p => p.Add(x => x.Id, registry.Id));

        page.Find("#countries").Change("SE");
        page.Find("form").Submit();

        var saved = User(context).FindStandard(registry.Id!.Value)!;
        Assert.AreEqual(2, Schedules.Model.Layouts.ExternalReferences.ModuleRegistryId(saved.ExternalReferences, Schedules.Model.Layouts.ExternalReferences.StandardId));
    }

    [TestMethod]
    public void OnlyTheShortNameScaleAndCountriesAreShown()
    {
        using var context = new AppTestContext();
        var page = context.Render<StandardEditorPage>(p => p.Add(x => x.Id, SampleCatalogue.Instance.Standards[0].Id));

        CollectionAssert.AreEquivalent(new[] { "shortName", "scale", "countries" }, page.FindAll("form input, form select").Select(e => e.Id).ToArray());
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
        Assert.IsFalse(problems.Any(p => p.Contains("gauge", StringComparison.OrdinalIgnoreCase)), "A gauge is given per track in the end profiles.");
        Assert.HasCount(SampleCatalogue.Instance.Standards.Count, User(context).Standards, "Nothing is saved.");
    }

    [TestMethod]
    public void AnEditedStandardIsSavedInItsPlace()
    {
        using var context = new AppTestContext();
        var standard = SampleCatalogue.Instance.Standards[0];
        var page = context.Render<StandardEditorPage>(p => p.Add(x => x.Id, standard.Id));
        Assert.AreEqual(standard.ShortName, page.Find("#shortName").GetAttribute("value"));

        page.Find("#shortName").Change("RENAMED");
        page.Find("form").Submit();

        var saved = User(context).FindStandard(standard.Id!.Value)!;
        Assert.AreEqual("RENAMED", saved.ShortName);
        Assert.AreEqual((standard.NormalGauge, standard.DefaultSingleTrackEndProfileId), (saved.NormalGauge, saved.DefaultSingleTrackEndProfileId), "The values not shown are kept.");
        Assert.HasCount(SampleCatalogue.Instance.Standards.Count, User(context).Standards);
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

        Assert.IsTrue(User(context).AddOns.Any(a => a.ShortName == "MY-ADDON"));
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
        context.UseCountry("SE");
        var page = context.Render<CataloguePage>();
        Assert.AreEqual("catalogue/standard/new", page.Find("#new-standard").GetAttribute("href"));
        Assert.HasCount(SampleCatalogue.Instance.AddOns.Count, page.FindAll(".add-ons tbody tr"));
        Assert.IsTrue(page.FindAll(".standards .actions a").All(a => a.GetAttribute("href")!.StartsWith("catalogue/standard/", StringComparison.Ordinal)));
    }
}
