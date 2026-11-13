using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class EndProfileAnalysisTests
{
    private const double Precision = 1e-9;

    [TestMethod]
    public void TemplateProfileProperties()
    {
        var properties = EndProfileAnalysis.Analyze(TestProfiles.Double(45));
        Assert.AreEqual(500, properties.PlateWidth, Precision);
        Assert.AreEqual(100, properties.PlateHeight, Precision);
        Assert.AreEqual(-40, properties.PlateBounds.Max.Y, Precision);
        Assert.AreEqual(2, properties.TrackCount);
        CollectionAssert.AreEqual(new[] { -22.5, 22.5 }, properties.TrackOffsets.ToArray());
        Assert.HasCount(1, properties.TrackCentreDistances);
        Assert.AreEqual(45, properties.TrackCentreDistances[0], Precision);
        Assert.HasCount(2, properties.ScrewHolePositions);
        Assert.IsTrue(properties.IsSymmetric);
        Assert.AreEqual(FitDifferences.None, properties.AsymmetricElements);
    }

    [TestMethod]
    public void AsymmetricGroundLineIsDetected()
    {
        var properties = EndProfileAnalysis.Analyze(TestProfiles.Asymmetric());
        Assert.IsFalse(properties.IsSymmetric);
        Assert.AreEqual(FitDifferences.GroundLine, properties.AsymmetricElements);
    }

    [TestMethod]
    public void AsymmetricScrewHolesAreDetected() =>
        Assert.AreEqual(FitDifferences.ScrewHoles, EndProfileAnalysis.AsymmetricElements(TestProfiles.AsymmetricHoles()));

    [TestMethod]
    public void OffCentreTrackIsAsymmetric()
    {
        var profile = TestProfiles.FremoSingle() with { Tracks = [new() { Offset = 20, Gauge = TestProfiles.H0Gauge }] };
        Assert.AreEqual(FitDifferences.TrackPositions, EndProfileAnalysis.AsymmetricElements(profile));
    }

    [TestMethod]
    public void AsymmetricOutlineAndDowelsDoNotMakeTheProfileAsymmetric()
    {
        var profile = TestProfiles.FremoSingle() with
        {
            Outline = PathDrawing.Polyline([new(-250, -140), new(270, -140), new(270, -40), new(-250, -40)], isClosed: true),
            Dowels = [new Circle(new Point(100, -90), 6)],
        };
        var properties = EndProfileAnalysis.Analyze(profile);
        Assert.AreEqual(FitDifferences.Outline | FitDifferences.Dowels, properties.AsymmetricElements);
        Assert.IsTrue(properties.IsSymmetric);
    }

    [TestMethod]
    public void ProfileWithoutOutlineHasEmptyPlate()
    {
        var properties = EndProfileAnalysis.Analyze(TestProfiles.FremoSingle() with { Outline = null, Tracks = [] });
        Assert.IsTrue(properties.PlateBounds.IsEmpty);
        Assert.AreEqual(0, properties.PlateWidth);
        Assert.AreEqual(0, properties.TrackCount);
        Assert.IsEmpty(properties.TrackCentreDistances);
    }

    [TestMethod]
    public void MirroringTwiceRestoresTheProfile()
    {
        var profile = TestProfiles.Asymmetric();
        var twice = profile.MirroredX().MirroredX();
        Assert.AreEqual(JointFit.Fits, JointFitEvaluator.Evaluate(profile, ProfileOrientation.Mirrored, twice, ProfileOrientation.Normal).Fit);
        Assert.IsTrue(ShapeComparison.MaximumDeviation(profile.GroundLine!.Tessellate(), twice.GroundLine!.Tessellate()).IsWithin(1e-9));
        Assert.AreSame(profile, profile.Oriented(ProfileOrientation.Normal));
    }

    [TestMethod]
    [DataRow(1, 45, new[] { 0.0 })]
    [DataRow(2, 45, new[] { -22.5, 22.5 })]
    [DataRow(3, 50, new[] { -50.0, 0.0, 50.0 })]
    public void CentredOffsets(int count, double centreDistance, double[] expected) =>
        CollectionAssert.AreEqual(expected, EndProfileTemplate.CentredOffsets(count, centreDistance).ToArray());

    [TestMethod]
    public void NoTracksGiveNoOffsets() =>
        Assert.IsEmpty(EndProfileTemplate.CentredOffsets(0, 45));

    [TestMethod]
    public void InvalidTemplatesAreRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => EndProfileTemplate.Create(new() { Designation = "X", Scale = "H0", PlateWidth = 0, PlateTop = 0, PlateBottom = -10, Gauge = 16.5 }));
        Assert.ThrowsExactly<ArgumentException>(() => EndProfileTemplate.Create(new() { Designation = "X", Scale = "H0", PlateWidth = 100, PlateTop = -50, PlateBottom = -10, Gauge = 16.5 }));
    }
}
