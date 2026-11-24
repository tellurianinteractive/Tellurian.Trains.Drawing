using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Documents;
using Tellurian.Trains.Drawing.App.Editors;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Schedules.Model.Geometry;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class HallEditorTests
{
    [TestMethod]
    public void NewHallOpensInTheEditor()
    {
        using var context = new AppTestContext();
        var page = context.Render<NewHallPage>();

        page.Find("#hall-name").Change("Club hall");
        page.Find("#hall-width").Change("15");
        page.Find("#hall-depth").Change("9");
        page.Find("form").Submit();

        var draft = context.Workbench.Drafts.Values.Single();
        var hall = DrawingFileSerializer.DeserializeHall(draft.Json);
        Assert.AreEqual(DrawingFileKind.Hall, draft.Kind);
        Assert.AreEqual("Club hall", hall.Name);
        Assert.AreEqual(15000.0 * 9000, hall.FloorArea, Tolerance.Length);
        Assert.EndsWith($"drawing/{draft.Id}", context.Services.GetRequiredService<NavigationManager>().Uri);
    }

    [TestMethod]
    public void NewHallNeedsAName()
    {
        using var context = new AppTestContext();
        var page = context.Render<NewHallPage>();

        page.Find("form").Submit();

        Assert.IsEmpty(context.Workbench.Drafts);
        Assert.IsNotNull(page.Find("ul.error"));
    }

    [TestMethod]
    public void DrawingPageShowsTheHallEditorWithDerivedMeasures()
    {
        using var context = new AppTestContext();
        var page = RenderHall(context, Hall.Rectangle("Hall", 12000, 8000).With(h => h.Add(Obstacle.RoundPillar(new(4000, 4000), 400))));

        Assert.IsNotNull(page.FindComponent<HallEditor>());
        Assert.AreEqual("4", page.Find("#wall-count").TextContent);
        Assert.AreEqual("1", page.Find("#obstacle-count").TextContent);
        Assert.AreEqual("96 m²", page.Find("#floor-area").TextContent);
        Assert.AreEqual("95.9 m²", page.Find("#effective-floor-area").TextContent, "Without the pillar.");
        Assert.IsEmpty(page.FindAll("#save-to-catalogue"), "Only end profiles go in the catalogue.");
    }

    [TestMethod]
    public void TypedWidthResizesTheHallAndIsUndoable()
    {
        using var context = new AppTestContext();
        var page = RenderHall(context, Hall.Rectangle("Hall", 12000, 8000));
        var before = page.Find("#floor-area").TextContent;

        var width = page.FindAll(".walls-pane .property-grid input")[0];
        width.Input("20");
        width.Change("20");
        Assert.AreNotEqual(before, page.Find("#floor-area").TextContent);
        Assert.AreEqual(20000.0 * 8000, page.FindComponent<HallEditor>().Instance.Current!.FloorArea, Tolerance.Length);

        page.Find("#undo").Click();
        Assert.AreEqual(before, page.Find("#floor-area").TextContent);
    }

    [TestMethod]
    public void HallLengthsAreShownInMetres()
    {
        using var context = new AppTestContext();
        var page = RenderHall(context, Hall.Rectangle("Hall", 12500, 8000));

        var width = page.FindAll(".walls-pane .property-grid input")[0];
        Assert.AreEqual("12.5", width.GetAttribute("value"));
        width.Change("450 mm");
        Assert.AreEqual(450, page.FindComponent<HallEditor>().Instance.Current!.Outline.ToPath().Bounds.Width, Tolerance.Length, "A unit can still be typed.");
    }

    [TestMethod]
    public void DoorPositionIsShownAndTypedFromTheCorners()
    {
        using var context = new AppTestContext();
        var hall = Hall.Rectangle("Hall", 12000, 8000).With(h => h.Add(new Door { Position = new(new(3000, 0), new(90)), Width = 1000 }));
        var page = RenderHall(context, hall, selection: "Door:0");
        Assert.AreEqual(2, page.FindAll(".dimension").Count, "Dimension lines from the corners to the opening.");

        var fields = page.FindAll(".number-field-input");
        var fromCorner = fields.Single(f => f.GetAttribute("value") == "2.5");
        fromCorner.Change("4");

        Assert.AreEqual(4500, page.FindComponent<HallEditor>().Instance.Current!.Doors[0].Position.Position.X, Tolerance.Length);
    }

    [TestMethod]
    public async Task SnapGridIsRememberedAndDefaultsToATenthOfAMetre()
    {
        using var context = new AppTestContext();
        var page = RenderHall(context, Hall.Rectangle("Hall", 12000, 8000));
        Assert.AreEqual("100", page.Find("#snap-grid").GetAttribute("value"));

        page.Find("#snap-grid").Change("500");

        Assert.AreEqual("500", await context.Workbench.GetValueAsync(HallEditor.GridSpacingKey));
        var reopened = RenderHall(context, Hall.Rectangle("Other hall", 12000, 8000));
        Assert.AreEqual("500", reopened.Find("#snap-grid").GetAttribute("value"));
    }

    [TestMethod]
    public void HallHasAPrintSheetWithItsName()
    {
        using var context = new AppTestContext();
        var page = RenderHall(context, Hall.Rectangle("Club hall", 12000, 8000).With(h => h.Address = "Station Road 1"));

        var sheet = page.Find(".print-sheet svg.print-drawing");
        StringAssert.Contains(sheet.InnerHtml, ">Club hall</text>");
        StringAssert.Contains(sheet.InnerHtml, ">Station Road 1</text>");
        StringAssert.Contains(sheet.InnerHtml, ">2 m</text>", "The scale bar.");
        StringAssert.Contains(sheet.InnerHtml, ">12 × 8 m</text>", "The size in the middle of the hall.");
        StringAssert.Contains(sheet.InnerHtml, ">Floor area: 96 m²</text>");
        StringAssert.Contains(sheet.InnerHtml, ">Effective floor area: 96 m²</text>");
        page.Find("#print").Click();
        Assert.AreEqual(1, context.JSInterop.Invocations.Count(i => i.Identifier == "print"));
    }

    [TestMethod]
    public void AddCornerAddsAWall()
    {
        using var context = new AppTestContext();
        var page = RenderHall(context, Hall.Rectangle("Hall", 12000, 8000));

        page.Find("#add-corner").Click();

        Assert.AreEqual("5", page.Find("#wall-count").TextContent);
        Assert.HasCount(5, page.FindAll(".wall-corners tbody tr"));
    }

    [TestMethod]
    public async Task EditsAreSavedToTheDraft()
    {
        using var context = new AppTestContext();
        var page = RenderHall(context, Hall.Rectangle("Hall", 12000, 8000));
        var editor = page.FindComponent<HallEditor>();

        page.Find("#add-corner").Click();
        await page.InvokeAsync(() => editor.Instance.FlushAsync());

        var draft = context.Workbench.Drafts.Values.Single();
        Assert.IsTrue(draft.HasUnsavedChanges);
        Assert.AreEqual(5, DrawingFileSerializer.DeserializeHall(draft.Json).WallCount);
    }

    [TestMethod]
    public void DoorCanBeMarkedAsModuleEntrance()
    {
        using var context = new AppTestContext();
        var hall = Hall.Rectangle("Hall", 12000, 8000).With(h => h.Add(new Door { Position = new(new(3000, 0), new(90)), Width = 1000, Label = "Gate" }));
        // The editor reopens with its stored selection (AP-7); a click on the canvas would need JS.
        var page = RenderHall(context, hall, selection: "Door:0");
        Assert.AreEqual("None marked", page.Find("#module-entrances").TextContent);

        page.Find("#module-entrance").Change(true);

        Assert.AreEqual("Gate", page.Find("#module-entrances").TextContent);
        Assert.IsTrue(page.FindComponent<HallEditor>().Instance.Current!.Doors[0].IsModuleEntrance);
    }

    [TestMethod]
    public void EveryHallEditorPaneHasItsOwnHelp()
    {
        using var context = new AppTestContext();
        var page = RenderHall(context, Hall.Rectangle("Hall", 12000, 8000));
        page.WaitForElement("#floor-area");

        Layout.HelpTests.AssertPaneHelp(page, ("archive", "Archive"), ("halleditor", "Edit"), ("hallwalls", "Walls"), ("hallproperties", "Hall"), ("hallderived", "Derived properties"));
    }

    private static IRenderedComponent<DrawingPage> RenderHall(AppTestContext context, Hall hall, string? selection = null)
    {
        var draft = context.Services.GetRequiredService<DrawingDocuments>().CreateAsync(DrawingFileSerializer.Serialize(hall)).Result;
        if (selection is not null)
        {
            var state = new EditorState(hall.Outline.ToPath().Bounds.Center.X, hall.Outline.ToPath().Bounds.Center.Y, 0.05, [selection]);
            // Stored the way the workbench stores editor states: camel-case JSON.
            context.Workbench.SetValueAsync(EditorState.StorageKey(draft.Id.ToString()), System.Text.Json.JsonSerializer.Serialize(state, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))).Wait();
        }
        var page = context.Render<DrawingPage>(p => p.Add(x => x.Id, draft.Id));
        page.WaitForElement("#wall-count");
        return page;
    }
}
