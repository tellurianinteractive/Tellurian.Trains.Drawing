namespace Tellurian.Trains.Drawing.Geometry.Tests;

[TestClass]
public sealed class ComparisonTests
{
    [TestMethod]
    public void IdenticalShapesHaveNoDeviation()
    {
        var line = new Polyline([new(-200, 0), new(-100, 20), new(100, 20), new(200, 0)], false);
        Assert.AreEqual(0, ShapeComparison.MaximumDeviation(line, line).Maximum, 1e-9);
    }

    [TestMethod]
    public void SymmetricShapeMatchesItsMirror()
    {
        var line = new Polyline([new(-200, 0), new(-100, 20), new(100, 20), new(200, 0)], false);
        Assert.IsTrue(ShapeComparison.MaximumDeviation(line, Mirror.AboutCentreLine(line)).IsWithin(1e-9));
    }

    [TestMethod]
    public void AsymmetricShapeDeviatesFromItsMirror()
    {
        var line = new Polyline([new(-200, 0), new(-100, 30), new(100, 20), new(200, 0)], false);
        var deviation = ShapeComparison.MaximumDeviation(line, Mirror.AboutCentreLine(line), 0.1);
        Assert.IsGreaterThan(5, deviation.Maximum);
        Assert.IsLessThan(10.01, deviation.Maximum);
    }

    [TestMethod]
    public void DeviationIsTwoWay()
    {
        var shortLine = new Polyline([new(0, 0), new(100, 0)], false);
        var longLine = new Polyline([new(0, 0), new(150, 0)], false);
        var deviation = ShapeComparison.MaximumDeviation(shortLine, longLine);
        Assert.AreEqual(50, deviation.Maximum, 1e-9);
        Assert.IsTrue(deviation.At.IsCloseTo(new(150, 0)));
    }

    [TestMethod]
    public void OffsetShapeDeviatesByOffset()
    {
        var a = new Polyline([new(0, 0), new(100, 0)], false);
        var b = new Polyline([new(0, 3), new(100, 3)], false);
        Assert.AreEqual(3, ShapeComparison.MaximumDeviation(a, b).Maximum, 1e-9);
    }

    [TestMethod]
    public void SampleSpacingMustBePositive() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            ShapeComparison.MaximumDeviation(new([new(0, 0), new(1, 0)], false), new([new(0, 0), new(1, 0)], false), 0));

    [TestMethod]
    public void PointsMatchWithinTolerance()
    {
        Point[] first = [new(-150, 50), new(150, 50), new(0, 100)];
        Point[] second = [new(150.3, 50), new(-150, 49.8), new(0, 200)];
        var result = PointMatching.Match(first, second, 0.5);
        Assert.HasCount(2, result.Pairs);
        Assert.AreEqual(new PointPair(0, 1, first[0].DistanceTo(second[1])), result.Pairs[0]);
        Assert.AreEqual(1, result.Pairs[1].FirstIndex);
        Assert.AreEqual(0, result.Pairs[1].SecondIndex);
        CollectionAssert.AreEqual(new[] { 2 }, result.UnmatchedFirst.ToArray());
        CollectionAssert.AreEqual(new[] { 2 }, result.UnmatchedSecond.ToArray());
        Assert.IsFalse(result.IsComplete);
    }

    [TestMethod]
    public void ClosestPairsWin()
    {
        // Both first points are within tolerance of second[0]; the closer one gets it.
        Point[] first = [new(0, 0), new(0.9, 0)];
        Point[] second = [new(1, 0), new(5, 0)];
        var result = PointMatching.Match(first, second, 1.5);
        Assert.HasCount(1, result.Pairs);
        Assert.AreEqual(1, result.Pairs[0].FirstIndex);
        CollectionAssert.AreEqual(new[] { 0 }, result.UnmatchedFirst.ToArray());
    }

    [TestMethod]
    public void EmptyAndCompleteSets()
    {
        Assert.IsTrue(PointMatching.Match([], [], 1).IsComplete);
        Point[] points = [new(1, 2), new(3, 4)];
        Assert.IsTrue(PointMatching.Match(points, points, 0).IsComplete);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => PointMatching.Match(points, points, -1));
    }
}
