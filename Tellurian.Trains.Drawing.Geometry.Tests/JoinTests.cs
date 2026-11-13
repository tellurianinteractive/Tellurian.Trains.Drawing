namespace Tellurian.Trains.Drawing.Geometry.Tests;

[TestClass]
public sealed class JoinTests
{
    [TestMethod]
    [DataRow(0, 0, 0, 0, 0, 180)]
    [DataRow(1000, 0, 0, 0, 0, 180)]
    [DataRow(500, 300, 37, 1200, 0, 0)]
    [DataRow(-250, 80, -120, 0, 600, 270)]
    public void PlacedExitFacesTarget(double tx, double ty, double th, double ex, double ey, double eh)
    {
        var target = new Pose2(new(tx, ty), new Angle(th));
        var localExit = new Pose2(new(ex, ey), new Angle(eh));

        var placement = Join.PlacementFor(target, localExit);
        var placedExit = placement.Apply(localExit);

        Assert.IsTrue(placedExit.Position.IsCloseTo(target.Position, 1e-9));
        Assert.IsTrue(placedExit.Heading.IsCloseTo(target.Reversed().Heading, 1e-9));
        Assert.IsTrue(Join.Gap(target, placedExit).IsWithin(1e-9, 1e-9));
    }

    [TestMethod]
    public void StraightModuleContinuesStraight()
    {
        // A 1000 mm straight module: entry exit at the origin facing back, exit at x=1000 facing forward.
        var entry = new Pose2(Point2.Origin, Angle.Half);
        var exit = new Pose2(new(1000, 0), Angle.Zero);
        var previousExit = new Pose2(new(1000, 0), Angle.Zero);

        var placement = Join.PlacementFor(previousExit, entry);
        Assert.IsTrue(placement.Apply(exit).IsCloseTo(new Pose2(new(2000, 0), Angle.Zero)));
    }

    [TestMethod]
    public void FourQuarterCurvesCloseALoop()
    {
        var radius = 1000.0;
        var entry = new Pose2(Point2.Origin, Angle.Half);
        var exit = ArcSegment.FromStart(Pose2.Origin, radius, 90).EndPose;

        var first = new Pose2(Point2.Origin, Angle.Half);
        var current = new Pose2(Point2.Origin, Angle.Zero);
        for (var i = 0; i < 4; i++)
        {
            current = Join.PlacementFor(current, entry).Apply(exit);
        }
        var gap = Join.Gap(first, current);
        Assert.IsTrue(gap.IsWithin(1e-6, 1e-9), gap.ToString());
    }

    [TestMethod]
    public void GapIsExpressedInTheExitFrame()
    {
        var exit = new Pose2(new(100, 100), new Angle(90));
        // The other exit is 5 mm further out (along +Y in world), 2 mm to the exit's left (−X), turned 1°.
        var other = new Pose2(new(98, 105), new Angle(270 + 1));
        var gap = Join.Gap(exit, other);
        Assert.AreEqual(5, gap.Dx, 1e-9);
        Assert.AreEqual(2, gap.Dy, 1e-9);
        Assert.AreEqual(1, gap.Dθ.Degrees, 1e-9);
        Assert.AreEqual(Math.Sqrt(29), gap.Distance, 1e-9);
        Assert.IsFalse(gap.IsWithin(1, 10));
        Assert.IsFalse(gap.IsWithin(10, 0.5));
        Assert.IsTrue(gap.IsWithin(10, 1.5));
    }
}
