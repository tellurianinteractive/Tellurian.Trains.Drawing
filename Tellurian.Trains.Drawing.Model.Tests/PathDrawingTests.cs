using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class PathDrawingTests
{
    [TestMethod]
    public void RoundTripThroughGeometryKeepsLinesAndArcs()
    {
        var path = new PathBuilder(new Pose(new(10, 20), new Angle(30))).Straight(500).Curve(2000, -15).Curve(800, 40).Straight(100).Build();
        var drawing = PathDrawing.From(path);
        Assert.HasCount(4, drawing.Steps);
        Assert.IsInstanceOfType<ArcStep>(drawing.Steps[1]);

        var back = drawing.ToPath();
        Assert.IsTrue(back.End.IsCloseTo(path.End, 1e-9));
        Assert.IsTrue(back.EndPose.Heading.IsCloseTo(path.EndPose.Heading, 1e-9));
        Assert.AreEqual(path.Length, back.Length, 1e-9);
        Assert.IsTrue(back.IsTangentContinuous(1e-9));
    }

    [TestMethod]
    public void ClosedPolylineAddsClosingStep()
    {
        var drawing = PathDrawing.Polyline([new(0, 0), new(10, 0), new(10, 10)], isClosed: true);
        Assert.HasCount(3, drawing.Steps);
        var path = drawing.ToPath();
        Assert.IsTrue(path.IsClosed);
        Assert.AreEqual(10 + 10 + Math.Sqrt(200), path.Length, 1e-9);
    }

    [TestMethod]
    public void ClosedPolylineThatRepeatsStartAddsNoStep() =>
        Assert.HasCount(3, PathDrawing.Polyline([new(0, 0), new(10, 0), new(10, 10), new(0, 0)], isClosed: true).Steps);

    [TestMethod]
    public void ClosedPathSnapsTinyRoundingGapAtTheEnd()
    {
        var drawing = new PathDrawing
        {
            Start = new(0, 0),
            Steps = [new LineStep(new(10, 0)), new LineStep(new(10, 10)), new LineStep(new(1e-9, 0))],
            IsClosed = true,
        };
        Assert.IsTrue(drawing.ToPath().End.Equals(new Point(0, 0)));
    }

    [TestMethod]
    public void MirroredPathFlipsX()
    {
        var drawing = PathDrawing.Polyline([new(-250, -10), new(100, -40)]);
        var mirrored = drawing.MirroredX();
        Assert.AreEqual(new Point(250, -10), mirrored.Start);
        Assert.IsTrue(mirrored.ToPath().End.IsCloseTo(new(-100, -40)));
    }

    [TestMethod]
    public void PolylineNeedsTwoPoints() =>
        Assert.ThrowsExactly<ArgumentException>(() => PathDrawing.Polyline([new(0, 0)]));
}
