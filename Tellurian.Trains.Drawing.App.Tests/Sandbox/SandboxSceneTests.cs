using Tellurian.Trains.Drawing.App.Sandbox;
using Tellurian.Trains.Drawing.Editing.Snapping;
using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.App.Tests.Sandbox;

[TestClass]
public sealed class SandboxSceneTests
{
    private static readonly SandboxScene Scene = new();
    private static SandboxDocument Document => SandboxDocument.Sample;

    [TestMethod]
    public void InsideOfAClosedShapeAndNearAnOpenOneAreHits()
    {
        Assert.IsTrue(Scene.TryHitTest(Document, new(600, 250), 1, out var rectangle));
        Assert.AreEqual(Document.Shapes[0].Id, rectangle);
        Assert.IsTrue(Scene.TryHitTest(Document, new(500, 1205), 10, out var track));
        Assert.AreEqual(Document.Shapes[3].Id, track);
        Assert.IsFalse(Scene.TryHitTest(Document, new(500, 900), 10, out _));
    }

    [TestMethod]
    public void MoveShiftsOnlyTheGivenShapes()
    {
        var id = Document.Shapes[0].Id;
        var moved = Scene.Move([id], new(100, -50)).Apply(Document);
        Assert.AreEqual(new Point(100, -50), moved.Find(id)!.Path.ToPath().Bounds.Min);
        Assert.AreSame(Document.Shapes[1], moved.Shapes[1]);
    }

    [TestMethod]
    public void CornersAndEdgesAreSnapTargets()
    {
        var targets = Scene.SnapTargetsOf(Document, Document.Shapes[0].Id).ToList();
        Assert.HasCount(4, targets.OfType<PointTarget>());
        Assert.HasCount(4, targets.OfType<EdgeTarget>());
        Assert.IsTrue(targets.All(t => Equals(t.Source, Document.Shapes[0].Id)));
    }
}
