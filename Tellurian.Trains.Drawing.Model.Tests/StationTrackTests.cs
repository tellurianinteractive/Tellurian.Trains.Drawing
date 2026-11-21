using static Tellurian.Trains.Schedules.Model.Layouts.LayoutPartCopying;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Modules;
using Tellurian.Trains.Drawing.Model.Tracks;
using StationTrack = Tellurian.Trains.Schedules.Model.Layouts.StationTrack;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>The list of tracks of a module and what it gets from the drawing (spec MD-8; tasks C1 to C5 and C7).</summary>
[TestClass]
public sealed class StationTrackTests
{
    private const double Clearance = 40;

    /// <summary>A fouling point is found to <see cref="StationTrackMeasurement.FoulingPointPrecision"/>, at each end of a stretch.</summary>
    private const double LengthPrecision = 4 * StationTrackMeasurement.FoulingPointPrecision;

    private const int Track3 = 3;
    private const int UnknownTrack = 99;
    private const double MillimetresPerMetre = 1000;
    private const double EnteredLength = 0.5;

    private static readonly TestStation Station = new();

    private static StationTrack Third(bool isScheduled = false) => new() { Id = Track3, Designation = "3", DisplayOrder = 3, IsScheduled = isScheduled };

    private static StationTrackProblemCode[] Codes(Module module) =>
        [.. StationTrackValidation.Validate(module, TestCatalogue.Lookup).Select(p => p.Code)];

    // The loop is free where it is 40 mm or more above the main track. Its curves are arcs, so the angle of the
    // part of each curve that is free follows from how far the straight part is from the main track.
    [TestMethod]
    public void ALoopEndsAtTheFoulingPointsNotAtThePoints()
    {
        var freeAngle = Math.Acos(1 - (Station.LoopOffset - Clearance) / TestStation.Radius);
        var expected = Station.LoopStraight + 2 * TestStation.Radius * freeAngle;

        var length = StationTrackMeasurement.UsableLength(Station.Drawing, TestStation.Track2, Clearance);

        Assert.AreEqual(expected, length, LengthPrecision);
        Assert.IsLessThan(Station.Loop.TrackLength, length);
    }

    // The main track is free where the loop's curve is 40 mm away: a point inside the circle of the curve is
    // (radius − its distance to the centre) from it.
    [TestMethod]
    public void TheMainTrackIsTheStretchBetweenTheFoulingPoints()
    {
        var curveStart = Station.FirstTurnout.Routes()[1].EndPose.Position;
        var centreX = curveStart.X + TestStation.Radius * Math.Sin(TestStation.TurnoutAngle * Math.PI / 180);
        var centreY = Station.LoopOffset - TestStation.Radius;
        var foulingPoint = centreX - Math.Sqrt(Math.Pow(TestStation.Radius - Clearance, 2) - centreY * centreY);
        var expected = TestStation.FirstPoints + TestStation.SecondPoints - 2 * foulingPoint;

        var length = StationTrackMeasurement.UsableLength(Station.Drawing, TestStation.Track1, Clearance);

        Assert.AreEqual(expected, length, LengthPrecision);
        Assert.IsLessThan(Station.Between.Length, length, "The fouling points are beyond the turnouts here.");
    }

    [TestMethod]
    public void TheTrackOutsideTheTurnoutsDoesNotAddToTheLength()
    {
        var withoutLeads = Station.Drawing with
        {
            TrackElements = [.. Station.Drawing.TrackElements.Select(t => t == Station.LeadIn || t == Station.LeadOut ? t with { StationTrackId = null } : t)],
        };
        Assert.AreEqual(
            StationTrackMeasurement.UsableLength(withoutLeads, TestStation.Track1, Clearance),
            StationTrackMeasurement.UsableLength(Station.Drawing, TestStation.Track1, Clearance),
            LengthPrecision);
    }

    [TestMethod]
    public void WithoutAFoulingDistanceTheLengthIsTheWholeTrack()
    {
        var mainLine = TestModules.Straight().Drawing.Exits[1].Pose.Position.X;
        Assert.AreEqual(mainLine, StationTrackMeasurement.UsableLength(Station.Drawing, TestStation.Track1, null), Precision);
        var loop = Station.Loop.TrackLength + Station.FirstTurnout.Routes()[1].Length + Station.SecondTurnout.Routes()[1].Length;
        Assert.AreEqual(loop, StationTrackMeasurement.UsableLength(Station.Drawing, TestStation.Track2, null), Precision);
    }

    [TestMethod]
    public void ATrackWithoutElementsHasNoLength() =>
        Assert.AreEqual(0, StationTrackMeasurement.UsableLength(Station.WithTrack(Third()).Drawing, Track3, Clearance));

