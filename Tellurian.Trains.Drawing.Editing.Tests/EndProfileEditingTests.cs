using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Editing.EndProfiles;
using Tellurian.Trains.Drawing.Editing.Selection;
using Tellurian.Trains.Drawing.Editing.Snapping;
using Tellurian.Trains.Drawing.Editing.Tools;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class EndProfileEditingTests
{
    private const double Reach = 2;

    private static readonly EndProfileScene Scene = new();

    /// <summary>A 500 mm plate from 10 to 200 mm below rail top, two tracks 45 mm apart, two screw holes.</summary>
    private static EndProfile Profile() => EndProfileTemplate.Create(new()
    {
        Designation = "TEST-2",
        Scale = "H0",
        PlateWidth = 500,
        PlateTop = -10,
        PlateBottom = -200,
        TrackOffsets = EndProfileTemplate.CentredOffsets(2, 45),
        Gauge = 16.5,
        ScrewHoles = [new(-150, -100), new(150, -100)],
    });

    [TestMethod]
    public void SmallItemsAreHitBeforeTheOutline()
    {
        var profile = Profile();
        Assert.IsTrue(Scene.TryHitTest(profile, new(-150, -102), Reach, out var hole));
        Assert.AreEqual(EndProfileItem.ScrewHole(0), hole);
        Assert.IsTrue(Scene.TryHitTest(profile, new(22.5, 0), Reach, out var track));
        Assert.AreEqual(EndProfileItem.Track(1), track);
        Assert.IsTrue(Scene.TryHitTest(profile, new(0, -150), Reach, out var outline));
        Assert.AreEqual(EndProfileItem.Outline, outline);
        Assert.IsFalse(Scene.TryHitTest(profile, new(0, 100), Reach, out _));
    }

    [TestMethod]
    public void PlatePointsAreHitFirst()
    {
        Assert.IsTrue(Scene.TryHitTest(Profile(), new(249, -11), Reach, out var item));
        Assert.AreEqual(EndProfileItem.PlatePoint(1), item);
    }

    [TestMethod]
    public void TracksMoveOnlySideways()
    {
        var moved = EndProfileCommands.Move([EndProfileItem.Track(0), EndProfileItem.ScrewHole(1)], new(5, 7)).Apply(Profile());
        Assert.AreEqual(-17.5, moved.Tracks[0].Offset, Precision);
        Assert.AreEqual(22.5, moved.Tracks[1].Offset, Precision);
        Assert.AreEqual(new Point(155, -93), moved.ScrewHoles[1].Center);
        Assert.AreEqual(new Point(-150, -100), moved.ScrewHoles[0].Center);
    }

    [TestMethod]
    public void OutlineMovesTheWholePlate()
    {
        var profile = Profile();
        var moved = EndProfileCommands.Move([EndProfileItem.Outline], new(0, -20)).Apply(profile);
        Assert.AreEqual(profile.Outline!.ToPath().Bounds.Min.Y - 20, moved.Outline!.ToPath().Bounds.Min.Y, Precision);
        Assert.AreEqual(-30, moved.GroundLine!.ToPath().Start.Y, Precision, "The ground line is the plate's upper edge, so it moves with the plate.");
    }

    [TestMethod]
    public void AddedItemsTakeTheExistingSizes()
    {
        var profile = Profile() with { ScrewHoles = [new(new(0, -50), 10)] };
        var withHole = EndProfileCommands.AddScrewHole(new(100, -50)).Apply(profile);
        Assert.AreEqual(10, withHole.ScrewHoles[^1].Diameter);
        var withTrack = EndProfileCommands.AddTrack(90, new() { Offset = 0, Gauge = 9 }).Apply(profile);
        Assert.AreEqual(16.5, withTrack.Tracks[^1].Gauge, "The gauge of the existing tracks, not the default.");
        Assert.AreEqual(90, withTrack.Tracks[^1].Offset);
        var first = EndProfileCommands.AddTrack(0, new() { Offset = 0, Gauge = 9 }).Apply(profile with { Tracks = [] });
        Assert.AreEqual(9, first.Tracks.Single().Gauge);
    }

    [TestMethod]
    public void DeleteRemovesTheChosenItems()
    {
        var deleted = EndProfileCommands.Delete([EndProfileItem.Track(0), EndProfileItem.ScrewHole(1), EndProfileItem.GroundLine]).Apply(Profile());
        Assert.AreEqual(22.5, deleted.Tracks.Single().Offset, Precision);
        Assert.AreEqual(new Point(-150, -100), deleted.ScrewHoles.Single().Center);
        Assert.IsNull(deleted.GroundLine);
        Assert.IsNotNull(deleted.Outline);
    }

    [TestMethod]
    public void MirrorScrewHolesMakesThemSymmetric()
    {
        var profile = Profile() with { ScrewHoles = [new(new(-150, -100), 8), new(new(150, -100), 8), new(new(-80, -50), 8), new(new(0, -180), 8)] };
        var mirrored = EndProfileCommands.MirrorScrewHoles().Apply(profile);
        Assert.HasCount(5, mirrored.ScrewHoles);
        Assert.IsTrue(EndProfileAnalysis.Analyze(mirrored).IsSymmetric);
    }

    [TestMethod]
    public void DraggedScrewHoleSnapsToThePlateEdge()
    {
        var document = new EditorDocument<EndProfile>(Profile());
        var snap = new SnapEngine();
        snap.AddRange(Scene.AllSnapTargets(document.Current));
        var tool = new SelectMoveTool<EndProfile, EndProfileItem>(document, new SelectionSet<EndProfileItem>(), Scene, snap);

        tool.PointerDown(new(new(150, -100), Reach));
        tool.PointerMove(new(new(200, -100), Reach));
        tool.PointerMove(new(new(248.8, -120), Reach));
        tool.PointerUp(new(new(248.8, -120), Reach));

        Assert.AreEqual(new Point(250, -120), document.Current.ScrewHoles[1].Center, "The hole's centre snapped to the plate's right edge.");
    }

    [TestMethod]
    public void PlaceToolPutsTracksOnRailTop()
    {
        var document = new EditorDocument<EndProfile>(Profile());
        var tool = new PlaceTool<EndProfile>(document, new SnapEngine(), p => EndProfileCommands.AddTrack(p.X, new() { Offset = 0, Gauge = 16.5 }))
        {
            Constrain = p => new(p.X, 0),
        };
        tool.PointerMove(new(new(90, 30), Reach));
        Assert.AreEqual(new Point(90, 0), tool.Preview);
        tool.PointerDown(new(new(90, 30), Reach));
        Assert.AreEqual(90, document.Current.Tracks[^1].Offset);
    }

    [TestMethod]
    public void ItemsRoundTripAsText()
    {
        foreach (var item in new[] { EndProfileItem.Outline, EndProfileItem.Track(3), EndProfileItem.ScrewHole(12) })
        {
            Assert.IsTrue(EndProfileItem.TryParse(item.ToString(), out var parsed));
            Assert.AreEqual(item, parsed);
        }
        Assert.IsFalse(EndProfileItem.TryParse("Nonsense:1", out _));
    }

    [TestMethod]
    public void RubberBandFindsItemsByTheirBounds()
    {
        var selection = new SelectionSet<EndProfileItem>();
        var profile = Profile();
        selection.SelectInside(new(new(-200, -120), new(-100, -80)), Scene.Index(profile), id => Scene.BoundsOf(profile, id));
        Assert.AreEqual(EndProfileItem.ScrewHole(0), selection.Single);
    }

    [TestMethod]
    public void DraggingAPlatePointReshapesTheUpperEdgeAndTheSides()
    {
        var document = new EditorDocument<EndProfile>(Profile());
        var snap = new SnapEngine();
        snap.AddRange(Scene.AllSnapTargets(document.Current));
        var tool = new SelectMoveTool<EndProfile, EndProfileItem>(document, new SelectionSet<EndProfileItem>(), Scene, snap);

        // The right end of the upper edge, (250, -10), dragged up to (250, 20): the right side gets taller.
        tool.PointerDown(new(new(250, -10), Reach));
        tool.PointerMove(new(new(250, 5), Reach) { IsSnapSuppressed = true });
        tool.PointerMove(new(new(250, 20), Reach) { IsSnapSuppressed = true });
        tool.PointerUp(new(new(250, 20), Reach) { IsSnapSuppressed = true });

        var plate = PlateShape.From(document.Current)!;
        Assert.AreEqual(new Point(250, 20), plate.UpperEdge[1]);
        Assert.AreEqual(220, plate.RightHeight, Precision);
        Assert.AreEqual(190, plate.LeftHeight, Precision);
    }

    [TestMethod]
    public void PlatePointsCanBeAddedChangedAndDeleted()
    {
        var withPoint = EndProfileCommands.AddPlatePoint(0).Apply(Profile());
        var plate = PlateShape.From(withPoint)!;
        CollectionAssert.AreEqual(new Point[] { new(-250, -10), new(0, -10), new(250, -10) }, plate.UpperEdge.ToArray());

        var raised = EndProfileCommands.SetPlatePoint(1, new(0, -4)).Apply(withPoint);
        Assert.AreEqual(new Point(0, -4), PlateShape.From(raised)!.UpperEdge[1]);
        Assert.AreEqual(new Point(0, -4), raised.GroundLine!.ToPath().Segments[0].End, "The ground line follows the upper edge.");

        var deleted = EndProfileCommands.Delete([EndProfileItem.PlatePoint(1)]).Apply(raised);
        Assert.HasCount(2, PlateShape.From(deleted)!.UpperEdge);

        var tooFew = EndProfileCommands.Delete([EndProfileItem.PlatePoint(0)]).Apply(deleted);
        Assert.HasCount(2, PlateShape.From(tooFew)!.UpperEdge, "An upper edge keeps at least two points.");
    }

    /// <summary>The template plate with its upper edge as a line to the middle, a 20 mm fillet and a line up to the right.</summary>
    private static EndProfile FilletedProfile() =>
        EndProfileCommands.SetPlate(new(new(-250, -10), [PlateSegment.Line(new(0, -10)), PlateSegment.Fillet(20), PlateSegment.Line(new(250, 40))], -200)).Apply(Profile());

    [TestMethod]
    public void SegmentCanBecomeAnArcAndBeSplitOnTheArc()
    {
        var arc = EndProfileCommands.SetPlateSegment(0, PlateSegment.Arc(new(0, -510), new(250, -10))).Apply(Profile());
        var plate = PlateShape.From(arc)!;
        Assert.AreEqual(PlateSegmentKind.Arc, plate.Segments[0].Kind);
        Assert.IsInstanceOfType<ArcSegment>(arc.Outline!.ToPath().Segments.Single(s => s is ArcSegment));

        var split = PlateShape.From(EndProfileCommands.AddPlatePoint(0).Apply(arc))!;
        Assert.HasCount(2, split.Segments);
        Assert.IsTrue(split.Segments.All(s => s.Kind == PlateSegmentKind.Arc && s.Center == new Point(0, -510)), "Both halves keep the centre.");
        Assert.IsTrue(split.UpperEdge[1].IsCloseTo(new(0, plate.Edge.Bounds.Max.Y)), "The new point is on the arc's middle.");
    }

    [TestMethod]
    public void FilletEndIsWorkedOutAndCantBeMovedOrSplit()
    {
        var profile = FilletedProfile();
        var plate = PlateShape.From(profile)!;
        Assert.HasCount(4, plate.UpperEdge);
        Assert.AreSame(profile, EndProfileCommands.SetPlatePoint(2, new(10, 0)).Apply(profile), "A fillet's end can't be moved.");
        Assert.AreSame(profile, EndProfileCommands.AddPlatePoint(1).Apply(profile), "A fillet can't be split.");

        var withoutFillet = PlateShape.From(EndProfileCommands.Delete([EndProfileItem.PlatePoint(2)]).Apply(profile))!;
        Assert.IsTrue(withoutFillet.Segments.All(s => s.Kind == PlateSegmentKind.Line), "Deleting the fillet's end deletes the fillet.");
    }

    [TestMethod]
    public void MovingThePlateKeepsItsSegments()
    {
        var moved = PlateShape.From(EndProfileCommands.Move([EndProfileItem.Outline], new(5, 3)).Apply(FilletedProfile()))!;
        Assert.AreEqual(PlateSegmentKind.Fillet, moved.Segments[1].Kind);
        Assert.AreEqual(new Point(-245, -7), moved.Start);
        Assert.AreEqual(-197, moved.Bottom, Precision);
    }

    [TestMethod]
    public void DrawnOutlineReplacesTheEnteredPlate()
    {
        var drawn = EndProfileCommands.SetOutline(PathDrawing.Polyline([new(-100, -100), new(100, -100), new(100, 0), new(-100, 0)], isClosed: true)).Apply(FilletedProfile());
        Assert.IsNull(drawn.Plate);
        Assert.AreEqual(200, PlateShape.From(drawn)!.Width, Precision);
    }

    [TestMethod]
    public void BottomMustStayBelowTheUpperEdge()
    {
        var lower = EndProfileCommands.SetPlateBottom(-250).Apply(Profile());
        Assert.AreEqual(-250, PlateShape.From(lower)!.Bottom, Precision);
        var refused = EndProfileCommands.SetPlateBottom(0).Apply(lower);
        Assert.AreSame(lower, refused);
    }

    [TestMethod]
    public void DrawnUpperEdgeKeepsTheBottomAndIsSortedLeftToRight()
    {
        var drawn = EndProfileCommands.SetUpperEdge([new(250, -4), new(0, 0), new(-250, -4)], defaultDepth: 100).Apply(Profile());
        var plate = PlateShape.From(drawn)!;
        Assert.AreEqual(new Point(-250, -4), plate.UpperEdge[0]);
        Assert.AreEqual(-200, plate.Bottom, Precision, "The existing bottom is kept.");
    }
}
