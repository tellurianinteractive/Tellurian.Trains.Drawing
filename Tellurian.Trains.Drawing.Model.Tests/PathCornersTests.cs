namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class PathCornersTests
{
    private static PathDrawing Square(double side = 1000) => PathDrawing.Rectangle(side, side, Transform.Identity);

    [TestMethod]
    public void ClosedPathHasOneCornerPerStep()
    {
        var corners = PathCorners.From(Square());
        Assert.HasCount(4, corners.Corners);
        Assert.HasCount(4, corners.Sweeps);
        Assert.IsTrue(corners.Corners[2].IsCloseTo(new(1000, 1000), Precision));
        Assert.IsTrue(corners.Sweeps.All(s => s == 0));
    }

    [TestMethod]
    public void OpenPathHasOneMoreCornerThanSteps()
    {
        var corners = PathCorners.From(PathDrawing.Polyline([new(0, 0), new(100, 0), new(100, 50)]));
        Assert.HasCount(3, corners.Corners);
        Assert.HasCount(2, corners.Sweeps);
        Assert.IsFalse(corners.IsClosed);
    }

    [TestMethod]
    public void RoundTripKeepsArcs()
    {
        var drawing = new PathDrawing { Start = new(1000, 0), Steps = [new ArcStep(Point.Origin, 90), new LineStep(new(0, 0)), new LineStep(new(1000, 0))], IsClosed = true };
        var path = PathCorners.From(drawing).ToDrawing().ToPath();
        var original = drawing.ToPath();
        Assert.AreEqual(original.Length, path.Length, Precision);
        Assert.IsInstanceOfType<ArcSegment>(path.Segments[0]);
        Assert.IsTrue(((ArcSegment)path.Segments[0]).Center.IsCloseTo(Point.Origin, Precision));
    }

    [TestMethod]
    public void ArcCentreOfAQuarterTurn()
    {
        Assert.IsTrue(PathCorners.ArcCenter(new(1000, 0), new(0, 1000), 90)!.Value.IsCloseTo(Point.Origin, Precision));
        Assert.IsTrue(PathCorners.ArcCenter(new(0, 1000), new(1000, 0), -90)!.Value.IsCloseTo(Point.Origin, Precision));
        Assert.IsTrue(PathCorners.ArcCenter(new(-500, 0), new(500, 0), 180)!.Value.IsCloseTo(Point.Origin, Precision));
        Assert.IsNull(PathCorners.ArcCenter(new(0, 0), new(1000, 0), 0));
    }

    [TestMethod]
    public void MovedCornerKeepsTheArcSweep()
    {
        var corners = PathCorners.From(Square()).WithSweep(0, 30).WithCorner(1, new(1200, 0));
        var arc = (ArcSegment)corners.ToDrawing().ToPath().Segments[0];
        Assert.AreEqual(30, arc.SweepDegrees, Precision);
        Assert.IsTrue(arc.End.IsCloseTo(new(1200, 0), Precision));
        Assert.IsTrue(arc.Start.IsCloseTo(Point.Origin, Precision));
    }

    [TestMethod]
    public void InsertedCornerSplitsAStepInTheMiddle()
    {
        var corners = PathCorners.From(Square()).WithCornerInserted(0);
        Assert.HasCount(5, corners.Corners);
        Assert.IsTrue(corners.Corners[1].IsCloseTo(new(500, 0), Precision));
    }

    [TestMethod]
    public void InsertedCornerSplitsAnArcInTwoHalves()
    {
        var corners = PathCorners.From(Square()).WithSweep(0, 60).WithCornerInserted(0);
        Assert.AreEqual(30, corners.Sweeps[0], Precision);
        Assert.AreEqual(30, corners.Sweeps[1], Precision);
        Assert.AreEqual(PathCorners.From(Square()).WithSweep(0, 60).LengthOf(0), corners.LengthOf(0) + corners.LengthOf(1), Precision);
    }

    [TestMethod]
    public void RemovedCornerJoinsItsStepsInAStraightStep()
    {
        var corners = PathCorners.From(Square()).WithCornerInserted(1).WithSweep(1, 45).WithoutCorner(2)!;
        Assert.HasCount(4, corners.Corners);
        Assert.AreEqual(0, corners.Sweeps[1]);
        Assert.IsTrue(corners.Corners[2].IsCloseTo(new(1000, 1000), Precision));
    }

    [TestMethod]
    public void RemovingTheFirstCornerOfAClosedPathKeepsTheOtherSteps()
    {
        var corners = PathCorners.From(Square()).WithSweep(1, 20).WithoutCorner(0)!;
        Assert.HasCount(3, corners.Corners);
        Assert.IsTrue(corners.Corners[0].IsCloseTo(new(1000, 0), Precision));
        Assert.AreEqual(20, corners.Sweeps[0], "The step from (1000, 0) to (1000, 1000) is still an arc.");
        Assert.AreEqual(0, corners.Sweeps[2], "The joined step back to the start is straight.");
    }

    [TestMethod]
    public void AClosedPathKeepsThreeCorners()
    {
        var triangle = PathCorners.From(PathDrawing.Polyline([new(0, 0), new(100, 0), new(0, 100)], isClosed: true));
        Assert.IsNull(triangle.WithoutCorner(0));
    }

    [TestMethod]
    public void NewLengthMovesTheEndCornerAlongTheStep()
    {
        var corners = PathCorners.From(Square()).WithLength(0, 1500)!;
        Assert.IsTrue(corners.Corners[1].IsCloseTo(new(1500, 0), Precision));
        Assert.AreEqual(1500, corners.LengthOf(0), Precision);
    }

    [TestMethod]
    public void LengthOfTheLastStepMovesTheStart()
    {
        var corners = PathCorners.From(Square()).WithLength(3, 500)!;
        Assert.IsTrue(corners.Corners[0].IsCloseTo(new(0, 500), Precision));
    }

    [TestMethod]
    public void ArcLengthCantBeTyped()
    {
        Assert.IsNull(PathCorners.From(Square()).WithSweep(0, 30).WithLength(0, 1500));
        Assert.IsNull(PathCorners.From(Square()).WithLength(0, 0));
    }

    [TestMethod]
    public void RecognisesARotatedRectangle()
    {
        var corners = PathCorners.From(PathDrawing.Rectangle(new(500, 300), 400, 200, new(30)));
        Assert.IsTrue(corners.TryGetRectangle(out var center, out var width, out var depth, out var rotation));
        Assert.IsTrue(center.IsCloseTo(new(500, 300), Precision));
        Assert.AreEqual(400, width, Precision);
        Assert.AreEqual(200, depth, Precision);
        Assert.AreEqual(30, rotation.Degrees, Precision);
    }

    [TestMethod]
    public void AQuadrilateralIsNotARectangle()
    {
        var corners = PathCorners.From(PathDrawing.Polyline([new(0, 0), new(100, 0), new(120, 50), new(0, 50)], isClosed: true));
        Assert.IsFalse(corners.TryGetRectangle(out _, out _, out _, out _));
    }
}
