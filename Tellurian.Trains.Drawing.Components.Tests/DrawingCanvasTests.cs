using Bunit;
using Microsoft.AspNetCore.Components;
using Tellurian.Trains.Drawing.Components.Canvas;
using Tellurian.Trains.Drawing.Editing.Viewing;

namespace Tellurian.Trains.Drawing.Components.Tests;

[TestClass]
public sealed class DrawingCanvasTests
{
    private const string ModulePath = "./_content/Tellurian.Trains.Drawing.Components/Canvas/DrawingCanvas.razor.js";

    private static readonly Viewport View = new() { Center = new(1000, 500), Scale = 0.5, Width = 800, Height = 600 };

    private static BunitContext Context()
    {
        var context = new BunitContext();
        context.JSInterop.SetupModule(ModulePath).Mode = JSRuntimeMode.Loose;
        return context;
    }

    [TestMethod]
    public void RendersTheViewBoxAndTheWorldGroupFlipped()
    {
        using var context = Context();
        var canvas = context.Render<DrawingCanvas>(p => p
            .Add(c => c.Viewport, View)
            .AddChildContent("<circle class=\"shape\" r=\"10\" />"));

        Assert.AreEqual("200 -1100 1600 1200", canvas.Find("svg").GetAttribute("viewBox"));
        var world = canvas.Find("g.drawing-world");
        Assert.AreEqual("scale(1,-1)", world.GetAttribute("transform"));
        Assert.IsNotNull(world.QuerySelector("circle.shape"));
    }

    [TestMethod]
    public async Task RendersStaticallyWithoutAnyScript()
    {
        var html = await StaticRendering.RenderAsync<DrawingCanvas>(new Dictionary<string, object?>
        {
            [nameof(DrawingCanvas.Viewport)] = View,
            [nameof(DrawingCanvas.Label)] = "Module",
        });
        StringAssert.Contains(html, "viewBox=\"200 -1100 1600 1200\"");
        StringAssert.Contains(html, "aria-label=\"Module\"");
    }

    [TestMethod]
    public void GridSpacingFollowsTheScale()
    {
        using var context = Context();
        var canvas = context.Render<DrawingCanvas>(p => p.Add(c => c.Viewport, View));
        var spacing = NiceSteps.GridSpacing(View.Scale);
        Assert.AreEqual(spacing.ToString(System.Globalization.CultureInfo.InvariantCulture), canvas.Find("pattern.drawing-grid").GetAttribute("width"));
    }

    [TestMethod]
    public void GridAndScaleBarCanBeHidden()
    {
        using var context = Context();
        var canvas = context.Render<DrawingCanvas>(p => p.Add(c => c.Viewport, View).Add(c => c.ShowGrid, false).Add(c => c.ShowScaleBar, false));
        Assert.IsEmpty(canvas.FindAll("pattern"));
        Assert.IsEmpty(canvas.FindAll(".drawing-scale-bar"));
    }

    [TestMethod]
    public void ScaleBarShowsARoundLength()
    {
        using var context = Context();
        var canvas = context.Render<DrawingCanvas>(p => p.Add(c => c.Viewport, View));
        Assert.AreEqual("200 mm", canvas.Find(".drawing-scale-bar-text").TextContent, "At 0.5 px/mm, 120 px is 240 mm, rounded down to 200 mm.");
        Assert.AreEqual("width: 100px", canvas.Find(".drawing-scale-bar-line").GetAttribute("style"));

        var zoomedOut = context.Render<DrawingCanvas>(p => p.Add(c => c.Viewport, View with { Scale = 0.02 }));
        Assert.AreEqual("5 m", zoomedOut.Find(".drawing-scale-bar-text").TextContent);
    }

    [TestMethod]
    public void ViewReportedByTheScriptIsPassedOn()
    {
        using var context = Context();
        Viewport? reported = null;
        var canvas = context.Render<DrawingCanvas>(p => p
            .Add(c => c.Viewport, View)
            .Add(c => c.ViewportChanged, EventCallback.Factory.Create<Viewport>(this, v => reported = v)));

        canvas.InvokeAsync(() => canvas.Instance.ViewChanged(10, 20, 2, 1024, 768)).Wait();

        Assert.AreEqual(View with { Center = new(10, 20), Scale = 2, Width = 1024, Height = 768 }, reported);
    }

    [TestMethod]
    public void PointerEventsArriveInWorldCoordinates()
    {
        using var context = Context();
        CanvasPointerEventArgs? down = null;
        var canvas = context.Render<DrawingCanvas>(p => p
            .Add(c => c.Viewport, View)
            .Add(c => c.OnPointerDown, EventCallback.Factory.Create<CanvasPointerEventArgs>(this, e => down = e)));

        canvas.InvokeAsync(() => canvas.Instance.PointerDown(1200, 700, 500, 200, 0, 1, 2 | 8, "pen")).Wait();

        Assert.IsNotNull(down);
        Assert.AreEqual(new Point(1200, 700), down.World);
        Assert.AreEqual((true, true, false, "pen"), (down.CtrlKey, down.MetaKey, down.ShiftKey, down.PointerType));
        Assert.IsTrue(down.IsAdditive);
    }
}
