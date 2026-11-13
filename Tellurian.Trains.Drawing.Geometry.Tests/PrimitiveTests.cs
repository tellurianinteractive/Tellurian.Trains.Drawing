namespace Tellurian.Trains.Drawing.Geometry.Tests;

[TestClass]
public sealed class PrimitiveTests
{
    private const double Precision = 1e-9;

    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(360, 0)]
    [DataRow(-90, 270)]
    [DataRow(725, 5)]
    [DataRow(359.9999999999999, 0)]
    public void AngleNormalizesToZeroUpTo360(double degrees, double expected) =>
        Assert.AreEqual(expected, new Angle(degrees).Normalized.Degrees, Precision);

    [TestMethod]
    [DataRow(180, 180)]
    [DataRow(181, -179)]
    [DataRow(-180, 180)]
    [DataRow(270, -90)]
    public void AngleSignedIsInHalfOpenRange(double degrees, double expected) =>
        Assert.AreEqual(expected, new Angle(degrees).Signed.Degrees, Precision);

    [TestMethod]
    public void AnglesAcrossZeroAreClose() =>
        Assert.IsTrue(new Angle(359.9999999999).IsCloseTo(new Angle(0.0000000001), 1e-6));

    [TestMethod]
    public void AngleRadiansRoundTrip() =>
        Assert.AreEqual(123.456, Angle.FromRadians(new Angle(123.456).Radians).Degrees, Precision);

    [TestMethod]
    public void VectorRotationAndCross()
    {
        var rotated = new Vector2(1, 0).Rotated(new Angle(90));
        Assert.IsTrue(rotated.IsCloseTo(new Vector2(0, 1)));
        Assert.IsGreaterThan(0, new Vector2(1, 0).Cross(new Vector2(0, 1)));
        Assert.AreEqual(Vector2.Zero, Vector2.Zero.Normalized);
        Assert.AreEqual(5, new Vector2(3, 4).Length, Precision);
        Assert.AreEqual(90, new Vector2(0, 2).Direction.Degrees, Precision);
        Assert.AreEqual(new Vector2(-2, 1), new Vector2(1, 2).LeftNormal);
    }

    [TestMethod]
    public void PoseAdvanceAndReverse()
    {
        var pose = new Pose2(new(10, 0), new Angle(90));
        Assert.IsTrue(pose.Advanced(5).Position.IsCloseTo(new(10, 5)));
        Assert.IsTrue(pose.Left.IsCloseTo(new Vector2(-1, 0)));
        Assert.AreEqual(270, pose.Reversed().Heading.Normalized.Degrees, Precision);
    }

    [TestMethod]
    public void TransformComposesAndInverts()
    {
        var first = new Transform2(new Angle(30), new Vector2(100, -50));
        var second = new Transform2(new Angle(-75), new Vector2(-20, 400));
        var point = new Point2(12.5, 7.25);

        var composed = first.Then(second);
        Assert.IsTrue(composed.Apply(point).IsCloseTo(second.Apply(first.Apply(point))));
        Assert.IsTrue(first.Then(first.Inverse()).IsCloseTo(Transform2.Identity));
        Assert.IsTrue(first.Inverse().Apply(first.Apply(point)).IsCloseTo(point));
    }

    [TestMethod]
    public void TransformFromPoseMapsOriginToPose()
    {
        var pose = new Pose2(new(300, 200), new Angle(45));
        Assert.IsTrue(Transform2.FromPose(pose).Apply(Pose2.Origin).IsCloseTo(pose));
    }

    [TestMethod]
    public void RepeatedTransformsKeepPrecision()
    {
        // NF-8: 300 modules joined in a chain must not drift measurably.
        var step = new Transform2(new Angle(1.2), new Vector2(1000, 0));
        var total = Transform2.Identity;
        for (var i = 0; i < 300; i++) total = total.Then(step);
        for (var i = 0; i < 300; i++) total = total.Then(step.Inverse());
        Assert.IsTrue(total.IsCloseTo(Transform2.Identity, 1e-6, 1e-9));
    }

    [TestMethod]
    public void BoundingBoxOperations()
    {
        var box = BoundingBox.FromPoints([new(0, 0), new(10, 5)]);
        Assert.AreEqual(10, box.Width, Precision);
        Assert.AreEqual(5, box.Height, Precision);
        Assert.AreEqual(new Point2(5, 2.5), box.Center);
        Assert.IsTrue(BoundingBox.Empty.IsEmpty);
        Assert.AreEqual(0, BoundingBox.Empty.Width);
        Assert.AreEqual(box, BoundingBox.Empty.Union(box));
        Assert.AreEqual(box, box.Union(BoundingBox.Empty));
        Assert.AreEqual(BoundingBox.Empty, BoundingBox.Empty.Inflated(3));
        Assert.IsTrue(box.Inflated(1).Contains(new(-1, -1)));
        Assert.IsTrue(box.Intersects(BoundingBox.FromPoints([new(10, 5), new(20, 20)])));
        Assert.IsFalse(box.Intersects(BoundingBox.Empty));
        Assert.IsFalse(box.Intersects(BoundingBox.FromPoints([new(11, 0), new(20, 20)])));
    }

    [TestMethod]
    public void MirrorFlipsXOnly()
    {
        Assert.AreEqual(new Point2(-3, 4), Mirror.AboutCentreLine(new Point2(3, 4)));
        var square = new Polyline([new(0, 0), new(2, 0), new(2, 1), new(0, 1)], true);
        Assert.IsTrue(square.IsCounterClockwise);
        Assert.IsFalse(Mirror.AboutCentreLine(square).IsCounterClockwise);
        CollectionAssert.AreEqual(new[] { new Point2(-1, 0) }, Mirror.AboutCentreLine([new Point2(1, 0)]).ToArray());
    }
}
