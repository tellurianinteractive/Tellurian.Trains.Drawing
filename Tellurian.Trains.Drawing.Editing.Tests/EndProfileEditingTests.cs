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
    public void GroundLineIsHitNearItAboveTheOutline()
    {
        Assert.IsTrue(Scene.TryHitTest(Profile(), new(200, -10.5), Reach, out var item));
        Assert.AreEqual(EndProfileItem.GroundLine, item);
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
    public void OutlineAndGroundLineMoveAsAWhole()
    {
        var profile = Profile();
        var moved = EndProfileCommands.Move([EndProfileItem.Outline], new(0, -20)).Apply(profile);
        Assert.AreEqual(profile.Outline!.ToPath().Bounds.Min.Y - 20, moved.Outline!.ToPath().Bounds.Min.Y, Precision);
        Assert.AreSame(profile.GroundLine, moved.GroundLine);
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
}
