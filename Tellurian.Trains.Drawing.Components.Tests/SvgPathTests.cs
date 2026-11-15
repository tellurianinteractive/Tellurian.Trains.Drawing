using Tellurian.Trains.Drawing.Components.Rendering;
using Tellurian.Trains.Drawing.Model;

namespace Tellurian.Trains.Drawing.Components.Tests;

[TestClass]
public sealed class SvgPathTests
{
    [TestMethod]
    public void ClosedPolyline()
    {
        var path = PathDrawing.Polyline([new(0, 0), new(1200, 0), new(1200, 500.25), new(0, 500.25)], isClosed: true);
        Assert.AreEqual("M 0 0 L 1200 0 L 1200 500.25 L 0 500.25 L 0 0 Z", SvgPath.From(path));
    }

    [TestMethod]
    public void CounterClockwiseArcHasSweepFlagOne()
    {
        var path = new PathBuilder(Pose.Origin).Curve(1000, 90).Build();
        Assert.AreEqual("M 0 0 A 1000 1000 0 0 1 1000 1000", SvgPath.From(path));
    }

    [TestMethod]
    public void ClockwiseArcHasSweepFlagZero()
    {
        var path = new PathBuilder(Pose.Origin).Curve(1000, -90).Build();
        Assert.AreEqual("M 0 0 A 1000 1000 0 0 0 1000 -1000", SvgPath.From(path));
    }

    [TestMethod]
    public void FullCircleIsDrawnInTwoHalves()
    {
        var circle = SvgPath.From(PathDrawing.Circle(new(0, 0), 400));
        Assert.AreEqual("M 200 0 A 200 200 0 0 1 -200 0 A 200 200 0 0 1 200 0 Z", circle);
    }

    [TestMethod]
    public void ArcLongerThanHalfATurnIsSplit()
    {
        var path = new PathBuilder(Pose.Origin).Curve(100, 270).Build();
        Assert.AreEqual(2, SvgPath.From(path).Split(" A ").Length - 1);
    }

    [TestMethod]
    public void NumbersAreInvariant()
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("sv-SE");
            Assert.AreEqual("M 1.5 -2.25 L 3.125 0", SvgPath.Polyline([new(1.5, -2.25), new(3.125, 0)]));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }
    }
}
