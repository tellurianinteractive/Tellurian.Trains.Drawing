using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>Arcs and fillets in a plate's upper edge, entered as a dimensioned drawing gives them.</summary>
[TestClass]
public sealed class PlateArcTests
{
    private const double CentreX = 250;
    private const double LowTrack = 104;

    /// <summary>A point as on the B96 drawing, measured from the low plate's lower left corner.</summary>
    private static Point Low(double x, double y) => new(x - CentreX, y - LowTrack);

    /// <summary>The low B96 plate, entered exactly as the drawing gives it.</summary>
    private static PlateShape LowB96FromTheDrawing() => new(
        Low(0, 46),
        [
            PlateSegment.Line(Low(189, 74)), PlateSegment.Line(Low(215, 100)), PlateSegment.Line(Low(226, 100)), PlateSegment.Line(Low(230, 104)),
            PlateSegment.Line(Low(270, 104)), PlateSegment.Line(Low(274, 100)), PlateSegment.Line(Low(285, 100)), PlateSegment.Line(Low(295, 90)),
            PlateSegment.Line(Low(305, 90)), PlateSegment.Line(Low(310, 95)), PlateSegment.Line(Low(323, 95)),
            PlateSegment.Fillet(10),
            PlateSegment.Arc(Low(577, -391), Low(500, 151)),
        ],
        Low(0, 0).Y);

    [TestMethod]
    public void FilletIsTangentToTheLineBeforeAndTheArcAfter()
    {
        var edge = LowB96FromTheDrawing().Edge.Segments;
        var fillet = (ArcSegment)edge[^2];
        var arc = (ArcSegment)edge[^1];
        Assert.AreEqual(10, fillet.Radius, Precision);
        Assert.IsTrue(fillet.StartHeading.IsCloseTo(edge[^3].EndHeading, 1e-9), "The fillet continues the line.");
        Assert.IsTrue(fillet.EndHeading.IsCloseTo(arc.StartHeading, ArcAngleRounding), "The arc continues the fillet, up to the drawing's rounding.");
        Assert.AreEqual(547, arc.Radius, DrawingRounding, "The drawing's rounded values cost a fraction of a millimetre.");
        Assert.IsTrue(arc.End.IsCloseTo(Low(500, 151), Precision), "The plate's corner is exactly where the drawing says.");
    }

    [TestMethod]
    public void MatchesTheBuiltInLowB96()
    {
        var builtIn = BuiltInCatalogue.Instance.FindEndProfile("FREMO-H0-B96f", "H0")!.GroundLine!.Tessellate(Tolerance.Chord);
        var drawn = LowB96FromTheDrawing().Edge.Tessellate(Tolerance.Chord);
        foreach (var point in builtIn.Points)
            Assert.IsLessThanOrEqualTo(DrawingRounding, Measure.PointToPolyline(point, drawn), $"{point}");
    }

    [TestMethod]
    public void BuiltInOutlineIsReadBackWithItsArcs()
    {
        var b96 = BuiltInCatalogue.Instance.FindEndProfile("FREMO-H0-B96", "H0")!;
        var plate = PlateShape.From(b96)!;
        var arcs = plate.Segments.Where(s => s.Kind == PlateSegmentKind.Arc).ToList();
        CollectionAssert.AreEqual(new[] { 10.0, 547.0 }, arcs.Select(a => Math.Round(a.EffectiveRadius, 6)).ToArray());
        Assert.IsTrue(plate.Outline().ToPath().Length - b96.Outline!.ToPath().Length is > -Precision and < Precision, "The same outline comes back.");
    }

    [TestMethod]
    public void FilletIntoALineEndsWhereTheLineIsTangent()
    {
        // A horizontal line, then a 20 mm fillet turning up into a line to (100, 100).
        var plate = new PlateShape(new(-100, 0), [PlateSegment.Line(new(0, 0)), PlateSegment.Fillet(20), PlateSegment.Line(new(100, 100))], -100);
        var fillet = (ArcSegment)plate.Edge.Segments[1];
        var line = plate.Edge.Segments[2];
        Assert.IsTrue(fillet.EndHeading.IsCloseTo(line.StartHeading, 1e-9));
        Assert.AreEqual(new Point(100, 100), plate.Edge.End);
        Assert.AreEqual(plate.UpperEdge[2], fillet.End, "The fillet's worked-out end is a vertex.");
    }

