using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class PlateShapeTests
{
    private static readonly Point[] Ballast = [new(-250, -4), new(-30, -4), new(-20, 0), new(20, 0), new(30, -4), new(250, -10)];

    [TestMethod]
    public void SidesFollowTheEndsOfTheUpperEdge()
    {
        var shape = new PlateShape(Ballast, -104);
        Assert.AreEqual(500, shape.Width, Precision);
        Assert.AreEqual(100, shape.LeftHeight, Precision);
        Assert.AreEqual(94, shape.RightHeight, Precision);

        var outline = shape.Outline().Tessellate().Points;
        CollectionAssert.AreEqual(new Point[] { new(-250, -104), new(250, -104), new(250, -10), new(30, -4) }, outline.Take(4).ToArray());
        Assert.IsTrue(shape.Outline().IsClosed);
        CollectionAssert.AreEqual(Ballast, shape.GroundLine().Tessellate().Points.ToArray());
    }

    [TestMethod]
    public void InvalidShapesAreRefused()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new PlateShape([new(0, 0)], -100));
        Assert.ThrowsExactly<ArgumentException>(() => new PlateShape([new(100, 0), new(-100, 0)], -100));
        Assert.ThrowsExactly<ArgumentException>(() => new PlateShape(Ballast, -2));
        Assert.IsNull(PlateShape.TryCreate([new(100, 0), new(-100, 0)], -100));
    }

    [TestMethod]
    public void ShapeIsReadBackFromItsOutline()
    {
        var shape = new PlateShape(Ballast, -104);
        var profile = TestProfiles.FremoSingle() with { Outline = shape.Outline() };
        var read = PlateShape.From(profile)!;
        Assert.AreEqual(-104, read.Bottom, Precision);
        CollectionAssert.AreEqual(Ballast, read.UpperEdge.ToArray());
    }

    [TestMethod]
    public void ClockwiseOutlineIsReadToo()
    {
        var clockwise = PathDrawing.Polyline([new(-250, -104), new(-250, -4), new(0, 0), new(250, -4), new(250, -104)], isClosed: true);
        var read = PlateShape.From(TestProfiles.FremoSingle() with { Outline = clockwise })!;
        CollectionAssert.AreEqual(new Point[] { new(-250, -4), new(0, 0), new(250, -4) }, read.UpperEdge.ToArray());
    }

    [TestMethod]
    public void TemplateRectangleIsAFlatUpperEdge()
    {
        var read = PlateShape.From(TestProfiles.FremoSingle())!;
        Assert.HasCount(2, read.UpperEdge);
        Assert.AreEqual(read.UpperEdge[0].Y, read.UpperEdge[1].Y, Precision);
    }

    [TestMethod]
    public void ArcsOfABuiltInOutlineBecomePoints()
    {
        var b96 = BuiltInCatalogue.Instance.FindEndProfile("FREMO-H0-B96", "H0")!;
        var read = PlateShape.From(b96);
        Assert.IsNotNull(read);
        Assert.AreEqual(b96.Outline!.ToPath().Bounds.Width, read.Width, Precision);
        Assert.AreEqual(b96.Outline.ToPath().Bounds.Min.Y, read.Bottom, Precision);
        Assert.IsTrue(read.UpperEdge.Count > 10, "The arcs are approximated by points.");
    }
}
