using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Catalogues;
using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Drawing.Model.Catalogues;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class CataloguePageTests
{
    [TestMethod]
    [DataRow(null), DataRow("DE")]
    public void OutsideSwedenTheMmmEntriesAreNotListed(string? country)
    {
        using var context = new AppTestContext();
        if (country is not null) context.UseCountry(country);
        var page = context.Render<CataloguePage>();

        var standards = page.FindAll(".standards tbody tr").Select(r => r.TextContent).ToArray();
        Assert.IsNotEmpty(standards);
        Assert.IsFalse(standards.Any(s => s.Contains("MMM", StringComparison.Ordinal)), string.Join(" | ", standards));
        Assert.IsEmpty(page.FindAll(".add-ons tbody tr"));
        Assert.IsLessThan(SampleCatalogue.Instance.EndProfiles.Count, page.FindAll(".end-profiles tbody tr").Count);
    }

    [TestMethod]
    public void ListsTheBuiltInEndProfilesAndStandards()
    {
        using var context = new AppTestContext();
        context.UseCountry("SE");
        var page = context.Render<CataloguePage>();
        Assert.HasCount(SampleCatalogue.Instance.EndProfiles.Count, page.FindAll(".end-profiles tbody tr"));
        Assert.HasCount(SampleCatalogue.Instance.Standards.Count, page.FindAll(".standards tbody tr"));
        Assert.IsTrue(page.FindAll(".end-profiles .badge").All(b => b.TextContent == "Built in"));
    }

    [TestMethod]
    public void ImportPreviewSelectsNewProfilesAndImportsThem()
    {
        using var context = new AppTestContext();
        var profile = SampleCatalogue.Instance.EndProfiles[0].Copy("REGISTRY-1");
        context.Files.FilesToOpen = [new OpenedFile("r.tdendprofile", DrawingFileSerializer.Serialize(profile))];
        var page = context.Render<CataloguePage>();

        page.Find("#import-files").Click();
        Assert.AreEqual("New", page.Find(".import-preview tbody td:nth-child(3)").TextContent);
        page.Find("#apply-import").Click();

        Assert.IsEmpty(page.FindAll(".import-preview"));
        StringAssert.Contains(page.Find(".status").TextContent, "1");
        Assert.IsTrue(page.FindAll(".end-profiles tbody tr").Any(r => r.TextContent.Contains("REGISTRY-1")));
    }

    [TestMethod]
    public void CopyAddsAUserProfile()
    {
        using var context = new AppTestContext();
        context.UseCountry("SE");
        var page = context.Render<CataloguePage>();
        var first = SampleCatalogue.Instance.EndProfiles.OrderBy(p => p.Scale).ThenBy(p => p.Designation).First();

        page.Find(".end-profiles tbody tr .actions button").Click();

        StringAssert.Contains(page.Find(".status").TextContent, string.Join(" ", first.Designation, "copy"));
        Assert.HasCount(SampleCatalogue.Instance.EndProfiles.Count + 1, page.FindAll(".end-profiles tbody tr"));
    }

    [TestMethod]
    public void AnEmptyCatalogueTellsHowToGetOne()
    {
        using var context = new AppTestContext(withCatalogue: false);
        var page = context.Render<CataloguePage>();

        StringAssert.Contains(page.Find("#catalogue-empty").TextContent, "import a catalogue file");
        Assert.IsEmpty(page.FindAll(".standards tbody tr"));
    }

    [TestMethod]
    public void ImportingACatalogueFileFillsTheCatalogue()
    {
        using var context = new AppTestContext(withCatalogue: false);
        context.Files.FilesToOpen = [new OpenedFile("sample.catalogue.json", SampleCatalogue.Json)];
        var page = context.Render<CataloguePage>();

        page.Find("#import-files").Click();
        StringAssert.Contains(page.Find("#import-entries").TextContent, $"{SampleCatalogue.Instance.Standards.Count} standards");
        page.Find("#apply-import").Click();

        Assert.IsEmpty(page.FindAll("#catalogue-empty"));
        Assert.IsNotEmpty(page.FindAll(".standards tbody tr"));
    }

    /// <summary>An end profile of the catalogue as the Module Registry gives one it has no drawing of.</summary>
    private static EndProfile Shapeless(AppTestContext context)
    {
        var like = SampleCatalogue.Instance.EndProfiles[0];
        var profile = new EndProfile { Id = Guid.NewGuid(), Designation = "REGISTRY-SHAPELESS", Scale = like.Scale, Standards = [.. like.Standards] };
        context.Services.GetRequiredService<CatalogueService>().SaveEndProfileAsync(profile).Wait();
        return profile;
    }

    [TestMethod]
    public void AnEndProfileWithoutAShapeCanBeDrawn()
    {
        using var context = new AppTestContext();
        var profile = Shapeless(context);
        var page = context.Render<CataloguePage>();

        var row = page.FindAll(".end-profiles tbody tr").Single(r => r.TextContent.Contains(profile.Designation));
        Assert.AreEqual(NewEndProfilePage.Url(profile.Id), row.QuerySelector("a.create-drawing")!.GetAttribute("href"));
        Assert.IsNull(row.QuerySelector(".edit-profile"));
        Assert.IsNotNull(row.QuerySelector(".no-drawing"));
    }

    [TestMethod]
    public void TheModuleRegistrysCatalogueIsFetchedAndPreviewed()
    {
        using var context = new AppTestContext(withCatalogue: false);
        context.Registry.Json = SampleCatalogue.Json;
        var page = context.Render<CataloguePage>();

        page.Find("#fetch-registry").Click();

        Assert.HasCount(SampleCatalogue.Instance.EndProfiles.Count, page.FindAll(".import-preview tbody tr"));
        page.Find("#apply-import").Click();
        Assert.HasCount(SampleCatalogue.Instance.Standards.Count, context.Services.GetRequiredService<CatalogueService>().GetUserAsync().Result.Standards);
    }

    [TestMethod]
    public void ARegistryThatCantBeReachedIsShown()
    {
        using var context = new AppTestContext();
        context.Registry.Unreachable = true;
        var page = context.Render<CataloguePage>();

        page.Find("#fetch-registry").Click();

        StringAssert.Contains(page.Find(".error").TextContent, "Failed to fetch");
    }

    [TestMethod]
    public void TheDrawingOfAnEndProfileWithoutAShapeKeepsItsIdentity()
    {
        using var context = new AppTestContext();
        var profile = Shapeless(context);
        context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo(NewEndProfilePage.Url(profile.Id));
        var page = context.Render<NewEndProfilePage>();

        Assert.AreEqual(profile.Designation, page.Find("#designation").GetAttribute("value"));
        page.Find("form").Submit();

        page.WaitForAssertion(() => Assert.HasCount(1, context.Workbench.Drafts));
        var drawn = DrawingFileSerializer.DeserializeEndProfile(context.Workbench.Drafts.Values.Single().Json);
        Assert.AreEqual((profile.Id, profile.Designation, profile.Scale), (drawn.Id, drawn.Designation, drawn.Scale));
        Assert.IsTrue(drawn.HasOutline());
    }

    [TestMethod]
    public void AModuleExitCantHaveAnEndProfileWithoutAShape()
    {
        using var context = new AppTestContext();
        var profile = Shapeless(context);
        var page = context.Render<NewModulePage>();

        Assert.IsFalse(page.FindAll("option").Any(o => o.TextContent.Contains(profile.Designation)));
    }
}