    [TestMethod]
    public void TwoPlainTracksAtTheFoulingDistanceAreBothFree()
    {
        var drawing = TestModules.Straight().Drawing with
        {
            TrackElements =
            [
                new StraightTrack { Start = TestStation.At(0, 0), Length = 1200, StationTrackId = TestStation.Track1 },
                new StraightTrack { Start = TestStation.At(0, 0, Clearance), Length = 1000, StationTrackId = TestStation.Track2 },
            ],
        };
        Assert.AreEqual(1200, StationTrackMeasurement.UsableLength(drawing, TestStation.Track1, Clearance), Precision);
        Assert.AreEqual(1000, StationTrackMeasurement.UsableLength(drawing, TestStation.Track2, Clearance), Precision);
    }

    [TestMethod]
    public void TheFoulingDistanceComesFromTheStandardOrItsDoubleTrackEndProfile()
    {
        var standard = TestCatalogue.Instance.FindStandard(TestCatalogue.FremoId)!;
        Assert.AreEqual(45, StationTrackMeasurement.FoulingDistance(standard, TestCatalogue.Lookup)!.Value, Precision);
        Assert.AreEqual(Clearance, StationTrackMeasurement.FoulingDistance(standard with { FoulingDistance = Clearance }, TestCatalogue.Lookup));
        Assert.IsNull(StationTrackMeasurement.FoulingDistance(standard with { DefaultDoubleTrackEndProfileId = null }, TestCatalogue.Lookup));
    }

    [TestMethod]
    public void TracksJoinedAtBothEndsOrEndingAtExitsAreNotStubs()
    {
        Assert.IsFalse(StationTrackMeasurement.IsStub(Station.Drawing, TestStation.Track1, TestCatalogue.Lookup));
        Assert.IsFalse(StationTrackMeasurement.IsStub(Station.Drawing, TestStation.Track2, TestCatalogue.Lookup));
    }

    [TestMethod]
    public void ATrackThatJustEndsOrEndsAtABufferStopIsAStub()
    {
        // A siding from a third turnout on the main track, ending in the open.
        var turnout = new Turnout { Start = TestStation.At(500, 0), Kind = TurnoutKind.Right, Length = TestStation.TurnoutLength, Radius = TestStation.Radius, Angle = TestStation.TurnoutAngle, StationTrackId = TestStation.Track1 };
        var end = turnout.Routes()[1].EndPose;
        var siding = new StraightTrack { Start = end, Length = 200, StationTrackId = Track3 };
        var open = Station.WithTrack(Third(), turnout, siding).Drawing;
        Assert.IsTrue(StationTrackMeasurement.IsStub(open, Track3, TestCatalogue.Lookup));

        var withBuffer = open with { TrackElements = [.. open.TrackElements, new BufferStop { Pose = end.Advanced(200).Reversed() }] };
        Assert.IsTrue(StationTrackMeasurement.IsStub(withBuffer, Track3, TestCatalogue.Lookup));
        Assert.IsFalse(StationTrackMeasurement.IsStub(Station.WithTrack(Third()).Drawing, Track3, TestCatalogue.Lookup), "A track without elements has no ends.");
    }

    [TestMethod]
    public void MeasuredValuesAreWrittenInMetresUnlessEntered()
    {
        var entered = Station.Module.With(copy => { copy.Tracks = [.. Station.Module.Tracks.Select(t => t.Id == TestStation.Track2 ? t.With(c => { c.Length = EnteredLength; c.IsStub = true; }) : t)]; copy.Drawing = Station.Drawing with { TracksWithEnteredLength = [TestStation.Track2], TracksWithEnteredStub = [TestStation.Track2] }; });

        var measured = entered.WithMeasuredTracks(Clearance, TestCatalogue.Lookup);

        var main = measured.Tracks[0];
        Assert.AreEqual(StationTrackMeasurement.UsableLength(Station.Drawing, TestStation.Track1, Clearance) / MillimetresPerMetre, main.Length, 0.5 / MillimetresPerMetre);
        Assert.AreEqual(Math.Round(main.Length, 3), main.Length, "To the millimetre.");
        Assert.IsFalse(main.IsStub);
        Assert.AreEqual((EnteredLength, true), (measured.Tracks[1].Length, measured.Tracks[1].IsStub));
        Assert.AreSame(measured, measured.WithMeasuredTracks(Clearance, TestCatalogue.Lookup), "Nothing differs the second time.");
    }

    [TestMethod]
    public void WithoutTheMarkOfBeingEnteredTheValuesFollowTheDrawing()
    {
        var notEntered = Station.Module.With(copy => copy.Tracks = [.. Station.Module.Tracks.Select(t => t.Id == TestStation.Track2 ? t.With(c => { c.Length = EnteredLength; c.IsStub = true; }) : t)]);

        var loop = notEntered.WithMeasuredTracks(Clearance, TestCatalogue.Lookup).Tracks[1];

        Assert.AreEqual(StationTrackMeasurement.Metres(StationTrackMeasurement.UsableLength(Station.Drawing, TestStation.Track2, Clearance)), loop.Length);
        Assert.IsFalse(loop.IsStub);
    }

