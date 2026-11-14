using Tellurian.Trains.Drawing.Model.Halls;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class HallTests
{
    [TestMethod]
    public void RectangleHasFourWallsCounterClockwise()
    {
        var hall = Hall.Rectangle("Hall", 12000, 8000);
        Assert.AreEqual(4, hall.WallCount);
        Assert.IsTrue(hall.Outline.IsClosed);
        Assert.IsTrue(hall.Wall(1).Start.IsCloseTo(new(12000, 0), Precision));
        Assert.IsTrue(hall.Wall(1).End.IsCloseTo(new(12000, 8000), Precision));
        Assert.AreEqual(2 * (12000 + 8000), hall.Outline.ToPath().Length, Precision);
    }

    [TestMethod]
    public void MissingWallIsRefused()
    {
        var hall = Hall.Rectangle("Hall", 12000, 8000);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => hall.Wall(4));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => hall.Wall(-1));
    }

    [TestMethod]
    public void RoundPillarIsACircle()
    {
        var pillar = Obstacle.RoundPillar(new(4000, 3000), 400);
        var path = pillar.Area.ToPath();
        Assert.AreEqual(ObstacleKind.Pillar, pillar.Kind);
        Assert.IsTrue(path.IsClosed);
        Assert.AreEqual(Math.PI * 400, path.Length, Precision);
        Assert.AreEqual(400, path.Bounds.Width, Precision);
        Assert.IsTrue(path.Bounds.Center.IsCloseTo(new(4000, 3000), Precision));
    }

    [TestMethod]
    public void RectangularPillarIsCentredAndTurned()
    {
        var bounds = Obstacle.RectangularPillar(new(8000, 4000), 300, 500, new(90)).Area.ToPath().Bounds;
        Assert.AreEqual(500, bounds.Width, Precision);
        Assert.AreEqual(300, bounds.Height, Precision);
        Assert.IsTrue(bounds.Center.IsCloseTo(new(8000, 4000), Precision));
    }

    [TestMethod]
    public void KeepFreeZoneIsInFrontOfTheDoor()
    {
        var door = new Door { Position = new(new(2000, 0), new(90)), Width = 1000 };
        var bounds = door.KeepFreeZone().ToPath().Bounds;
        Assert.IsTrue(bounds.Min.IsCloseTo(new(1500, 0), Precision));
        Assert.IsTrue(bounds.Max.IsCloseTo(new(2500, Door.DefaultKeepFreeDepth), Precision));
    }

    [TestMethod]
    public void KeepFreeZoneFollowsTheWall()
    {
        var door = new Door { Position = new(new(12000, 4000), new(180)), Width = 1200, KeepFreeDepth = 1500 };
        var bounds = door.KeepFreeZone().ToPath().Bounds;
        Assert.IsTrue(bounds.Min.IsCloseTo(new(10500, 3400), Precision));
        Assert.IsTrue(bounds.Max.IsCloseTo(new(12000, 4600), Precision));
    }

    [TestMethod]
    public void FindsObstaclesById()
    {
        var hall = TestHalls.Club();
        Assert.AreSame(hall.Obstacles[1], hall.FindObstacle(hall.Obstacles[1].Id));
        Assert.IsNull(hall.FindObstacle(Guid.NewGuid()));
    }
}
