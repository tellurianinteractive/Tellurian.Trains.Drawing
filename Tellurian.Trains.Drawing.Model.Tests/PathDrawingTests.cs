
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
        Assert.IsTrue(back.End.IsCloseTo(path.End, Precision));
        Assert.IsTrue(back.EndPose.Heading.IsCloseTo(path.EndPose.Heading, Precision));
        Assert.AreEqual(path.Length, back.Length, Precision);
        Assert.IsTrue(back.IsTangentContinuous(Precision));
    }

    [TestMethod]
    public void ClosedPolylineAddsClosingStep()
    {
        var drawing = PathDrawing.Polyline([new(0, 0), new(10, 0), new(10, 10)], isClosed: true);
        Assert.HasCount(3, drawing.Steps);
        var path = drawing.ToPath();
        Assert.IsTrue(path.IsClosed);
        Assert.AreEqual(10 + 10 + Math.Sqrt(200), path.Length, Precision);
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
            Steps = [new LineStep(new(10, 0)), new LineStep(new(10, 10)), new LineStep(new(Tolerance.Length / 1000, 0))],
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
