using Tellurian.Trains.Drawing.Editing.Viewing;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class ViewportTests
{
    private static Viewport View() => new() { Center = new(1000, 500), Scale = 0.5, Width = 800, Height = 600 };

    [TestMethod]
    public void CentreOfTheCanvasIsTheCentreOfTheView()
    {
        var view = View();
        Assert.IsTrue(view.ScreenToWorld(new(400, 300)).IsCloseTo(new(1000, 500), Precision));
        Assert.IsTrue(view.WorldToScreen(new(1000, 500)).IsCloseTo(new(400, 300), Precision));
    }

    [TestMethod]
    public void WorldYPointsUpOnScreen()
    {
        var view = View();
        var above = view.WorldToScreen(new(1000, 600));
        Assert.AreEqual(250, above.Y, Precision, "100 mm up in the world is 50 px up on screen.");
        Assert.IsTrue(view.ScreenToWorld(above).IsCloseTo(new(1000, 600), Precision));
    }

    [TestMethod]
    public void VisibleWorldAndViewBox()
    {
        var view = View();
        var visible = view.VisibleWorld;
        Assert.IsTrue(visible.Min.IsCloseTo(new(200, -100), Precision));
        Assert.IsTrue(visible.Max.IsCloseTo(new(1800, 1100), Precision));
        Assert.AreEqual(new ViewBox(200, -1100, 1600, 1200), view.ViewBox);
        Assert.AreEqual("200 -1100 1600 1200", view.ViewBox.ToString());
    }

    [TestMethod]
    public void ZoomKeepsThePointUnderThePointer()
    {
        var view = View();
        var pointer = new Point(100, 450);
        var before = view.ScreenToWorld(pointer);
        var zoomed = view.ZoomAt(pointer, 2.5);
        Assert.AreEqual(1.25, zoomed.Scale, Precision);
        Assert.IsTrue(zoomed.ScreenToWorld(pointer).IsCloseTo(before, Precision));
    }

    [TestMethod]
    public void ZoomIsClamped()
    {
        Assert.AreEqual(Viewport.MaximumScale, View().ZoomAt(new(0, 0), 1e9).Scale);
        Assert.AreEqual(Viewport.MinimumScale, View().ZoomAt(new(0, 0), 1e-9).Scale);
    }

    [TestMethod]
    public void PanMovesTheDrawingWithThePointer()
    {
        var view = View();
        var world = new Point(1200, 700);
        var screen = view.WorldToScreen(world);
        var panned = view.PanBy(30, -20);
        Assert.IsTrue(panned.WorldToScreen(world).IsCloseTo(new(screen.X + 30, screen.Y - 20), Precision));
    }

    [TestMethod]
    public void FitShowsTheWholeAreaWithAMargin()
    {
        var area = new BoundingBox(new(0, 0), new(4000, 1000));
        var view = Viewport.Fit(area, 840, 600, margin: 20);
        Assert.AreEqual(0.2, view.Scale, Precision, "4000 mm across 800 px.");
        Assert.IsTrue(view.Center.IsCloseTo(new(2000, 500), Precision));
        Assert.IsTrue(view.WorldToScreen(area.Min).X >= 20 - Precision);
        Assert.IsTrue(view.WorldToScreen(area.Max).X <= 820 + Precision);
    }

    [TestMethod]
    public void FitOfNothingShowsTheOrigin()
    {
        var view = Viewport.Fit(BoundingBox.Empty, 800, 600);
        Assert.AreEqual(Point.Origin, view.Center);
        Assert.AreEqual(1, view.Scale);
    }

    [TestMethod]
    public void PixelsToMillimetres() => Assert.AreEqual(16, View().ToWorld(8), Precision);
}
