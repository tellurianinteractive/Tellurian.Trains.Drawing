using Tellurian.Trains.Drawing.Editing.Snapping;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class SnapEngineTests
{
    private const double Reach = 10;

    private static SnapEngine Engine()
    {
        var engine = new SnapEngine(cellSize: 100) { GridSpacing = 50 };
        engine.Add(new EdgeTarget(new(0, 0), new(1000, 0), "wall"));
        engine.Add(new PointTarget(new(500, 0), "corner"));
        engine.Add(new ConnectionTarget(new Pose(new(800, 3), new Angle(90)), "exit"));
        return engine;
    }

    [TestMethod]
    public void NothingWithinReachLeavesThePoint()
    {
        var result = Engine().Snap(new(123, 321), Reach);
        Assert.AreEqual(SnapKind.None, result.Kind);
        Assert.AreEqual(new Point(123, 321), result.Point);
    }

    [TestMethod]
    public void EdgeGivesTheClosestPointOnIt()
    {
        var result = Engine().Snap(new(260, 7), Reach);
        Assert.AreEqual(SnapKind.Edge, result.Kind);
        Assert.IsTrue(result.Point.IsCloseTo(new(260, 0), Precision));
        Assert.AreEqual("wall", result.Target?.Source);
    }

    [TestMethod]
    public void PointWinsOverEdge()
    {
        var result = Engine().Snap(new(506, 4), Reach);
        Assert.AreEqual(SnapKind.Point, result.Kind);
        Assert.AreEqual(new Point(500, 0), result.Point);
    }

    [TestMethod]
    public void ConnectionWinsAndGivesItsHeading()
    {
        var result = Engine().Snap(new(803, 1), Reach);
        Assert.AreEqual(SnapKind.Connection, result.Kind);
        Assert.AreEqual(new Angle(90), result.Heading);
    }

    [TestMethod]
    public void GridIsUsedWhenNothingElseIsNear()
    {
        var result = Engine().Snap(new(147, 303), Reach);
        Assert.AreEqual(SnapKind.Grid, result.Kind);
        Assert.AreEqual(new Point(150, 300), result.Point);
    }

    [TestMethod]
    public void AFineGridCatchesEveryPointWhenItAlwaysSnaps()
    {
        var engine = new SnapEngine(cellSize: 100) { GridSpacing = 100 };
        Assert.AreEqual(SnapKind.None, engine.Snap(new(140, 360), Reach).Kind, "Too far from a grid point.");

        engine.SnapsAlwaysToGrid = true;
        var result = engine.Snap(new(140, 360), Reach);
        Assert.AreEqual(SnapKind.Grid, result.Kind);
        Assert.AreEqual(new Point(100, 400), result.Point);
    }

    [TestMethod]
    public void TargetsWithinReachWinOverAGridThatAlwaysSnaps()
    {
        var engine = Engine();
        engine.SnapsAlwaysToGrid = true;
        Assert.AreEqual(SnapKind.Edge, engine.Snap(new(260, 7), Reach).Kind);
    }

    [TestMethod]
    public void DisabledKindsAndExcludedTargetsAreIgnored()
    {
        var engine = Engine();
        engine.EnabledKinds.Remove(SnapKind.Point);
        Assert.AreEqual(SnapKind.Edge, engine.Snap(new(506, 4), Reach).Kind);

        var result = engine.Snap(new(803, 1), Reach, exclude: t => Equals(t.Source, "exit"));
        Assert.AreEqual(SnapKind.Edge, result.Kind);
    }

    [TestMethod]
    public void RemovedTargetsAreGone()
    {
        var engine = Engine();
        var corner = new PointTarget(new(500, 0), "corner");
        Assert.IsTrue(engine.Remove(corner));
        Assert.AreEqual(SnapKind.Edge, engine.Snap(new(506, 4), Reach).Kind);
    }
}
