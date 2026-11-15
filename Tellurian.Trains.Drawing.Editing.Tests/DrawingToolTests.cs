using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Editing.Snapping;
using Tellurian.Trains.Drawing.Editing.Tools;
using Tellurian.Trains.Drawing.Model;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class DrawingToolTests
{
    private const double Reach = 10;

    private sealed record Sketch(IReadOnlyList<PathDrawing> Paths, IReadOnlyList<Dimension> Dimensions);

    private static EditorDocument<Sketch> NewDocument() => new(new([], []));

    private static EditCommand<Sketch> AddPath(PathDrawing path) => new("Add path", s => s with { Paths = [.. s.Paths, path] });

    private static EditCommand<Sketch> AddDimension(Dimension dimension) => new("Add dimension", s => s with { Dimensions = [.. s.Dimensions, dimension] });

    private static ToolPointer At(double x, double y, bool constrained = false, bool noSnap = false) =>
        new(new(x, y), Reach) { IsConstrained = constrained, IsSnapSuppressed = noSnap };

    private static void Click(ITool tool, ToolPointer pointer)
    {
        tool.PointerMove(pointer);
        tool.PointerDown(pointer);
        tool.PointerUp(pointer);
    }

    [TestMethod]
    public void ClickingTheStartClosesTheOutline()
    {
        var document = NewDocument();
        var tool = new PathTool<Sketch>(document, new SnapEngine(), AddPath) { RequiresClosed = true };
        Click(tool, At(0, 0));
        Click(tool, At(1000, 0));
        Click(tool, At(1000, 500));
        Click(tool, At(0, 500));
        tool.PointerMove(At(4, 3));
        Assert.IsTrue(tool.WouldClose);
        Click(tool, At(4, 3));

        var path = document.Current.Paths.Single();
        Assert.IsTrue(path.IsClosed);
        Assert.HasCount(4, path.Steps);
        Assert.AreEqual(3000, path.ToPath().Length, Precision);
        Assert.IsFalse(tool.IsDrawing);
        Assert.AreEqual("Add path", document.UndoDescription);
    }

    [TestMethod]
    public void GridSnappedStartIsClosedWhereItIsDrawn()
    {
        var document = NewDocument();
        var snap = new SnapEngine { GridSpacing = 50 };
        var tool = new PathTool<Sketch>(document, snap, AddPath);
        static ToolPointer Far(double x, double y) => new(new(x, y), 40);
        // The start snaps from (24, 24) to the grid point (0, 0), where it is drawn…
        Click(tool, Far(24, 24));
        Click(tool, Far(1000, 0));
        Click(tool, Far(500, 800));
        // …so a click next to the drawn start closes the path.
        tool.PointerMove(Far(5, -5));
        Assert.IsTrue(tool.WouldClose);
        Click(tool, Far(5, -5));
        var path = document.Current.Paths.Single();
        Assert.IsTrue(path.IsClosed);
        Assert.AreEqual(Point.Origin, path.Start);
    }

    [TestMethod]
    public void EnterFinishesAnOpenPathUnlessItMustBeClosed()
    {
        var document = NewDocument();
        var open = new PathTool<Sketch>(document, new SnapEngine(), AddPath) { AllowsClosed = false };
        Click(open, At(0, 0));
        Click(open, At(500, 0));
        Assert.IsTrue(open.KeyDown("Enter"));
        Assert.IsFalse(document.Current.Paths.Single().IsClosed);

        var outline = new PathTool<Sketch>(document, new SnapEngine(), AddPath) { RequiresClosed = true };
        Click(outline, At(0, 0));
        Click(outline, At(500, 0));
        Assert.IsFalse(outline.KeyDown("Enter"));
        Assert.HasCount(1, document.Current.Paths);
    }

    [TestMethod]
    public void BackspaceRemovesTheLastStepAndEscapeAbandons()
    {
        var document = NewDocument();
        var tool = new PathTool<Sketch>(document, new SnapEngine(), AddPath);
        Click(tool, At(0, 0));
        Click(tool, At(500, 0));
        Click(tool, At(500, 500));
        tool.KeyDown("Backspace");
        Assert.HasCount(1, tool.Drawn!.Steps);
        Assert.IsTrue(tool.KeyDown("Escape"));
        Assert.IsFalse(tool.IsDrawing);
        Assert.IsEmpty(document.Current.Paths);
    }

    [TestMethod]
    public void TangentArcContinuesTheDirection()
    {
        var document = NewDocument();
        var tool = new PathTool<Sketch>(document, new SnapEngine(), AddPath) { AllowsClosed = false };
        Click(tool, At(0, 0));
        Click(tool, At(1000, 0));
        tool.Mode = PathStepMode.TangentArc;
        // A quarter circle of radius 500 turning left ends at (1500, 500).
        Click(tool, At(1500, 500));
        tool.KeyDown("Enter");

        var path = document.Current.Paths.Single().ToPath();
        Assert.IsTrue(path.IsTangentContinuous(1e-6));
        var arc = (ArcSegment)path.Segments[1];
        Assert.AreEqual(500, arc.Radius, Precision);
        Assert.AreEqual(90, arc.SweepDegrees, Precision);
        Assert.IsTrue(path.End.IsCloseTo(new(1500, 500), Precision));
    }

    [TestMethod]
    [DataRow(1500.0, -500.0, -90.0, 500.0)]
    [DataRow(1000.0, 2000.0, 180.0, 1000.0)]
    public void TangentArcs(double x, double y, double sweep, double radius)
    {
        var arc = PathTool<Sketch>.TangentArc(new Point(1000, 0), Angle.Zero, new(x, y));
        Assert.IsNotNull(arc);
        Assert.AreEqual(sweep, arc.Sweep, Precision);
        Assert.AreEqual(radius, arc.Center.DistanceTo(new(1000, 0)), Precision);
    }

    [TestMethod]
    public void NoTangentArcStraightAhead() =>
        Assert.IsNull(PathTool<Sketch>.TangentArc(Point.Origin, Angle.Zero, new(100, 0)));

    [TestMethod]
    public void FirstStepOfATangentArcPathIsALine()
    {
        var document = NewDocument();
        var tool = new PathTool<Sketch>(document, new SnapEngine(), AddPath) { Mode = PathStepMode.TangentArc };
        Click(tool, At(0, 0));
        Click(tool, At(100, 100));
        Assert.IsInstanceOfType<LineStep>(tool.Drawn!.Steps.Single());
    }

    [TestMethod]
    public void PointsSnapAndShiftConstrainsTheDirection()
    {
        var snap = new SnapEngine();
        snap.Add(new PointTarget(new(500, 500)));
        var document = NewDocument();
        var tool = new PathTool<Sketch>(document, snap, AddPath);
        Click(tool, At(0, 0));
        Click(tool, At(504, 497));
        Assert.AreEqual(new Point(500, 500), tool.Drawn!.ToPath().End);

        tool.PointerMove(At(1000, 530, constrained: true));
        var preview = tool.Preview!.ToPath().End;
        Assert.AreEqual(500, preview.Y, 1e-6, "Shift keeps the direction horizontal (a multiple of 15°).");
    }

    [TestMethod]
    public void MeasureGivesDistanceAndDirection()
    {
        var snap = new SnapEngine();
        snap.Add(new PointTarget(new(300, 400)));
        var tool = new MeasureTool(snap);
        Click(tool, At(0, 0));
        tool.PointerMove(At(150, 150));
        Assert.IsTrue(tool.IsMeasuring);
        Click(tool, At(303, 398));

        var measurement = tool.Measurement!;
        Assert.AreEqual(500, measurement.Distance, Precision);
        Assert.AreEqual((300.0, 400.0), (measurement.DeltaX, measurement.DeltaY));
        Assert.AreEqual(53.130102354, measurement.Direction.Degrees, 1e-6);
        Assert.IsFalse(tool.IsMeasuring);
        Assert.IsTrue(tool.KeyDown("Escape"));
        Assert.IsNull(tool.Measurement);
    }

    [TestMethod]
    public void DimensionKindFollowsThePlacement()
    {
        Point a = new(0, 0), b = new(1000, 300);
        Assert.AreEqual(DimensionKind.Horizontal, Dimension.Place(a, b, new(500, 600)).Kind);
        Assert.AreEqual(DimensionKind.Vertical, Dimension.Place(a, b, new(1400, 150)).Kind);
        Assert.AreEqual(DimensionKind.Aligned, Dimension.Place(a, b, new(400, 200)).Kind);

        var horizontal = Dimension.Place(a, b, new(500, 600));
        Assert.AreEqual(1000, horizontal.Length, Precision);
        Assert.AreEqual(300, horizontal.Offset, Precision);
        Assert.AreEqual((new Point(0, 600), new Point(1000, 600)), horizontal.Line());
    }

    [TestMethod]
    public void AlignedDimensionLineIsOffsetToTheLeft()
    {
        var dimension = Dimension.Place(new(0, 0), new(1000, 0), new(500, 80), DimensionKind.Aligned);
        Assert.AreEqual(80, dimension.Offset, Precision);
        var (start, end) = dimension.Line();
        Assert.IsTrue(start.IsCloseTo(new(0, 80), Precision) && end.IsCloseTo(new(1000, 80), Precision));
        Assert.AreEqual(1000, dimension.Length, Precision);
    }

    [TestMethod]
    public void DimensionToolAddsADimensionWithThreeClicks()
    {
        var document = NewDocument();
        var tool = new DimensionTool<Sketch>(document, new SnapEngine(), AddDimension);
        Click(tool, At(0, 0));
        Click(tool, At(1000, 0));
        tool.PointerMove(At(500, -150));
        Assert.AreEqual(DimensionKind.Horizontal, tool.Preview?.Kind);
        Click(tool, At(500, -150));

        var dimension = document.Current.Dimensions.Single();
        Assert.AreEqual(1000, dimension.Length, Precision);
        Assert.IsNull(tool.Preview);
        document.Undo();
        Assert.IsEmpty(document.Current.Dimensions);
    }
}
