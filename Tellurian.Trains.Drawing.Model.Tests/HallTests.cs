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

    [TestMethod]
    public void DoorOnACounterClockwiseWallHeadsIntoTheHall()
    {
        var hall = Hall.Rectangle("Hall", 12000, 8000);
        Assert.IsTrue(hall.PoseOnWall(new(3000, -200)).IsCloseTo(new(new(3000, 0), new(90)), Precision));
        Assert.IsTrue(hall.PoseOnWall(new(11900, 5000)).IsCloseTo(new(new(12000, 5000), new(180)), Precision));
    }

    [TestMethod]
    public void DoorOnAClockwiseWallHeadsIntoTheHall()
    {
        var outline = PathDrawing.Polyline([new(0, 0), new(0, 8000), new(12000, 8000), new(12000, 0)], isClosed: true);
        Assert.IsTrue(Hall.PoseOnWall(outline, new(3000, 100)).IsCloseTo(new(new(3000, 0), new(90)), Precision));
    }

    [TestMethod]
    public void DoorsMoveWithTheirWall()
    {
        var hall = TestHalls.Club();
        var moved = hall.WithOutline(PathDrawing.Rectangle(TestHalls.Width, TestHalls.Depth, new Transform(Angle.Zero, new(0, -500))));
        Assert.IsTrue(moved.Doors[0].Position.Position.IsCloseTo(new(1500, -500), Precision));
        Assert.AreEqual(hall.Doors[0].Position.Heading.Degrees, moved.Doors[0].Position.Heading.Degrees, Precision);
    }

    [TestMethod]
    public void OpenWallsAreRefused()
    {
        var hall = Hall.Rectangle("Hall", 12000, 8000);
        Assert.ThrowsExactly<ArgumentException>(() => hall.WithOutline(PathDrawing.Polyline([new(0, 0), new(100, 0), new(100, 100)])));
    }

    [TestMethod]
    public void FloorAreaOfARectangle() =>
        Assert.AreEqual(12000.0 * 8000, Hall.Rectangle("Hall", 12000, 8000).FloorArea, Precision);

    [TestMethod]
    public void RecognisesPillarShapes()
    {
        Assert.IsTrue(Obstacle.RoundPillar(new(4000, 3000), 400).TryGetCircle(out var center, out var diameter));
        Assert.IsTrue(center.IsCloseTo(new(4000, 3000), Precision));
        Assert.AreEqual(400, diameter, Precision);
        Assert.IsFalse(Obstacle.RoundPillar(new(4000, 3000), 400).TryGetRectangle(out _, out _, out _, out _));

        var square = Obstacle.RectangularPillar(new(8000, 4000), 300, 500, new(15));
        Assert.IsFalse(square.TryGetCircle(out _, out _));
        Assert.IsTrue(square.TryGetRectangle(out var squareCenter, out var width, out var depth, out var rotation));
        Assert.IsTrue(squareCenter.IsCloseTo(new(8000, 4000), Precision));
        Assert.AreEqual(300, width, Precision);
        Assert.AreEqual(500, depth, Precision);
        Assert.AreEqual(15, rotation.Degrees, Precision);
    }

    [TestMethod]
    public void ModuleEntranceIsSavedAndMissingMeansNo()
    {
        var hall = TestHalls.Club();
        var read = Files.DrawingFileSerializer.DeserializeHall(Files.DrawingFileSerializer.Serialize(hall));
        Assert.IsTrue(read.Doors[0].IsModuleEntrance);
        Assert.IsFalse(read.Doors[1].IsModuleEntrance);
        Assert.IsFalse(new Door { Position = Pose.Origin, Width = 1000 }.IsModuleEntrance);
    }

    [TestMethod]
    public void EffectiveFloorAreaLeavesOutObstaclesDoorsAndZones()
    {
        var hall = Hall.Rectangle("Hall", 12000, 8000) with
        {
            Obstacles = [Obstacle.RectangularPillar(new(4000, 4000), 400, 400)],
            Doors = [new() { Position = new(new(2000, 0), new(90)), Width = 1000, KeepFreeDepth = 1000 }],
            NoGoZones = [new() { Area = PathDrawing.Rectangle(new(10000, 6000), 1000, 1000, Angle.Zero), Reason = "Fire hose" }],
        };
        Assert.AreEqual(12000.0 * 8000 - 400 * 400 - 1000 * 1000 - 1000 * 1000, hall.EffectiveFloorArea, hall.FloorArea * RelativeArea);
    }

    [TestMethod]
    public void EffectiveFloorAreaCountsOverlapsOnce()
    {
        var hall = Hall.Rectangle("Hall", 12000, 8000) with
        {
            // The pillar stands inside the no-go zone, which reaches 500 mm outside the walls.
            Obstacles = [Obstacle.RectangularPillar(new(1000, 4000), 400, 400)],
            NoGoZones = [new() { Area = PathDrawing.Rectangle(new(500, 4000), 2000, 1000, Angle.Zero), Reason = "Stage" }],
        };
        Assert.AreEqual(12000.0 * 8000 - 1500 * 1000, hall.EffectiveFloorArea, hall.FloorArea * RelativeArea);
    }

    [TestMethod]
    public void EffectiveFloorAreaWithoutRoundPillarIsCloseToTheCircle()
    {
        var hall = Hall.Rectangle("Hall", 12000, 8000) with { Obstacles = [Obstacle.RoundPillar(new(4000, 4000), 400)] };
        Assert.AreEqual(12000.0 * 8000 - Math.PI * 200 * 200, hall.EffectiveFloorArea, hall.FloorArea * RelativeArea);
    }
}
