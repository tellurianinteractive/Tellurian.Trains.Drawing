using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Documents;
using Tellurian.Trains.Drawing.App.Editors;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Drawing.Components.Canvas;
using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.AutoDraw;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class ModuleEditorTests
{
    private const double Length = 2000;

    private static IRenderedComponent<DrawingPage> OpenStraightModule(AppTestContext context)
    {
        var catalogue = BuiltInCatalogue.Instance;
        AutoDrawExit west = new() { Label = "W", Direction = ExitDirection.West, EndProfile = "FREMO-H0-B96" };
        var module = ModuleAutoDraw.Create(new() { Name = "Tellby", Standard = "FREMO/H0-RE", Straight = Length, Exits = [west, west with { Label = "E", Direction = ExitDirection.East }] }, catalogue).Module!;
        var json = DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, catalogue.FindEndProfile));
        var draft = context.Services.GetRequiredService<DrawingDocuments>().CreateAsync(json).Result;
        var page = context.Render<DrawingPage>(p => p.Add(x => x.Id, draft.Id));
        page.WaitForElement(".sections-pane");
        return page;
    }

    private static Point OnTrack(IRenderedComponent<DrawingPage> page, double distance) =>
        page.FindComponent<ModuleEditor>().Instance.Current!.Module.DefaultVariant.MainPath!.ToPath().PoseAt(distance).Position;

    /// <summary>A click in the drawing, as the canvas script reports it: in module millimetres.</summary>
    private static void Click(IRenderedComponent<DrawingPage> page, Point at)
    {
        var canvas = page.FindComponent<DrawingCanvas>();
        canvas.InvokeAsync(() => canvas.Instance.PointerDown(at.X, at.Y, 0, 0, 0, 1, 0, "mouse")).Wait();
        canvas.InvokeAsync(() => canvas.Instance.PointerUp(at.X, at.Y, 0, 0, 0, 0, 0, "mouse")).Wait();
    }

    [TestMethod]
    public void ModuleOpensInTheEditorAsOneSection()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);

        Assert.IsNotNull(page.FindComponent<ModuleEditor>());
        Assert.HasCount(1, page.FindAll(".module-sections tbody tr"));
        Assert.AreEqual("2000 mm", page.Find("#section-length-1").TextContent);
        Assert.IsNotNull(page.Find(".module-properties"), "The identification is edited beside the drawing.");
    }

    [TestMethod]
    public void ClickWithTheJointToolDividesTheModule()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);

        page.Find("#tool-joint").Click();
        Click(page, OnTrack(page, 700));

        Assert.HasCount(2, page.FindAll(".module-sections tbody tr"));
        Assert.AreEqual("700 mm", page.Find("#section-length-1").TextContent);
        Assert.AreEqual("1300 mm", page.Find("#section-length-2").TextContent);
    }

    [TestMethod]
    public void SelectedJointsPositionIsTypedExactly()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);
        page.Find("#tool-joint").Click();
        Click(page, OnTrack(page, 700));
        page.Find("#tool-select").Click();

        Click(page, OnTrack(page, 700));
        Assert.AreEqual("Joint between sections 1 and 2", page.Find("#joint-title").TextContent);
        var distance = page.Find(".sections-pane .number-field-input");
        distance.Input("1234");
        distance.Change("1234");

        Assert.AreEqual("1234 mm", page.Find("#section-length-1").TextContent);
        Assert.HasCount(2, page.FindAll(".joint-points tbody tr"), "A straight joint has its two ends.");
    }

    [TestMethod]
    public async Task SectionsAreSavedToTheDraft()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);
        page.Find("#tool-joint").Click();
        Click(page, OnTrack(page, 500));

        var editor = page.FindComponent<ModuleEditor>();
        await page.InvokeAsync(() => editor.Instance.FlushAsync());

        var variant = DrawingFileSerializer.DeserializeModule(context.Workbench.Drafts.Values.Single().Json).Module.DefaultVariant;
        Assert.HasCount(2, variant.Sections);
        Assert.AreEqual(("1", "2"), (variant.InternalJoints[0].FirstSection, variant.InternalJoints[0].SecondSection));
    }
}
