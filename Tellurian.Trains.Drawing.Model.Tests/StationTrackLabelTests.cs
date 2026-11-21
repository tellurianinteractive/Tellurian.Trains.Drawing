using Tellurian.Trains.Drawing.Model.Tracks;
using StationTrack = Tellurian.Trains.Schedules.Model.Layouts.StationTrack;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>A station track's designation is written at the middle of its longest element (decided 2026-11-21).</summary>
[TestClass]
public sealed class StationTrackLabelTests
{
    private static readonly TestStation Station = new();

    [TestMethod]
    public void EachStationTrackIsLabelledAtTheMiddleOfItsLongestElement()
    {
        var labels = StationTrackLabels.Of(Station.Module);

        CollectionAssert.AreEqual(new[] { "1", "2" }, labels.Select(l => l.Designation).ToArray());
        var between = Station.Between.Routes()[0];
        Assert.IsTrue(labels[0].Pose.Position.IsCloseTo(between.PoseAt(between.Length / 2).Position), "Track 1's longest element is the straight between the turnouts.");
        Assert.AreEqual(Station.LoopOffset, labels[1].Pose.Position.Y, Precision, "Track 2 is the loop.");
    }

    [TestMethod]
    public void ARenamedTrackIsLabelledWithItsNewDesignation()
    {
        var renamed = Station.Module.With(c => c.Tracks = [.. Station.Module.Tracks.Select(t => t.Id == TestStation.Track2 ? t.With(r => r.Designation = " 1a ") : t)]);

        Assert.AreEqual("1a", StationTrackLabels.Of(renamed).Single(l => l.TrackId == TestStation.Track2).Designation);
    }

    [TestMethod]
    public void ATrackWithoutElementsOrDesignationHasNoLabel()
    {
        var module = Station.Module.With(c => c.Tracks = [
            .. Station.Module.Tracks.Select(t => t.Id == TestStation.Track2 ? t.With(r => r.Designation = "") : t),
            new StationTrack { Id = 3, Designation = "3" },
        ]);

        CollectionAssert.AreEqual(new[] { TestStation.Track1 }, StationTrackLabels.Of(module).Select(l => l.TrackId).ToArray());
    }

    [TestMethod]
    public void PlainLineHasNoLabels() => Assert.IsEmpty(StationTrackLabels.Of(TestModules.Straight().With(c => c.Tracks = [])));
}
