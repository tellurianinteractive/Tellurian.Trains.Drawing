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
        page.WaitForAssertion(() => Assert.HasCount(1, context.Workbench.Drafts));

        Assert.IsEmpty(page.FindAll(".busy-message"), "The wait message is gone when the module is created.");
        var draft = context.Workbench.Drafts.Values.Single();
        Assert.AreEqual(DrawingFileKind.Module, draft.Kind);
        Assert.AreEqual("Straight 1000", draft.Name);
        Assert.IsTrue(draft.HasUnsavedChanges);
        Assert.IsNull(draft.FileName);
        Assert.AreEqual(2, DrawingFileSerializer.DeserializeModule(draft.Json).Module.DefaultVariant.Exits.Count);
        Assert.EndsWith($"drawing/{draft.Id}", context.Services.GetRequiredService<NavigationManager>().Uri);
    }

    [TestMethod]
    public void FremoModuleNeedsNoName()
    {
        using var context = new AppTestContext();
        var page = context.Render<NewModulePage>();

        page.Find("#name").Change("");
        page.Find("#ownerPrefix").Change("SFjä");
        page.Find("#fremoNumber").Change("12");
        page.Find("#ownerName").Change("Stefan Fjällemark");
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.HasCount(1, context.Workbench.Drafts));

        var draft = context.Workbench.Drafts.Values.Single();
        Assert.AreEqual("SFjä 012", draft.Name);
        var module = DrawingFileSerializer.DeserializeModule(draft.Json).Module;
        Assert.AreEqual(12, module.FremoNumber);
        Assert.AreEqual(new Model.Modules.Owner { Name = "Stefan Fjällemark", Prefix = "SFjä" }, module.Owners.Single());
    }

    [TestMethod]
    public void FremoNumberWithoutPrefixIsReported()
    {
        using var context = new AppTestContext();
        var page = context.Render<NewModulePage>();

        page.Find("#fremoNumber").Change("12");
        page.Find("form").Submit();

        CollectionAssert.Contains(page.FindAll(".error li").Select(e => e.TextContent).ToList(), "Enter the owner's FREMO prefix with the FREMO number.");
        Assert.IsEmpty(context.Workbench.Drafts);
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
        CollectionAssert.Contains(errors, "Enter a name, or a FREMO number with the owner's prefix.");
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

    [TestMethod]
    public void EndBCanHaveAnotherEndProfileAndTheModuleCanBeAnEndewender()
    {
        using var context = new AppTestContext();
        var page = context.Render<NewModulePage>();
        var first = page.Find("#endProfile").GetAttribute("value");
        var second = page.FindAll("#secondEndProfile option").Select(o => o.GetAttribute("value")).First(v => v != Guid.Empty.ToString() && v != first);

        page.Find("#secondEndProfile").Change(second);
        page.Find("#endewender").Change(true);
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.HasCount(1, context.Workbench.Drafts));

        var module = DrawingFileSerializer.DeserializeModule(context.Workbench.Drafts.Values.Single().Json).Module;
        Assert.IsTrue(module.IsEndProfileSwitcher);
        CollectionAssert.AreEqual(new[] { first, second }, module.DefaultVariant.Exits.Select(e => e.EndProfile.Id.ToString()).ToArray());
    }

    // The module drawing fills the window beside the file actions, and can be printed with its name as title.
    [TestMethod]
    public void ModuleDrawingFillsTheWindowAndCanBePrinted()
    {
        using var context = new AppTestContext();
        var newModule = context.Render<NewModulePage>();
        newModule.Find("#name").Change("Print me");
        newModule.Find("form").Submit();
        newModule.WaitForAssertion(() => Assert.HasCount(1, context.Workbench.Drafts));
        var draft = context.Workbench.Drafts.Values.Single();

        var page = context.Render<DrawingPage>(p => p.Add(x => x.Id, draft.Id));

        page.WaitForElement(".editor-layout .editor-canvas .drawing-canvas");
        StringAssert.Contains(page.Find(".print-sheet").InnerHtml, ">Print me</text>");
        page.Find("#print").Click();
        context.JSInterop.VerifyInvoke("print");
    }
}