    [TestMethod]
    public void FilletCentreIsOnTheChosenSide()
    {
        PlateShape WithSide(FilletSide? side) => LowB96FromTheDrawing().WithSegment(11, PlateSegment.Fillet(10, side))!;
        var above = (ArcSegment)WithSide(FilletSide.Above).Edge.Segments[^2];
        var below = (ArcSegment)WithSide(FilletSide.Below).Edge.Segments[^2];
        var automatic = (ArcSegment)WithSide(null).Edge.Segments[^2];
        Assert.IsGreaterThan(above.Start.Y, above.Center.Y, "Above: the centre is above the edge.");
        Assert.IsLessThan(below.Start.Y, below.Center.Y, "Below: the centre is below the edge.");
        Assert.IsTrue(automatic.Center.IsCloseTo(above.Center), "On the B96 plate the best fit is above.");
    }

    [TestMethod]
    public void FilletSideIsKeptThroughAFile()
    {
        var plate = LowB96FromTheDrawing().WithSegment(11, PlateSegment.Fillet(10, FilletSide.Above))!;
        var profile = TestProfiles.FremoSingle() with { Plate = plate, Outline = plate.Outline(), GroundLine = plate.GroundLine() };
        var json = Files.DrawingFileSerializer.Serialize(profile);
        StringAssert.Contains(json, "\"side\": \"above\"");
        Assert.AreEqual(FilletSide.Above, Files.DrawingFileSerializer.DeserializeEndProfile(json).Plate!.Segments[11].Side);
        SchemaValidator.AssertValid(Files.DrawingFileKind.EndProfile, json);
    }

    [TestMethod]
    public void FilletWithoutANeighbourIsLeftOut()
    {
        var plate = new PlateShape(new(-100, 0), [PlateSegment.Line(new(0, 0)), PlateSegment.Fillet(20)], -100);
        Assert.HasCount(1, plate.Edge.Segments);
        Assert.AreEqual(plate.UpperEdge[1], plate.UpperEdge[2]);
    }

    [TestMethod]
    public void ArcEndsCanBeMovedAndFilletsCant()
    {
        var plate = LowB96FromTheDrawing();
        var lastVertex = plate.UpperEdge.Count - 1;
        var moved = plate.WithVertex(lastVertex, Low(500, 160))!;
        Assert.AreEqual(Low(577, -391), moved.Segments[^1].Center, "The arc keeps its centre.");
        Assert.IsNull(plate.WithVertex(lastVertex - 1, Low(330, 100)), "A fillet's end is worked out.");
    }

    [TestMethod]
    public void WholeShapeMovesWithItsCentres()
    {
        var moved = LowB96FromTheDrawing().Moved(new(10, -5))!;
        Assert.AreEqual(Low(587, -396), moved.Segments[^1].Center);
        Assert.AreEqual(Low(0, 0).Y - 5, moved.Bottom, Precision);
    }

    [TestMethod]
    public void ReadBackArcCanBeTurnedIntoAFillet()
    {
        var plate = PlateShape.From(BuiltInCatalogue.Instance.FindEndProfile("FREMO-H0-B96f", "H0")!)!;
        var index = plate.Segments.ToList().FindIndex(s => s.Kind == PlateSegmentKind.Arc && Math.Abs(s.EffectiveRadius - 10) < 0.01);
        var filleted = plate.WithSegment(index, PlateSegment.Fillet(10));
        Assert.IsNotNull(filleted);
        Assert.AreEqual(PlateSegmentKind.Fillet, filleted.Segments[index].Kind);
    }

    [TestMethod]
    public void StoredPlateKeepsItsFilletThroughAFile()
    {
        var plate = LowB96FromTheDrawing();
        var profile = TestProfiles.FremoSingle() with { Plate = plate, Outline = plate.Outline(), GroundLine = plate.GroundLine() };

        var read = Files.DrawingFileSerializer.DeserializeEndProfile(Files.DrawingFileSerializer.Serialize(profile));

        var segments = PlateShape.From(read)!.Segments;
        Assert.AreEqual(PlateSegmentKind.Fillet, segments[^2].Kind, "The fillet is stored as a fillet, not as the arc it resolves to.");
        Assert.AreEqual(10, segments[^2].Radius);
        Assert.AreEqual(Low(577, -391), segments[^1].Center);
        Assert.AreEqual(read.Outline!.ToPath().Length, profile.Outline.ToPath().Length, Precision, "The outline is still written for other readers.");
    }

    [TestMethod]
    public void MirroredProfileDropsItsPlateAndKeepsTheMirroredOutline()
    {
        var plate = LowB96FromTheDrawing();
        var profile = TestProfiles.FremoSingle() with { Plate = plate, Outline = plate.Outline(), GroundLine = plate.GroundLine() };
        var mirrored = profile.MirroredX();
        Assert.IsNull(mirrored.Plate);
        Assert.AreEqual(-profile.Outline!.ToPath().Bounds.Min.X, mirrored.Outline!.ToPath().Bounds.Max.X, Precision);
    }
}
