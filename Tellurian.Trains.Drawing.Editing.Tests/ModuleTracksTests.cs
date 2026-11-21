using static Tellurian.Trains.Schedules.Model.Layouts.LayoutPartCopying;
using TrackDirection = Tellurian.Trains.Schedules.Model.Layouts.TrackDirection;
using Tellurian.Trains.Drawing.Editing.Modules;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Modules;
using Tellurian.Trains.Drawing.Model.Tracks;
using StationTrack = Tellurian.Trains.Schedules.Model.Layouts.StationTrack;
using Station = Tellurian.Trains.Schedules.Model.Layouts.Station;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class ModuleTracksTests
{
    private const double Length = 2000;
    private const double MillimetresPerMetre = 1000;

    private const int Track1 = 1;
    private const int Track2 = 2;
    private const int UnknownTrack = 99;
    private const double EnteredLength = 1.5;

    private static readonly AutoDrawExit Left = new() { Label = "W", Direction = ExitSide.Left, EndProfile = "FREMO-H0-2E96" };

    private static DrawingCatalogue Catalogue => BuiltInCatalogue.Instance;

    private static ModuleStandard Standard => Catalogue.FindStandard("FREMO/H0-RE")!;

    private static ModuleTracks Tracks => new(Standard, Catalogue.FindEndProfile);

    /// <summary>
    /// A straight double-track module, as auto-draw makes it: two through tracks from exit to exit. Its list of
    /// tracks is emptied, so that the commands are tested from the start.
    /// </summary>
    private static ModuleFileContent DoubleTrack() => WithoutTrackList(AutoDrawn(new() { Name = "Double", Standard = Standard.ShortName, Straight = Length, NumberOfThroughTracks = 2, Exits = [Left, Right] }));

    private static readonly AutoDrawExit Right = Left with { Label = "E", Direction = ExitSide.Right };

    private static ModuleFileContent AutoDrawn(AutoDrawParameters parameters) =>
        ModuleFileContent.Create(ModuleAutoDraw.Create(parameters, Catalogue).Module!, Catalogue.FindEndProfile);

    /// <summary><paramref name="content"/> made an operation location, which gets the list of its generated tracks.</summary>
    private static ModuleFileContent AsStation(ModuleFileContent content) =>
        Tracks.Measured(content with { Module = content.Module.With(m => m.OperationLocation = new Station { Name = "Tellby", Signature = "Tby" }).WithGeneratedTrackList() });

    private static ModuleFileContent WithoutTrackList(ModuleFileContent content)
    {
        var drawing = content.Module.Drawing;
        var emptied = drawing.With(edited => edited.TrackElements = [.. drawing.TrackElements.Select(e => e.With(edited => edited.StationTrackId = null))]);
        return content with { Module = content.Module.With(c => { c.Tracks = []; c.Drawing = emptied; }) };
    }

    [TestMethod]
    public void AnAutoDrawnModuleOfPlainLineHasNoTrackListAndNeedsNone()
    {
        var module = AutoDrawn(new() { Name = "Double", Standard = Standard.ShortName, Straight = Length, NumberOfThroughTracks = 2, Exits = [Left, Right] }).Module;
        Assert.IsEmpty(module.Tracks);
        Assert.IsTrue(module.Drawing.TrackElements.All(e => e.StationTrackId is null));
        Assert.IsEmpty(StationTrackValidation.Validate(module, Catalogue.FindEndProfile));
        Assert.AreSame(module, module.WithGeneratedTrackList());
    }

    [TestMethod]
    public void AModuleMadeAnOperationLocationGetsItsThroughTracksInTheList()
    {
        var module = AsStation(AutoDrawn(new() { Name = "Double", Standard = Standard.ShortName, Straight = Length, NumberOfThroughTracks = 2, Exits = [Left, Right] })).Module;
        CollectionAssert.AreEqual(
            new[] { (1, "1", 1, true, true, false, Length / MillimetresPerMetre), (2, "2", 2, true, true, false, Length / MillimetresPerMetre) },
            module.Tracks.Select(t => (t.Id, t.Designation, t.DisplayOrder, t.IsMain, t.IsScheduled, t.IsStub, t.Length)).ToArray());
        CollectionAssert.AreEqual(module.Tracks.Select(t => (int?)t.Id).ToArray(), module.Drawing.TrackElements.Select(e => e.StationTrackId).ToArray());
        Assert.IsEmpty(StationTrackValidation.Validate(module, Catalogue.FindEndProfile));
    }

    [TestMethod]
    public void AnAutoDrawnTerminusHasTracksThatEnd()
    {
        var module = AsStation(AutoDrawn(new() { Name = "Terminus", Standard = Standard.ShortName, Straight = Length, NumberOfThroughTracks = 2, IsEndOfLine = true, Exits = [Left] })).Module;
        Assert.HasCount(2, module.Tracks);
        Assert.IsTrue(module.Tracks.All(t => t.IsStub && t.Length > 0), "Each track ends at a buffer stop.");
        Assert.IsTrue(module.Drawing.TrackElements.OfType<BufferStop>().All(b => b.StationTrackId is null));
    }

    [TestMethod]
    public void AnOperationLocationWithoutATrackInItsListIsReported()
    {
        var station = DoubleTrack().Module.With(m => m.OperationLocation = new Station { Name = "Tellby", Signature = "Tby" });
        CollectionAssert.AreEqual(new[] { StationTrackProblemCode.NoTracks }, StationTrackValidation.Validate(station, Catalogue.FindEndProfile).Select(p => p.Code).ToArray());
        Assert.IsEmpty(StationTrackValidation.Validate(DoubleTrack().Module, Catalogue.FindEndProfile), "Plain line needs no tracks.");
    }

    [TestMethod]
    public void AListIsMadeOnlyForADrawingWithoutOneWhoseTracksAreAllGenerated()
    {
        var emptied = DoubleTrack().Module.With(m => m.OperationLocation = new Station { Name = "Tellby", Signature = "Tby" });
        var generated = emptied.WithGeneratedTrackList();
        CollectionAssert.AreEqual(new[] { Track1, Track2 }, generated.Tracks.Select(t => t.Id).ToArray(), "An older drawing gets its list.");
        CollectionAssert.AreEqual(new int?[] { Track1, Track2 }, generated.Drawing.TrackElements.Select(e => e.StationTrackId).ToArray());
        var listed = WithTwoTracks().Module;
        Assert.AreSame(listed, listed.WithGeneratedTrackList(), "The user's list is not touched.");
        var drawnByHand = emptied.With(c => c.Drawing = emptied.Drawing.With(edited => edited.TrackElements = [.. emptied.Drawing.TrackElements.Select((e, i) => i == 0 ? e.With(edited => edited.IsGenerated = false) : e)]));
        Assert.AreSame(drawnByHand, drawnByHand.WithGeneratedTrackList(), "Tracks drawn by hand are put in the list by the user.");
    }

    [TestMethod]
    public void ARedrawnModuleWithMoreTracksGetsThemInItsList()
    {
        var single = AsStation(AutoDrawn(new() { Name = "Single", Standard = Standard.ShortName, Straight = Length, Exits = [Left with { EndProfile = "FREMO-H0-B96" }, Right with { EndProfile = "FREMO-H0-B96" }] }));
        var first = TracksOf(single).Single();
        single = Tracks.Set(first.Id, t => t.With(c => { c.Designation = "2"; c.Usage = "Main line"; })).Apply(single);
        var doubleTrack = Catalogue.FindEndProfile("FREMO-H0-2E96", "H0")!;

        var redrawn = ModuleEnds.SetEndProfile("W", doubleTrack, Catalogue).Apply(single);
        redrawn = Tracks.Measured(ModuleEnds.SetEndProfile("E", doubleTrack, Catalogue).Apply(redrawn));

        var tracks = TracksOf(redrawn);
        CollectionAssert.AreEqual(new[] { ("2", "Main line"), ("1", string.Empty) }, tracks.Select(t => (t.Designation, t.Usage)).ToArray(), "The new track gets the lowest free number.");
        CollectionAssert.AreEqual(new[] { first.Id, StationTrackList.FreeId([first]) }, tracks.Select(t => t.Id).ToArray(), "The new track gets the lowest free id.");
        CollectionAssert.AreEqual(tracks.Select(t => (int?)t.Id).ToArray(), Drawing(redrawn).TrackElements.Select(e => e.StationTrackId).ToArray());
        Assert.IsEmpty(StationTrackValidation.Validate(redrawn.Module, Catalogue.FindEndProfile));
    }

    private static ModuleDrawing Drawing(ModuleFileContent content) => content.Module.Drawing;

    private static IReadOnlyList<StationTrack> TracksOf(ModuleFileContent content) => [.. content.Module.Tracks];

    private static ModuleFileContent WithTwoTracks()
    {
        var content = Tracks.Add("1", Track1).Apply(DoubleTrack());
        content = Tracks.Add(" 2 ", Track2).Apply(content);
        content = Tracks.Assign(Drawing(content).TrackElements[0].Id, Track1).Apply(content);
        return Tracks.Assign(Drawing(content).TrackElements[1].Id, Track2).Apply(content);
    }

    [TestMethod]
    public void AddedTracksComeLastAndAreScheduled()
    {
        var tracks = TracksOf(WithTwoTracks());
        CollectionAssert.AreEqual(
            new[] { ("1", 1, true), ("2", 2, true) },
            tracks.Select(t => (t.Designation, t.DisplayOrder, t.IsScheduled)).ToArray());
    }

    [TestMethod]
    public void AnAssignedElementGivesItsTrackItsLength()
    {
        var content = Tracks.Add("1", Track1).Apply(DoubleTrack());
        Assert.AreEqual(0, TracksOf(content)[0].Length);

        content = Tracks.Assign(Drawing(content).TrackElements[0].Id, Track1).Apply(content);

        Assert.AreEqual(Length / MillimetresPerMetre, TracksOf(content)[0].Length);
        Assert.IsFalse(TracksOf(content)[0].IsStub, "It runs from exit to exit.");
    }

    [TestMethod]
    public void TwoThroughTracksAtTheStandardsDistanceAreBothFree()
    {
        var tracks = TracksOf(WithTwoTracks());
        Assert.AreEqual(Length / MillimetresPerMetre, tracks[0].Length);
        Assert.AreEqual(Length / MillimetresPerMetre, tracks[1].Length);
    }

    [TestMethod]
    public void AnEnteredLengthStaysUntilItFollowsTheDrawingAgain()
    {
        var content = Tracks.SetLength(Track1, EnteredLength).Apply(WithTwoTracks());
        Assert.AreEqual(EnteredLength, TracksOf(content)[0].Length);
        CollectionAssert.AreEqual(new[] { Track1 }, Drawing(content).TracksWithEnteredLength.ToArray());

        content = Tracks.Set(Track1, t => t.With(c => c.Usage = "Loading")).Apply(content);
        Assert.AreEqual(EnteredLength, TracksOf(content)[0].Length);

        content = Tracks.SetLength(Track1, null).Apply(content);
        Assert.AreEqual(Length / MillimetresPerMetre, TracksOf(content)[0].Length);
        Assert.IsEmpty(Drawing(content).TracksWithEnteredLength);
    }

    [TestMethod]
    public void AnEnteredStubMarkStaysUntilItFollowsTheDrawingAgain()
    {
        var content = Tracks.SetStub(Track1, true).Apply(WithTwoTracks());
        Assert.IsTrue(TracksOf(content)[0].IsStub);
        CollectionAssert.AreEqual(new[] { Track1 }, Drawing(content).TracksWithEnteredStub.ToArray());

        content = Tracks.SetStub(Track1, null).Apply(content);
        Assert.IsFalse(TracksOf(content)[0].IsStub);
        Assert.IsEmpty(Drawing(content).TracksWithEnteredStub);
    }

    [TestMethod]
    public void TheMarksOfBeingEnteredAreListsOfTrackIdsInOrder()
    {
        var content = Tracks.SetLength(Track2, EnteredLength).Apply(WithTwoTracks());
        content = Tracks.SetLength(Track1, EnteredLength).Apply(content);
        content = Tracks.SetLength(Track1, EnteredLength).Apply(content);
        CollectionAssert.AreEqual(new[] { Track1, Track2 }, Drawing(content).TracksWithEnteredLength.ToArray());

        var unknown = Tracks.SetStub(UnknownTrack, true).Apply(content);
        Assert.IsEmpty(Drawing(unknown).TracksWithEnteredStub, "Only a track of the list can be marked.");
    }

    [TestMethod]
    public void ARemovedTrackIsRemovedFromTheMarksOfBeingEntered()
    {
        var content = Tracks.SetLength(Track1, EnteredLength).Apply(WithTwoTracks());
        content = Tracks.SetStub(Track1, true).Apply(content);
        content = Tracks.SetStub(Track2, true).Apply(content);

        content = Tracks.Remove(Track1).Apply(content);

        Assert.IsEmpty(Drawing(content).TracksWithEnteredLength);
        CollectionAssert.AreEqual(new[] { Track2 }, Drawing(content).TracksWithEnteredStub.ToArray());
    }

    [TestMethod]
    public void ANewTrackGetsTheLowestIdThatNoTrackHas()
    {
        var content = Tracks.Remove(Track1).Apply(WithTwoTracks());
        content = Tracks.Add("3").Apply(content);
        content = Tracks.Add("4").Apply(content);
        CollectionAssert.AreEqual(new[] { (Track2, "2"), (1, "3"), (3, "4") }, TracksOf(content).Select(t => (t.Id, t.Designation)).ToArray());
    }

    [TestMethod]
    public void SetChangesOnlyTheNamedTrackAndKeepsItsId()
    {
        var content = Tracks.Set(Track2, t => t.With(c => { c.Id = UnknownTrack; c.Direction = TrackDirection.RightToLeft; c.SpeedLimit = 40; c.PlatformLength = 1.2; })).Apply(WithTwoTracks());
        var tracks = TracksOf(content);
        Assert.AreEqual((Track2, TrackDirection.RightToLeft, 40, 1.2), (tracks[1].Id, tracks[1].Direction, tracks[1].SpeedLimit, tracks[1].PlatformLength));
        Assert.AreEqual(TrackDirection.Both, tracks[0].Direction);
    }

    [TestMethod]
    public void RemovingATrackLeavesItsElementsInTheDrawing()
    {
        var before = WithTwoTracks();
        var content = Tracks.Remove(Track1).Apply(before);
        CollectionAssert.AreEqual(new[] { Track2 }, TracksOf(content).Select(t => t.Id).ToArray());
        Assert.HasCount(Drawing(before).TrackElements.Count, Drawing(content).TrackElements);
        CollectionAssert.AreEqual(new int?[] { null, Track2 }, Drawing(content).TrackElements.Select(e => e.StationTrackId).ToArray());
    }

    [TestMethod]
    public void AnElementCanOnlyBePartOfATrackOfTheList()
    {
        var before = WithTwoTracks();
        Assert.AreSame(before, Tracks.Assign(Drawing(before).TrackElements[0].Id, UnknownTrack).Apply(before));
        var content = Tracks.Assign(Drawing(before).TrackElements[0].Id, null).Apply(before);
        Assert.IsNull(Drawing(content).TrackElements[0].StationTrackId);
        Assert.AreEqual(0, TracksOf(content)[0].Length, "A track without elements has no length.");
    }

    [TestMethod]
    public void RedrawingTheModuleKeepsTheTracksOfItsElements()
    {
        var before = WithTwoTracks();
        var redrawn = ModuleEnds.FlipCurve(Catalogue).Apply(before);
        var curved = WithoutTrackList(AutoDrawn(new() { Name = "Curve", Standard = Standard.ShortName, Radius = 2000, Angle = 30, NumberOfThroughTracks = 2, Exits = [Left, Right] }));
        curved = Tracks.Add("1", Track1).Apply(curved);
        curved = Tracks.Assign(Drawing(curved).TrackElements[0].Id, Track1).Apply(curved);
        var lengthBefore = TracksOf(curved)[0].Length;

        var flipped = Tracks.Measured(ModuleEnds.FlipCurve(Catalogue).Apply(curved));

        Assert.AreEqual(Track1, Drawing(flipped).TrackElements[0].StationTrackId);
        Assert.AreNotEqual(lengthBefore, TracksOf(flipped)[0].Length, "The first track is now on the other side of the curve.");
        Assert.AreSame(before, redrawn, "A straight module has no curve to flip.");
    }
}
