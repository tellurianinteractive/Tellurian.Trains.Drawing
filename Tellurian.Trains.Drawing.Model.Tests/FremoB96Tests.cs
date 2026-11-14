using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>
/// The built-in FREMO H0-B96 against its dimensioned drawing. The drawing measures from the plate's
/// left edge and bottom: the track (rail top) is at 195 mm on the high profile and 104 mm on the low one.
/// </summary>
[TestClass]
public sealed class FremoB96Tests
{
    private const double CentreX = 250;
    private const double HighTrack = 195;
    private const double LowTrack = 104;

    private static EndProfile High => BuiltInCatalogue.Instance.FindEndProfile("FREMO-H0-B96", "H0")!;
    private static EndProfile Low => BuiltInCatalogue.Instance.FindEndProfile("FREMO-H0-B96f", "H0")!;

    /// <summary>A point given as on the drawing, in end profile coordinates.</summary>
    private static Point Drawing(double x, double y, double track = HighTrack) => new(x - CentreX, y - track);

    [TestMethod]
    public void HasOneCentredTrack()
    {
        var track = High.Tracks.Single();
        Assert.AreEqual(0, track.Offset, Precision);
        Assert.AreEqual(16.5, track.Gauge, Precision);
    }

    [TestMethod]
    public void PlateSizes()
    {
        var high = EndProfileAnalysis.Analyze(High);
        var low = EndProfileAnalysis.Analyze(Low);
        Assert.AreEqual(500, high.PlateWidth, Precision);
        Assert.AreEqual(242, high.PlateHeight, DrawingRounding);
        Assert.AreEqual(151, low.PlateHeight, DrawingRounding);
        Assert.AreEqual(-HighTrack, high.PlateBounds.Min.Y, Precision);
        Assert.AreEqual(-LowTrack, low.PlateBounds.Min.Y, Precision);
    }

    // The same corners, read with the low/high values of the drawing's left scale.
    [TestMethod]
    [DataRow(0, 46, 137)]
    [DataRow(189, 74, 165)]
    [DataRow(215, 100, 191)]
    [DataRow(226, 100, 191)]
    [DataRow(230, 104, 195)]
    [DataRow(270, 104, 195)]
    [DataRow(274, 100, 191)]
    [DataRow(285, 100, 191)]
    [DataRow(295, 90, 181)]
    [DataRow(305, 90, 181)]
    [DataRow(310, 95, 186)]
    [DataRow(500, 151, 242)]
    public void GroundLinePassesThroughTheDimensionedPoints(double x, double low, double high)
    {
        var groundLine = High.GroundLine!.Tessellate(Tolerance.Chord);
        Assert.AreEqual(Drawing(x, low, LowTrack), Drawing(x, high), $"The low and high values must give the same point.");
        Assert.IsLessThanOrEqualTo(DrawingRounding, Measure.PointToPolyline(Drawing(x, high), groundLine));
        Assert.IsLessThanOrEqualTo(DrawingRounding, Measure.PointToPolyline(Drawing(x, low, LowTrack), Low.GroundLine!.Tessellate(Tolerance.Chord)));
    }

    [TestMethod]
    public void MountainSideIsAnArcOfRadius547()
    {
        var arcs = High.GroundLine!.ToPath().Segments.OfType<ArcSegment>().ToList();
        Assert.HasCount(2, arcs);
        Assert.AreEqual(10, arcs[0].Radius, Precision);
        Assert.AreEqual(547, arcs[1].Radius, Precision);
        Assert.IsTrue(arcs[1].Center.IsCloseTo(Drawing(577, -300), Precision));
        Assert.IsTrue(arcs[0].EndHeading.IsCloseTo(arcs[1].StartHeading, Precision), "The R10 fillet is tangent to the R547 arc.");
    }

    [TestMethod]
    public void ScrewHoles()
    {
        Point[] high = [Drawing(70, 50), Drawing(430, 50), Drawing(70, 116), Drawing(430, 116), Drawing(250, 130)];
        CollectionAssert.AreEquivalent(high, High.ScrewHoles.Select(h => h.Center).ToArray());
        Assert.IsTrue(High.ScrewHoles.All(h => h.Diameter == 12));
    }

    // The low profile has the upper three holes of the high one, at the same place relative to rail top.
    [TestMethod]
    public void LowProfileHolesAreThoseOfTheHighProfileAboveItsBottom()
    {
        var above = High.ScrewHoles.Where(h => h.Center.Y > -LowTrack).Select(h => h.Center).ToArray();
        CollectionAssert.AreEquivalent(above, Low.ScrewHoles.Select(h => h.Center).ToArray());
        Assert.HasCount(3, above);
    }

    // The three holes they have in common are enough to bolt them together (spec §3.2).
    [TestMethod]
    [DataRow(ProfileOrientation.Normal, ProfileOrientation.Mirrored)]
    [DataRow(ProfileOrientation.Mirrored, ProfileOrientation.Normal)]
    public void HighAndLowProfilesAreBolted(ProfileOrientation high, ProfileOrientation low)
    {
        Assert.AreEqual(JointFit.Fits, JointFitEvaluator.Evaluate(High, high, Low, low).Fit);
        Assert.AreEqual(JointFit.Fits, JointFitEvaluator.Evaluate(Low, low, High, high).Fit);
    }

    [TestMethod]
    public void HighAndLowHaveTheSameGroundLine() =>
        Assert.IsTrue(ShapeComparison.MaximumDeviation(High.GroundLine!.Tessellate(), Low.GroundLine!.Tessellate(), Tolerance.Chord).IsWithin(Precision));
}
