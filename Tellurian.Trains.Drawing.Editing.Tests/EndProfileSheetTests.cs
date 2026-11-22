using Tellurian.Trains.Drawing.Editing.EndProfiles;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.Catalogues;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class EndProfileSheetTests
{
    private static EndProfile B96() => SampleCatalogue.Instance.FindEndProfile("FREMO-H0-B96f", "H0")!;

    private static SheetLayout Sheet(EndProfile profile) =>
        EndProfileSheet.Create(profile, MeasureFrame.For(profile, MeasureOrigin.PlateLowerLeft), details: true);

    [TestMethod]
    public void PrintedAreaHoldsTheDrawingDimensionsAndTitle()
    {
        var profile = B96();
        var sheet = Sheet(profile);
        Assert.IsNotNull(sheet.Title);
        Assert.IsTrue(Contains(sheet.Bounds, EndProfileSheet.DrawingBounds(profile)), "The drawing.");
        Assert.IsTrue(Contains(sheet.Bounds, sheet.Title.Bounds), "The title.");
        foreach (var dimension in sheet.Dimensions)
        {
            var (start, end) = dimension.Line();
            Assert.IsTrue(sheet.Bounds.Contains(start) && sheet.Bounds.Contains(end), "Each dimension line.");
        }
    }

    [TestMethod]
    public void PrintedAreaIsTight()
    {
        var profile = B96();
        var sheet = Sheet(profile);
        var drawing = EndProfileSheet.DrawingBounds(profile);
        Assert.IsLessThan(1.5, sheet.Bounds.Width / drawing.Width, "Little room beside the drawing.");
        var scale = Math.Min(EndProfileSheet.PageWidth / sheet.Bounds.Width, EndProfileSheet.PageHeight / sheet.Bounds.Height);
        Assert.AreEqual(EndProfileSheet.PaperTextSize, sheet.TextSize * scale, 1, "Dimension text is about its paper size when printed.");
    }

    [TestMethod]
    public void ProfileWithoutADrawingStillGetsAnArea()
    {
        var empty = B96().With(edited => { edited.Outline = null; edited.Plate = null; edited.GroundLine = null; edited.Tracks = []; edited.ScrewHoles = []; edited.Dowels = []; });
        var sheet = Sheet(empty);
        Assert.IsGreaterThan(0, sheet.Bounds.Width);
        Assert.IsGreaterThan(0, sheet.Bounds.Height);
        Assert.IsNull(sheet.Title);
    }

    private static bool Contains(BoundingBox outer, BoundingBox inner) => outer.Contains(inner.Min) && outer.Contains(inner.Max);
}