    [TestMethod]
    public void AModuleWithMeasuredTracksIsLeftAsItIs()
    {
        var module = TestModules.Straight();
        var standard = TestCatalogue.Instance.FindStandard(TestCatalogue.FremoId)!;
        Assert.AreSame(module, module.WithMeasuredTracks(StationTrackMeasurement.FoulingDistance(standard, TestCatalogue.Lookup), TestCatalogue.Lookup));
        Assert.IsEmpty(Codes(module));
    }

    [TestMethod]
    public void AModuleWithoutTracksIsLeftAsItIsAndReported()
    {
        var module = TestModules.Straight().With(c => c.Tracks = []);
        Assert.AreSame(module, module.WithMeasuredTracks(Clearance, TestCatalogue.Lookup));
        CollectionAssert.Contains(Codes(module), StationTrackProblemCode.NoTracks);
    }

    [TestMethod]
    public void TheStationHasNoProblems() => Assert.IsEmpty(Codes(Station.Module));

    [TestMethod]
    public void DesignationsAreUniqueWithinAModule()
    {
        var drawing = Station.WithTrack(Third().With(c => c.Designation = " 1 "), new StraightTrack { Start = TestStation.At(400, 0, -100), Length = 100, StationTrackId = Track3 });
        CollectionAssert.AreEqual(
            new[] { StationTrackProblemCode.DesignationNotUnique, StationTrackProblemCode.DesignationNotUnique },
            Codes(drawing));
        CollectionAssert.Contains(Codes(Station.WithTrack(Third().With(c => c.Designation = " "))), StationTrackProblemCode.DesignationMissing);
    }

    [TestMethod]
    public void ElementsNeedATrackOfTheListOnceThereIsAList()
    {
        var loose = new StraightTrack { Start = TestStation.At(400, 0, -100), Length = 100 };
        var unknown = new StraightTrack { Start = TestStation.At(400, 0, -200), Length = 100, StationTrackId = UnknownTrack };
        var problems = StationTrackValidation.Validate(Station.With(loose, unknown, new BufferStop { Pose = TestStation.At(400, 180, -100) }), TestCatalogue.Lookup);
        CollectionAssert.AreEqual(
            new[] { (StationTrackProblemCode.ElementWithoutTrack, (Guid?)loose.Id), (StationTrackProblemCode.UnknownTrack, (Guid?)unknown.Id) },
            problems.Select(p => (p.Code, p.ElementId)).ToArray());
    }

    [TestMethod]
    public void ATrackNeedsElements() =>
        CollectionAssert.AreEqual(new[] { StationTrackProblemCode.TrackWithoutElements }, Codes(Station.WithTrack(Third(isScheduled: true))));

    [TestMethod]
    public void AScheduledTrackMustReachAnExit()
    {
        var apart = new StraightTrack { Start = TestStation.At(400, 0, -100), Length = 100, StationTrackId = Track3 };
        CollectionAssert.AreEqual(new[] { StationTrackProblemCode.ScheduledTrackReachesNoExit }, Codes(Station.WithTrack(Third(isScheduled: true), apart)));
        Assert.IsEmpty(Codes(Station.WithTrack(Third(isScheduled: false), apart)), "A track that isn't scheduled may lie apart.");
    }

    [TestMethod]
    public void ATurnoutGivesEachRouteToTheTrackThatContinuesFromIt()
    {
        // Both turnouts refer to track 1; the loop still gets their diverging routes, so it reaches the exits.
        var loopOnly = Station.Module.With(c => { c.Tracks = [.. Station.Module.Tracks.Where(t => t.Id == TestStation.Track2)]; c.Drawing = Station.Drawing with
            {
                TrackElements = [.. Station.Drawing.TrackElements.Select(t => t.StationTrackId == TestStation.Track1 ? t with { StationTrackId = null } : t)],
            }; });
        CollectionAssert.DoesNotContain(Codes(loopOnly), StationTrackProblemCode.ScheduledTrackReachesNoExit);
    }

    [TestMethod]
    public void TheStationRoundTripsAndMatchesTheSchema()
    {
        var module = (Station.Module.With(c => c.Drawing = Station.Drawing with { TracksWithEnteredLength = [TestStation.Track2], TracksWithEnteredStub = [TestStation.Track1] }))
            .WithMeasuredTracks(Clearance, TestCatalogue.Lookup);
        var json = DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, TestCatalogue.Lookup));
        SchemaValidator.AssertValid(DrawingFileKind.Module, json);
        var read = DrawingFileSerializer.DeserializeModule(json).Module;
        CollectionAssert.AreEqual(module.Tracks.ToArray(), read.Tracks.ToArray());
        CollectionAssert.AreEqual(module.Drawing.TrackElements.Select(t => t.StationTrackId).ToArray(), read.Drawing.TrackElements.Select(t => t.StationTrackId).ToArray());
        CollectionAssert.AreEqual(new[] { TestStation.Track2 }, read.Drawing.TracksWithEnteredLength.ToArray());
        CollectionAssert.AreEqual(new[] { TestStation.Track1 }, read.Drawing.TracksWithEnteredStub.ToArray());
        StringAssert.Contains(json, "\"direction\": \"Both\"");
    }
}
