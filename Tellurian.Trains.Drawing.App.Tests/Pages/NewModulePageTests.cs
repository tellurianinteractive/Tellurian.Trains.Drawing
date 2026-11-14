using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class NewModulePageTests
{
    [TestMethod]
    public void StraightModuleBecomesAnUnsavedDraft()
    {
        using var context = new AppTestContext();
        var page = context.Render<NewModulePage>();

        page.Find("#name").Change("Straight 1000");
        page.Find("form").Submit();

        var draft = context.Workbench.Drafts.Values.Single();
        Assert.AreEqual(DrawingFileKind.Module, draft.Kind);
        Assert.AreEqual("Straight 1000", draft.Name);
        Assert.IsTrue(draft.HasUnsavedChanges);
        Assert.IsNull(draft.FileName);
        Assert.AreEqual(2, DrawingFileSerializer.DeserializeModule(draft.Json).Module.DefaultVariant.Exits.Count);
        Assert.EndsWith($"drawing/{draft.Id}", context.Services.GetRequiredService<NavigationManager>().Uri);
    }

    [TestMethod]
    public void DefaultStandardHasEndProfiles()
    {
        using var context = new AppTestContext();
        var empty = new Model.Catalogue.ModuleStandard { Id = Guid.NewGuid(), ShortName = "AAA-FIRST", Scale = "H0", NormalGauge = 16.5 };
        context.Services.GetRequiredService<global::Tellurian.Trains.Drawing.App.Catalogue.CatalogueService>().SaveUserAsync(Model.Catalogue.DrawingCatalogue.Empty with { Standards = [empty] }).Wait();

        var page = context.Render<NewModulePage>();

        Assert.AreNotEqual(empty.Id.ToString(), page.Find("#standard").GetAttribute("value"));
        Assert.IsNotEmpty(page.FindAll("#endProfile option"));
    }

    [TestMethod]
    public void CurveHidesTheStraightLength()
    {
        using var context = new AppTestContext();
        var page = context.Render<NewModulePage>();
        Assert.IsNotNull(page.Find("#straight"));

        page.FindAll("input[type=radio]")[1].Change(true);

        Assert.IsEmpty(page.FindAll("#straight"));
        Assert.IsNotNull(page.Find("#radius"));
    }

    [TestMethod]
    public void InvalidValuesAreReportedAndNothingIsCreated()
    {
        using var context = new AppTestContext();
        var page = context.Render<NewModulePage>();

        page.Find("#name").Change("");
        page.Find("#straight").Change("-5");
        page.Find("form").Submit();

        var errors = page.FindAll(".error li").Select(e => e.TextContent).ToList();
        CollectionAssert.Contains(errors, "Enter a name.");
        CollectionAssert.Contains(errors, "Straight length (mm) must be greater than 0.");
        Assert.IsEmpty(context.Workbench.Drafts);
    }

    [TestMethod]
    public void EndProfilesFollowTheStandard()
    {
        using var context = new AppTestContext();
        var page = context.Render<NewModulePage>();
        var options = page.FindAll("#standard option");
        var before = page.FindAll("#endProfile option").Select(o => o.TextContent).ToList();

        page.Find("#standard").Change(options[^1].GetAttribute("value"));

        var after = page.FindAll("#endProfile option").Select(o => o.TextContent).ToList();
        Assert.IsNotEmpty(after);
        CollectionAssert.AreNotEqual(before, after);
    }
}
