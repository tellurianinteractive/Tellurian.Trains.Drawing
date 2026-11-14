using Tellurian.Trains.Drawing.Editing.EndProfiles;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class MeasureFrameTests
{
    /// <summary>A 500 mm plate from 4 to 104 mm below rail top, with a hole and a raised centre point.</summary>
    private static EndProfile Profile()
    {
        var profile = EndProfileTemplate.Create(new()
        {
            Designation = "TEST",
            Scale = "H0",
            PlateWidth = 500,
            PlateTop = -4,
            PlateBottom = -104,
            Gauge = 16.5,
            ScrewHoles = [new(-200, -40)],
        });
        var plate = new PlateShape([new(-250, -4), new(0, 0), new(250, -4)], -104);
        return profile with { Outline = plate.Outline(), GroundLine = plate.GroundLine() };
    }

    [TestMethod]
    public void RailTopFrameShowsStoredCoordinates()
    {
        var frame = MeasureFrame.For(Profile(), MeasureOrigin.RailTopCentre);
        Assert.AreEqual(new Point(-200, -40), frame.ToDisplay(new(-200, -40)));
    }

    [TestMethod]
    public void LowerLeftFrameMeasuresFromThePlateCorner()
    {
        var frame = MeasureFrame.For(Profile(), MeasureOrigin.PlateLowerLeft);
        Assert.AreEqual(new Point(-250, -104), frame.Origin);
        Assert.AreEqual(new Point(50, 64), frame.ToDisplay(new(-200, -40)));
        Assert.AreEqual(new Point(-200, -40), frame.FromDisplay(new(50, 64)));
        Assert.AreEqual(250, frame.ToDisplayX(0), "A track on the centre line is 250 mm from the left edge.");
        Assert.AreEqual(104, frame.ToDisplayY(0), "Rail top is 104 mm above the bottom.");
    }

    [TestMethod]
    public void ProfileWithoutAPlateMeasuresFromRailTop() =>
        Assert.AreSame(MeasureFrame.RailTopCentre, MeasureFrame.For(Profile() with { Outline = null }, MeasureOrigin.PlateLowerLeft));

    [TestMethod]
    public void PlateDimensionsWithoutDetails()
    {
        var dimensions = EndProfileDimensions.Create(Profile(), MeasureFrame.RailTopCentre, 20, details: false);
        CollectionAssert.AreEqual(new[] { 500.0, 100, 100, 104 }, dimensions.Select(d => Math.Round(d.Length, 6)).ToArray());
    }

    [TestMethod]
    public void HoleDimensionsFromRailTopAndCentre()
    {
        var dimensions = EndProfileDimensions.Create(Profile(), MeasureFrame.RailTopCentre, 20, details: true);
        var hole = dimensions.Where(d => d.End == new Point(-200, -40)).ToList();
        CollectionAssert.AreEquivalent(new[] { 200.0, 40.0 }, hole.Select(d => d.Length).ToArray());
    }

    [TestMethod]
    public void HoleDimensionsFromTheLowerLeftCorner()
    {
        var profile = Profile();
        var dimensions = EndProfileDimensions.Create(profile, MeasureFrame.For(profile, MeasureOrigin.PlateLowerLeft), 20, details: true);
        var hole = dimensions.Where(d => d.End == new Point(-200, -40)).ToList();
        CollectionAssert.AreEquivalent(new[] { 50.0, 64.0 }, hole.Select(d => d.Length).ToArray());
    }

    [TestMethod]
    public void UpperEdgePointsAreDimensionedInRowsAboveThePlate()
    {
        var profile = Profile();
        var dimensions = EndProfileDimensions.Create(profile, MeasureFrame.For(profile, MeasureOrigin.PlateLowerLeft), 20, details: true);
        var centre = dimensions.Where(d => d.End == new Point(0, 0)).ToList();
        CollectionAssert.AreEquivalent(new[] { 104.0, 250.0 }, centre.Select(d => d.Length).ToArray());
        var rows = dimensions.Where(d => d.Kind == DimensionKind.Horizontal && d.Line().Start.Y > 0).Select(d => d.Line().Start.Y).ToList();
        Assert.AreEqual(rows.Count, rows.Distinct().Count(), "Each point's horizontal dimension has its own row.");
    }
}
