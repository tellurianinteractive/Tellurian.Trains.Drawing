using Bunit;
using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Drawing.Model.Catalogue;
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
        Assert.IsLessThan(BuiltInCatalogue.Instance.EndProfiles.Count, page.FindAll(".end-profiles tbody tr").Count);
    }

    [TestMethod]
    public void ListsTheBuiltInEndProfilesAndStandards()
    {
        using var context = new AppTestContext();
        context.UseCountry("SE");
        var page = context.Render<CataloguePage>();
        Assert.HasCount(BuiltInCatalogue.Instance.EndProfiles.Count, page.FindAll(".end-profiles tbody tr"));
        Assert.HasCount(BuiltInCatalogue.Instance.Standards.Count, page.FindAll(".standards tbody tr"));
        Assert.IsTrue(page.FindAll(".end-profiles .badge").All(b => b.TextContent == "Built in"));
    }

    [TestMethod]
    public void ImportPreviewSelectsNewProfilesAndImportsThem()
    {
        using var context = new AppTestContext();
        var profile = BuiltInCatalogue.Instance.EndProfiles[0].Copy("REGISTRY-1");
        context.Files.FilesToOpen = [new OpenedFile("r.tdendprofile", DrawingFileSerializer.Serialize(profile))];
        var page = context.Render<CataloguePage>();

        page.Find("#import-files").Click();
        Assert.AreEqual("New", page.Find(".import-preview tbody td:nth-child(3)").TextContent);
        page.Find("#apply-import").Click();

        Assert.IsEmpty(page.FindAll(".import-preview"));
        StringAssert.Contains(page.Find(".status").TextContent, "1");
        Assert.IsTrue(page.FindAll(".end-profiles tbody tr").Any(r => r.TextContent.Contains("REGISTRY-1") && r.TextContent.Contains("Yours")));
    }

    [TestMethod]
    public void CopyAddsAUserProfile()
    {
        using var context = new AppTestContext();
        context.UseCountry("SE");
        var page = context.Render<CataloguePage>();
        var first = BuiltInCatalogue.Instance.EndProfiles.OrderBy(p => p.Scale).ThenBy(p => p.Designation).First();

        page.Find(".end-profiles tbody tr .actions button").Click();

        StringAssert.Contains(page.Find(".status").TextContent, string.Join(" ", first.Designation, "copy"));
        Assert.HasCount(BuiltInCatalogue.Instance.EndProfiles.Count + 1, page.FindAll(".end-profiles tbody tr"));
    }
}
