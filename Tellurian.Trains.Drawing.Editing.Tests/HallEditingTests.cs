using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Editing.Halls;
using Tellurian.Trains.Drawing.Editing.Selection;
using Tellurian.Trains.Drawing.Editing.Snapping;
using Tellurian.Trains.Drawing.Editing.Tools;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.Halls;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class HallEditingTests
{
    private const double Width = 12000;
    private const double Depth = 8000;
    private const double Reach = 50;

    private static Hall Hall() => Model.Halls.Hall.Rectangle("Hall", Width, Depth);

    private static Hall WithDoor() => HallCommands.AddDoor(new(3000, 100), DoorKind.Door).Apply(Hall());

    [TestMethod]
    public void DoorIsPlacedInTheClosestWallHeadingIn()
    {
        var door = WithDoor().Doors.Single();
        Assert.IsTrue(door.Position.IsCloseTo(new(new(3000, 0), new(90)), Precision));
        Assert.AreEqual(HallCommands.DefaultDoorWidth, door.Width);
    }

    [TestMethod]
    public void EmergencyExitIsWider()
    {
        var exit = HallCommands.AddDoor(new(Width, 4000), DoorKind.EmergencyExit).Apply(Hall()).Doors.Single();
        Assert.AreEqual(DoorKind.EmergencyExit, exit.Kind);
        Assert.AreEqual(HallCommands.DefaultEmergencyExitWidth, exit.Width);
    }

    [TestMethod]
    public void MovingACornerReshapesTheWallsAndKeepsDoorsInThem()
    {
        var moved = HallCommands.Move([HallItem.Corner(0)], new(0, -1000)).Apply(WithDoor());
        var corners = PathCorners.From(moved.Outline).Corners;
        Assert.IsTrue(corners[0].IsCloseTo(new(0, -1000), Precision));
        Assert.IsTrue(corners[1].IsCloseTo(new(Width, 0), Precision));
        var door = moved.Doors.Single().Position;
        Assert.AreEqual(0, Measure.PointToSegment(door.Position, corners[0], corners[1]), Precision, "The door is still in the wall.");
    }

    [TestMethod]
    public void MovingAWallMovesBothItsCorners()
    {
        var moved = HallCommands.Move([HallItem.Wall(1)], new(500, 0)).Apply(Hall());
        var corners = PathCorners.From(moved.Outline).Corners;
        Assert.IsTrue(corners[1].IsCloseTo(new(Width + 500, 0), Precision));
        Assert.IsTrue(corners[2].IsCloseTo(new(Width + 500, Depth), Precision));
        Assert.AreEqual((Width + 500) * Depth, moved.FloorArea, Precision);
    }

    [TestMethod]
    public void MovingTheLastWallMovesTheFirstCorner()
    {
        var moved = HallCommands.Move([HallItem.Wall(3)], new(-500, 0)).Apply(Hall());
        var corners = PathCorners.From(moved.Outline).Corners;
        Assert.IsTrue(corners[0].IsCloseTo(new(-500, 0), Precision));
        Assert.IsTrue(corners[3].IsCloseTo(new(-500, Depth), Precision));
    }

    [TestMethod]
    public void MovingADoorSlidesItAlongTheWall()
    {
        var moved = HallCommands.Move([HallItem.Door(0)], new(1000, 300)).Apply(WithDoor());
        Assert.IsTrue(moved.Doors[0].Position.IsCloseTo(new(new(4000, 0), new(90)), Precision));
    }

    [TestMethod]
    public void MovingAPillarMovesItsArea()
    {
        var hall = HallCommands.AddRoundPillar(new(4000, 4000)).Apply(Hall());
        var moved = HallCommands.Move([HallItem.Obstacle(0)], new(100, 200)).Apply(hall);
        Assert.IsTrue(moved.Obstacles[0].TryGetCircle(out var center, out var diameter));
        Assert.IsTrue(center.IsCloseTo(new(4100, 4200), Precision));
        Assert.AreEqual(HallCommands.DefaultPillarDiameter, diameter, Precision);
    }

    [TestMethod]
    public void DeletingCornersKeepsThree()
    {
        var hall = HallCommands.Delete([HallItem.Corner(0), HallItem.Corner(1)]).Apply(Hall());
        Assert.AreEqual(3, hall.WallCount, "Only one corner could go.");
    }

    [TestMethod]
    public void DeletesObstaclesDoorsAndZones()
    {
        var hall = HallCommands.AddNoGoZone(PathDrawing.Rectangle(new(6000, 7800), 800, 400, Angle.Zero), "Fire hose").Apply(
            HallCommands.AddRectangularPillar(new(4000, 4000)).Apply(WithDoor()));
        var deleted = HallCommands.Delete([HallItem.Obstacle(0), HallItem.Door(0), HallItem.NoGoZone(0), HallItem.Wall(0)]).Apply(hall);
        Assert.IsEmpty(deleted.Obstacles);
        Assert.IsEmpty(deleted.Doors);
        Assert.IsEmpty(deleted.NoGoZones);
        Assert.AreEqual(4, deleted.WallCount, "Walls aren't deleted.");
    }

    [TestMethod]
    public void OpenPathsDontReplaceTheWallsOrAddAreas()
    {
        var open = PathDrawing.Polyline([new(0, 0), new(1000, 0), new(1000, 1000)]);
        var hall = Hall();
        Assert.AreSame(hall, HallCommands.SetOutline(open).Apply(hall));
        Assert.AreSame(hall, HallCommands.AddFurniture(open).Apply(hall));
        Assert.AreSame(hall, HallCommands.AddNoGoZone(open, "x").Apply(hall));
    }

    [TestMethod]
    public void NewRectangleKeepsTheLowerLeftCorner()
    {
        var shifted = HallCommands.Move([HallItem.Corner(0), HallItem.Corner(1), HallItem.Corner(2), HallItem.Corner(3)], new(100, 200)).Apply(Hall());
        var resized = HallCommands.SetRectangle(20000, 10000).Apply(shifted);
        var bounds = resized.Outline.ToPath().Bounds;
        Assert.IsTrue(bounds.Min.IsCloseTo(new(100, 200), Precision));
        Assert.AreEqual(20000, bounds.Width, Precision);
        Assert.AreEqual(10000, bounds.Height, Precision);
    }

    [TestMethod]
    public void WallLengthAndArc()
    {
        var longer = HallCommands.SetWallLength(0, 15000).Apply(Hall());
        Assert.IsTrue(PathCorners.From(longer.Outline).Corners[1].IsCloseTo(new(15000, 0), Precision));
        var curved = HallCommands.SetWallSweep(2, 20).Apply(Hall());
        Assert.IsInstanceOfType<ArcStep>(curved.Outline.Steps[2]);
        Assert.AreSame(curved, HallCommands.SetWallSweep(2, 360).Apply(curved), "A full turn isn't a wall.");
    }

    [TestMethod]
    public void ChangedDoorStaysInTheWall()
    {
        var hall = WithDoor();
        var changed = HallCommands.SetDoor(0, hall.Doors[0] with { Width = 1500, Position = new(new(3000, 500), Angle.Zero) }).Apply(hall);
        Assert.AreEqual(1500, changed.Doors[0].Width);
        Assert.IsTrue(changed.Doors[0].Position.IsCloseTo(new(new(3000, 0), new(90)), Precision));
    }

    [TestMethod]
    public void HitTestPrefersCornersThenDoorsThenWalls()
    {
        var scene = new HallScene();
        var hall = WithDoor();
        Assert.IsTrue(scene.TryHitTest(hall, new(10, 10), Reach, out var corner));
        Assert.AreEqual(HallItem.Corner(0), corner);
        Assert.IsTrue(scene.TryHitTest(hall, new(3200, 10), Reach, out var door));
        Assert.AreEqual(HallItem.Door(0), door);
        Assert.IsTrue(scene.TryHitTest(hall, new(Width - 10, 4000), Reach, out var wall));
        Assert.AreEqual(HallItem.Wall(1), wall);
        Assert.IsFalse(scene.TryHitTest(hall, new(6000, 4000), Reach, out _), "The floor isn't an item.");
    }

    [TestMethod]
    public void HitTestFindsObstaclesAndZonesInside()
    {
        var scene = new HallScene();
        var hall = HallCommands.AddNoGoZone(PathDrawing.Rectangle(new(8000, 6000), 1000, 1000, Angle.Zero), "Fire hose").Apply(
            HallCommands.AddRoundPillar(new(4000, 4000)).Apply(Hall()));
        Assert.IsTrue(scene.TryHitTest(hall, new(4050, 4000), Reach, out var pillar));
        Assert.AreEqual(HallItem.Obstacle(0), pillar);
        Assert.IsTrue(scene.TryHitTest(hall, new(8100, 6100), Reach, out var zone));
        Assert.AreEqual(HallItem.NoGoZone(0), zone);
    }

    [TestMethod]
    public void WallTargetsMoveWithTheirCorners()
    {
        var scene = new HallScene();
        var hall = Hall();
        var lastWall = scene.SnapTargetsOf(hall, HallItem.Wall(3)).Single();
        Assert.IsTrue(scene.MovesWith(lastWall, new HashSet<HallItem> { HallItem.Corner(0) }), "The last wall ends at corner 0.");
        Assert.IsTrue(scene.MovesWith(lastWall, new HashSet<HallItem> { HallItem.Corner(3) }));
        Assert.IsFalse(scene.MovesWith(lastWall, new HashSet<HallItem> { HallItem.Corner(1) }));
        Assert.IsTrue(scene.MovesWith(lastWall, new HashSet<HallItem> { HallItem.Wall(3) }));
    }

    [TestMethod]
    public void DraggingACornerDoesntSnapToItsOwnWalls()
    {
        var document = new EditorDocument<Hall>(Hall());
        var selection = new SelectionSet<HallItem>();
        var scene = new HallScene();
        var snap = new SnapEngine(HallScene.IndexCellSize);
        snap.AddRange(scene.AllSnapTargets(document.Current));
        var tool = new SelectMoveTool<Hall, HallItem>(document, selection, scene, snap);

        // Drag corner 1 (Width, 0) to just beside its own wall 1, which would otherwise pull it back onto the wall line.
        tool.PointerDown(new(new(Width, 0), Reach));
        tool.PointerMove(new(new(Width + 20, 3000), Reach));
        tool.PointerUp(new(new(Width + 20, 3000), Reach));

        Assert.IsTrue(PathCorners.From(document.Current.Outline).Corners[1].IsCloseTo(new(Width + 20, 3000), Precision));
    }

    [TestMethod]
    public void ItemsRoundTripAsText()
    {
        Assert.IsTrue(HallItem.TryParse(HallItem.NoGoZone(2).ToString(), out var item));
        Assert.AreEqual(HallItem.NoGoZone(2), item);
        Assert.IsFalse(HallItem.TryParse("Nothing:1", out _));
    }

    [TestMethod]
    public void EntranceSymbolIsInFrontOfTheDoor()
    {
        var door = WithDoor().Doors[0] with { IsModuleEntrance = true };
        var zone = door.KeepFreeZone().Tessellate();
        var symbol = HallScene.EntranceSymbol(door);
        Assert.HasCount(10, symbol);
        Assert.IsTrue(symbol.All(zone.Contains), "The whole arrow is in the area kept free in front of the door.");
    }

    [TestMethod]
    public void EmergencySignIsCentredInFrontOfTheExit()
    {
        var exit = HallCommands.AddDoor(new(3000, 0), DoorKind.EmergencyExit).Apply(Hall()).Doors[0];
        var (center, size, _, _) = HallScene.EmergencySign(exit);
        Assert.IsTrue(center.IsCloseTo(new(3000, exit.KeepFreeDepth / 2), Precision));
        Assert.IsTrue(size > 0 && size <= Math.Min(exit.Width, exit.KeepFreeDepth));
    }

    [TestMethod]
    public void EmergencyExitThatIsAModuleEntranceHasSignAndArrowSideBySide()
    {
        var exit = HallCommands.AddDoor(new(3000, 0), DoorKind.EmergencyExit).Apply(Hall()).Doors[0] with { IsModuleEntrance = true };
        var (center, size, _, _) = HallScene.EmergencySign(exit);
        var arrow = HallScene.EntranceSymbol(exit);
        // The door heads up (+Y), so its left is -X: the sign is to the left of the opening's centre, the arrow to the right.
        Assert.IsTrue(center.X + size / 2 <= 3000 + Precision, "The sign is in the left half.");
        Assert.IsTrue(arrow.All(p => p.X >= 3000 - Precision), "The arrow is in the right half.");
        Assert.IsTrue(arrow.All(exit.KeepFreeZone().Tessellate().Contains));
    }

    [TestMethod]
    [DataRow(1500, 0)]
    [DataRow(Width, 4000)]
    [DataRow(6000, Depth)]
    [DataRow(0, 4000)]
    public void EmergencySignRunsTowardsTheExitAndNeverStandsUpsideDown(double x, double y)
    {
        var exit = HallCommands.AddDoor(new(x, y), DoorKind.EmergencyExit).Apply(Hall()).Doors[0];
        var (_, _, rotation, mirrored) = HallScene.EmergencySign(exit);
        var running = Vector.FromAngle(rotation) * (mirrored ? -1 : 1);
        Assert.IsTrue(running.IsCloseTo(-exit.Position.Forward, Precision), "The figure runs towards the wall.");
        Assert.IsTrue(Vector.FromAngle(rotation + Angle.Quarter).Y >= -Precision, "The sign's top doesn't point down.");
    }

    [TestMethod]
    public void DoorPositionAlongItsWall()
    {
        var hall = WithDoor();
        var (wall, distance) = hall.WallPositionOf(hall.Doors[0].Position.Position);
        Assert.AreEqual(0, wall);
        Assert.AreEqual(3000, distance, Precision);

        var moved = HallCommands.SetDoorPosition(0, 1, 2000).Apply(hall);
        Assert.IsTrue(moved.Doors[0].Position.IsCloseTo(new(new(Width, 2000), new(180)), Precision), "In wall 1, 2 m from corner 2, heading in.");
    }

    [TestMethod]
    public void DoorOpeningStaysWithinItsWall()
    {
        var moved = HallCommands.SetDoorPosition(0, 0, 100).Apply(WithDoor());
        Assert.AreEqual(HallCommands.DefaultDoorWidth / 2, moved.Doors[0].Position.Position.X, Precision);
    }

    [TestMethod]
    public void DoorDimensionsMeasureFromTheCornersToTheOpening()
    {
        var dimensions = HallScene.DoorDimensions(WithDoor(), 0, 300);
        Assert.HasCount(2, dimensions);
        Assert.AreEqual(3000 - HallCommands.DefaultDoorWidth / 2, dimensions[0].Length, Precision);
        Assert.AreEqual(Width - 3000 - HallCommands.DefaultDoorWidth / 2, dimensions[1].Length, Precision);
        Assert.IsTrue(dimensions.All(d => d.Line().Start.Y > 0), "The dimension lines are inside the hall.");
    }

    [TestMethod]
    public void DoorInACurvedWallHasNoStraightDimensions()
    {
        var hall = HallCommands.SetWallSweep(0, 20).Apply(WithDoor());
        Assert.IsEmpty(HallScene.DoorDimensions(hall, 0, 300));
    }

    [TestMethod]
    public void SignAndArrowAreStackedInADeepArea()
    {
        var exit = HallCommands.AddDoor(new(3000, 0), DoorKind.EmergencyExit).Apply(Hall()).Doors[0] with { IsModuleEntrance = true, KeepFreeDepth = 3000 };
        var (center, size, _, _) = HallScene.EmergencySign(exit);
        var arrow = HallScene.EntranceSymbol(exit);
        Assert.AreEqual(3000, center.X, Precision, "Stacked on the door's centre line.");
        Assert.IsTrue(center.Y + size / 2 <= 1500 + Precision, "The sign is in the half nearest the door.");
        Assert.IsTrue(arrow.All(p => p.Y >= 1500 - Precision), "The arrow is in the half further into the hall.");
        Assert.IsTrue(size > Math.Min(exit.Width / 2, exit.KeepFreeDepth) * 0.8, "Larger than side by side.");
    }

    [TestMethod]
    public void LabelIsBeyondTheSymbols()
    {
        var door = WithDoor().Doors[0] with { IsModuleEntrance = true };
        var label = HallScene.LabelPosition(door, "Gate", 200);
        Assert.IsTrue(label.Y - 100 >= door.KeepFreeDepth, "The whole text is outside the area in front of the door.");
        Assert.IsTrue(HallScene.EntranceSymbol(door).All(p => p.Y <= door.KeepFreeDepth));
    }

    [TestMethod]
    public void LabelOfADoorAtTheSideClearsTheAreaWithItsWidth()
    {
        var exit = HallCommands.AddDoor(new(Width, 4000), DoorKind.EmergencyExit).Apply(Hall()).Doors[0];
        const double textSize = 200;
        var shortLabel = HallScene.LabelPosition(exit, "E", textSize);
        var longLabel = HallScene.LabelPosition(exit, "Emergency exit 2", textSize);
        Assert.IsTrue(shortLabel.X < Width - exit.KeepFreeDepth, "Beyond the area in front of the exit.");
        Assert.IsTrue(longLabel.X < shortLabel.X - textSize, "A longer text is centred further into the hall.");
        Assert.AreEqual(4000, longLabel.Y, Precision, "Level with the exit.");
    }

    [TestMethod]
    [DataRow(19950.0, 6000.0, 20000.0, 6000.0, 180.0)]
    [DataRow(50.0, 6000.0, 0.0, 6000.0, 0.0)]
    [DataRow(10000.0, 11950.0, 10000.0, 12000.0, 270.0)]
    public void DoorsGoInTheShortWallsToo(double x, double y, double expectedX, double expectedY, double heading)
    {
        var hall = HallCommands.AddDoor(new(x, y), DoorKind.Door).Apply(Model.Halls.Hall.Rectangle("Hall", 20000, 12000));
        Assert.IsTrue(hall.Doors[0].Position.IsCloseTo(new(new(expectedX, expectedY), new(heading)), Precision), hall.Doors[0].Position.ToString());
    }

    [TestMethod]
    public void DraggingARectanglesCornerKeepsTheOppositeCornerAndItsShape()
    {
        var hall = HallCommands.AddRectangularPillar(new(4000, 4000), 400, 400).Apply(Hall());
        var corners = PathCorners.From(hall.Obstacles[0].Area).Corners;
        var opposite = corners[0];
        var handle = HallItem.ObstacleHandle(0, 2);
        var resized = HallCommands.Move([handle], new(600, 300)).Apply(hall);
        Assert.IsTrue(resized.Obstacles[0].TryGetRectangle(out _, out var width, out var depth, out _));
        Assert.AreEqual(1000, width, Precision);
        Assert.AreEqual(700, depth, Precision);
        Assert.IsTrue(PathCorners.From(resized.Obstacles[0].Area).Corners.Any(c => c.IsCloseTo(opposite, Precision)), "The opposite corner stays.");
    }

    [TestMethod]
    public void DraggingARotatedRectanglesCornerKeepsItsRotation()
    {
        var hall = Hall() with { Obstacles = [Obstacle.RectangularPillar(new(4000, 4000), 400, 200, new(30))] };
        var corner = HallHandles.PositionOf(hall, HallItem.ObstacleHandle(0, 2))!.Value;
        var resized = HallHandles.Drag(hall, HallItem.ObstacleHandle(0, 2), corner + Vector.FromAngle(new(30), 100));
        Assert.IsTrue(resized.Obstacles[0].TryGetRectangle(out _, out var width, out var depth, out var rotation));
        Assert.AreEqual(500, width, Precision);
        Assert.AreEqual(200, depth, Precision);
        Assert.AreEqual(30, rotation.Degrees, Precision);
    }

    [TestMethod]
    public void DraggingAPolygonsCornerMovesOnlyThatCorner()
    {
        var hall = HallCommands.AddFurniture(PathDrawing.Polyline([new(1000, 1000), new(2000, 1000), new(1500, 2000)], isClosed: true)).Apply(Hall());
        var resized = HallHandles.Drag(hall, HallItem.ObstacleHandle(0, 2), new(1500, 2500));
        var corners = PathCorners.From(resized.Obstacles[0].Area).Corners;
        Assert.IsTrue(corners[2].IsCloseTo(new(1500, 2500), Precision));
        Assert.IsTrue(corners[0].IsCloseTo(new(1000, 1000), Precision));
    }

    [TestMethod]
    public void DraggingARoundPillarsHandleSetsTheDiameter()
    {
        var hall = HallCommands.AddRoundPillar(new(4000, 4000)).Apply(Hall());
        var resized = HallHandles.Drag(hall, HallItem.ObstacleHandle(0, 0), new(4000, 4350));
        Assert.IsTrue(resized.Obstacles[0].TryGetCircle(out var center, out var diameter));
        Assert.AreEqual(700, diameter, Precision);
        Assert.IsTrue(center.IsCloseTo(new(4000, 4000), Precision));
    }

    [TestMethod]
    public void DraggingADoorsEndChangesItsWidthAlongTheWall()
    {
        var hall = WithDoor();
        var (a, b) = HallScene.Opening(hall.Doors[0]);
        var right = a.X > b.X ? 0 : 1;
        var resized = HallHandles.Drag(hall, HallItem.DoorHandle(0, right), new(4000, 300));
        Assert.AreEqual(1500, resized.Doors[0].Width, Precision);
        Assert.IsTrue(resized.Doors[0].Position.IsCloseTo(new(new(3250, 0), new(90)), Precision), "The other end stays at 2500.");
    }

    [TestMethod]
    public void DraggingADoorsFarHandleSetsTheDepthKeptFree()
    {
        var resized = HallHandles.Drag(WithDoor(), HallItem.DoorHandle(0, 2), new(3100, 1800));
        Assert.AreEqual(1800, resized.Doors[0].KeepFreeDepth, Precision);
    }

    [TestMethod]
    public void HandlesDontMakeItemsTooSmall()
    {
        var hall = HallCommands.AddRectangularPillar(new(4000, 4000), 400, 400).Apply(Hall());
        var opposite = PathCorners.From(hall.Obstacles[0].Area).Corners[0];
        Assert.AreSame(hall, HallHandles.Drag(hall, HallItem.ObstacleHandle(0, 2), opposite + new Vector(10, 300)));
    }

    [TestMethod]
    public void HandlesCanOnlyBeGrabbedOnSelectedItems()
    {
        var scene = new HallScene();
        var hall = HallCommands.AddRectangularPillar(new(4000, 4000), 400, 400).Apply(Hall());
        var corner = HallHandles.PositionOf(hall, HallItem.ObstacleHandle(0, 2))!.Value;
        Assert.IsTrue(scene.TryHitTest(hall, corner, Reach, out var before));
        Assert.AreEqual(HallItem.Obstacle(0), before, "Unselected: the pillar itself.");

        scene.ShowHandlesFor([HallItem.Obstacle(0)]);
        Assert.IsTrue(scene.TryHitTest(hall, corner, Reach, out var after));
        Assert.AreEqual(HallItem.ObstacleHandle(0, 2), after);
        Assert.IsTrue(scene.TryHitTest(hall, new(4000, 4000), Reach, out var inside));
        Assert.AreEqual(HallItem.Obstacle(0), inside, "Inside the pillar it moves as a whole.");
    }

    [TestMethod]
    public void DeletingAHandleDeletesItsItem() =>
        Assert.IsEmpty(HallCommands.Delete([HallItem.DoorHandle(0, 1)]).Apply(WithDoor()).Doors);

    [TestMethod]
    public void HandlesRoundTripAsText()
    {
        Assert.IsTrue(HallItem.TryParse(HallItem.DoorHandle(3, 2).ToString(), out var item));
        Assert.AreEqual(HallItem.DoorHandle(3, 2), item);
        Assert.AreEqual(HallItem.Door(3), item.Owner);
    }

    [TestMethod]
    public void PrintedHallFitsALandscapePage()
    {
        var hall = Hall() with { Address = "Station Road 1" };
        var sheet = HallSheet.Create(hall);
        var scale = Math.Min(EndProfiles.EndProfileSheet.PageWidth / sheet.Bounds.Width, EndProfiles.EndProfileSheet.PageHeight / sheet.Bounds.Height);
        Assert.IsTrue(sheet.Bounds.Width * scale <= EndProfiles.EndProfileSheet.PageWidth + Precision);
        Assert.IsTrue(sheet.Bounds.Height * scale <= EndProfiles.EndProfileSheet.PageHeight + Precision);
        Assert.IsTrue(sheet.Bounds.Width * scale > EndProfiles.EndProfileSheet.PageWidth * 0.9 || sheet.Bounds.Height * scale > EndProfiles.EndProfileSheet.PageHeight * 0.9, "It fills the page.");
        Assert.AreEqual(HallSheet.PaperTextSize, sheet.TextSize * scale, HallSheet.PaperTextSize * 0.2, "Labels are about their paper size.");
    }

    [TestMethod]
    public void PrintedTitleAddressAndScaleBarAreBelowTheHall()
    {
        var sheet = HallSheet.Create(Hall() with { Address = "Station Road 1" });
        Assert.AreEqual("Hall", sheet.Title.Text);
        Assert.IsTrue(sheet.Title.Bounds.Max.Y < 0, "Under the walls.");
        Assert.IsTrue(sheet.Subtitle!.Center.Y < sheet.Title.Center.Y);
        Assert.IsTrue(sheet.ScaleBarStart.Y < 0);
        Assert.AreEqual(2000, sheet.ScaleBarLength, "A round length of about a fifth of the 12 m hall.");
        Assert.IsTrue(sheet.Bounds.Contains(sheet.Subtitle.Bounds.Min) && sheet.Bounds.Contains(sheet.ScaleBarStart + new Vector(sheet.ScaleBarLength, 0)));
    }

    [TestMethod]
    public void PrintedInformationIsCentredInTheHall()
    {
        var sheet = HallSheet.Create(Hall(), ["12 × 8 m", "Floor area: 96 m²", "Effective floor area: 95.9 m²"]);
        Assert.HasCount(3, sheet.Information);
        Assert.AreEqual(Width / 2, sheet.Information[1].Center.X, Precision);
        Assert.AreEqual(Depth / 2, sheet.Information[1].Center.Y, Precision, "The middle line is in the middle.");
        Assert.IsTrue(sheet.Information[0].Center.Y > sheet.Information[2].Center.Y, "Top to bottom.");
        Assert.IsTrue(sheet.Information.All(l => l.Bounds.Min.X > 0 && l.Bounds.Max.X < Width), "Inside the walls.");
    }

    [TestMethod]
    public void PrintedInformationShrinksToFitANarrowHall()
    {
        var narrow = Model.Halls.Hall.Rectangle("Corridor", 3000, 20000);
        var sheet = HallSheet.Create(narrow, ["Effective floor area: 59.4 m²"]);
        Assert.IsTrue(sheet.Information[0].Bounds.Width < 3000);
    }

    [TestMethod]
    public void NoInformationWhenNoneIsGiven() => Assert.IsEmpty(HallSheet.Create(Hall()).Information);
}
