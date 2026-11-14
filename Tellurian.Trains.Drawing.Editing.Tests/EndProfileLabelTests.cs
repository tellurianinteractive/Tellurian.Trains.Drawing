using Tellurian.Trains.Drawing.Editing.EndProfiles;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class EndProfileLabelTests
{
    private const double DimensionText = 10;

    /// <summary>A 500 mm plate from 10 to 200 mm below rail top.</summary>
    private static EndProfile Profile() => EndProfileTemplate.Create(new()
    {
        Designation = "B96",
        Scale = "H0",
        PlateWidth = 500,
        PlateTop = -10,
        PlateBottom = -200,
        TrackOffsets = EndProfileTemplate.CentredOffsets(2, 45),
        Gauge = 16.5,
        ScrewHoles = [new(-150, -100), new(150, -100)],
    }) with { Standards = [new(Guid.NewGuid(), "FREMO")] };

    [TestMethod]
    public void TextIsStandardScaleAndDesignation()
    {
        Assert.AreEqual("FREMO H0 B96", EndProfileLabel.Text(Profile()));
        Assert.AreEqual("H0 B96", EndProfileLabel.Text(Profile() with { Standards = [] }));
        Assert.AreEqual("FREMO, MMM H0 B96", EndProfileLabel.Text(Profile() with { Standards = [new(Guid.NewGuid(), "FREMO"), new(Guid.NewGuid(), "MMM")] }));
    }

    [TestMethod]
    public void LabelIsCentredBelowTheLowestDimension()
    {
        var profile = Profile();
        var dimensions = EndProfileDimensions.Create(profile, MeasureFrame.For(profile, MeasureOrigin.RailTopCentre), gap: 30, details: true);
        var lowestLine = dimensions.Min(d => Math.Min(d.Line().Start.Y, d.Line().End.Y));

        var label = EndProfileLabel.Place(profile, dimensions, DimensionText)!;

        Assert.AreEqual(0, label.Center.X, Precision, "Centred under the plate.");
        Assert.IsLessThan(lowestLine - DimensionText, label.Bounds.Max.Y, "Below the lowest dimension and its text.");
        Assert.AreEqual(EndProfileLabel.MaximumSize, label.Size, Precision, "A wide plate gets the largest text.");
    }

    [TestMethod]
    public void NarrowPlateGetsSmallerText()
    {
        var narrow = EndProfileTemplate.Create(new() { Designation = "NARROW", Scale = "N", PlateWidth = 100, PlateTop = -5, PlateBottom = -50, TrackOffsets = [0], Gauge = 9 });
        var label = EndProfileLabel.Place(narrow, [], DimensionText)!;
        Assert.IsLessThan(EndProfileLabel.MaximumSize, label.Size);
        Assert.IsLessThanOrEqualTo(100 * EndProfileLabel.WidthShare + Precision, label.Bounds.Width, "Within its share of the plate's width.");
        Assert.IsLessThan(-50, label.Bounds.Max.Y, "Below the plate.");
    }

    [TestMethod]
    public void NoLabelWithoutAnOutline() =>
        Assert.IsNull(EndProfileLabel.Place(Profile() with { Outline = null, Plate = null }, [], DimensionText));
}
