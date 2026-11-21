using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class ProfileShadingTests
{
    private static ModuleExit Exit(ProfileOrientation orientation = ProfileOrientation.Normal) =>
        new() { Label = "A", EndProfile = new(Guid.NewGuid(), "P"), Orientation = orientation, Pose = new(Point.Origin, Angle.Half) };

    [TestMethod]
    public void ShortNameFallsBackToTheDesignation()
    {
        var profile = TestProfiles.FremoSingle().With(edited => { edited.Designation = "FREMO-H0-B96"; });
        Assert.AreEqual("FREMO-H0-B96", profile.ShortNameOrDesignation);
        Assert.AreEqual("FREMO-H0-B96", (profile.With(edited => { edited.ShortName = " "; })).ShortNameOrDesignation);
        Assert.AreEqual("B96", (profile.With(edited => { edited.ShortName = "B96"; })).ShortNameOrDesignation);
    }

    // A plate whose top is 40 mm below rail top is light over its whole width.
    [TestMethod]
    public void FlatProfileBelowRailTopIsOneLightInterval()
    {
        var intervals = ProfileShading.Intervals(TestProfiles.FremoSingle());
        Assert.HasCount(1, intervals);
        Assert.IsFalse(intervals[0].IsAboveRailTop);
        Assert.AreEqual(-250, intervals[0].From, Precision);
        Assert.AreEqual(250, intervals[0].To, Precision);
    }

    [TestMethod]
    public void GroundLineCrossingRailTopSplitsAtTheCrossing()
    {
        var profile = TestProfiles.FremoSingle().With(edited => { edited.GroundLine = PathDrawing.Polyline([new(-250, 100), new(-100, 100), new(-50, -50), new(250, -50)]); });
        var intervals = ProfileShading.Intervals(profile);
        Assert.HasCount(2, intervals);
        Assert.IsTrue(intervals[0].IsAboveRailTop);
        Assert.AreEqual(-250, intervals[0].From, Precision);
        // From 100 to −50 over 50 mm: rail top is crossed 2/3 of the way, at X = −66.67.
        Assert.AreEqual(-100 + 50 * 2.0 / 3, intervals[0].To, Precision);
        Assert.IsFalse(intervals[1].IsAboveRailTop);
        Assert.AreEqual(250, intervals[1].To, Precision);
    }

    [TestMethod]
    public void ProfileWithoutEdgeHasNoShading() =>
        Assert.IsEmpty(ProfileShading.Intervals(TestProfiles.FremoSingle().With(edited => { edited.GroundLine = null; edited.Outline = null; edited.Plate = null; })));

    // B96 has a mountain on one side: dark there, light at the track.
    [TestMethod]
    public void B96HasDarkAndLightParts()
    {
        var intervals = ProfileShading.Intervals(BuiltInCatalogue.Instance.FindEndProfile("FREMO-H0-B96", "H0")!);
        Assert.IsTrue(intervals.Any(i => i.IsAboveRailTop));
        Assert.IsTrue(intervals.Any(i => !i.IsAboveRailTop));
    }

    // The shading lies inside the module: the exit heads out along −X, so it is at X ≥ 0.
    // A plate top 40 mm below rail top folds to a light band 40 mm deep.
    [TestMethod]
    public void FlatProfileFoldsToABandAsDeepAsItsHeight()
    {
        var shade = ProfileShading.Shades(Exit(), TestProfiles.FremoSingle()).Single();
        Assert.IsFalse(shade.IsAboveRailTop);
        Assert.AreEqual(0, shade.Corners.Min(c => c.X), Precision);
        Assert.AreEqual(40, shade.Corners.Max(c => c.X), Precision);
        Assert.AreEqual(500, shade.Corners.Max(c => c.Y) - shade.Corners.Min(c => c.Y), Precision);
        Assert.AreEqual(40, ProfileShading.Depth(TestProfiles.FremoSingle()), Precision);
    }

    // The shading has the form of the profile: each edge point lands |y| inwards from the plate.
    [TestMethod]
    public void ShadingFollowsTheFormOfTheProfile()
    {
        var profile = TestProfiles.FremoSingle().With(edited => { edited.GroundLine = PathDrawing.Polyline([new(-250, 100), new(-150, 60), new(-100, 60), new(-50, -30), new(250, -30)]); });
        var shades = ProfileShading.Shades(Exit(), profile);
        var dark = shades.Single(s => s.IsAboveRailTop);
        // End view X maps to plan −Y at this exit (heading 180°, left = −Y).
        Assert.IsTrue(dark.Corners.Any(c => c.IsCloseTo(new(100, 250))));
        Assert.IsTrue(dark.Corners.Any(c => c.IsCloseTo(new(60, 150))));
        Assert.IsTrue(dark.Corners.Any(c => c.IsCloseTo(new(60, 100))));
        var light = shades.Single(s => !s.IsAboveRailTop);
        Assert.IsTrue(light.Corners.Any(c => c.IsCloseTo(new(30, -250))));
        Assert.AreEqual(100, ProfileShading.Depth(profile), Precision);
    }

    // A vertical step through rail top is split at rail top and keeps its step.
    [TestMethod]
    public void VerticalStepThroughRailTopIsKept()
    {
        var profile = TestProfiles.FremoSingle().With(edited => { edited.GroundLine = PathDrawing.Polyline([new(-250, 50), new(0, 50), new(0, -20), new(250, -20)]); });
        var intervals = ProfileShading.Intervals(profile);
        Assert.HasCount(2, intervals);
        Assert.AreEqual(0, intervals[0].To, Precision);
        Assert.AreEqual(0, intervals[1].From, Precision);
        var dark = ProfileShading.Shades(Exit(), profile).Single(s => s.IsAboveRailTop);
        Assert.IsTrue(dark.Corners.Any(c => c.IsCloseTo(new(50, 0))));
    }

    [TestMethod]
    public void MirroredExitMirrorsTheShades()
    {
        var profile = TestProfiles.FremoSingle().With(edited => { edited.GroundLine = PathDrawing.Polyline([new(-250, 100), new(0, 100), new(0, -50), new(250, -50)]); });
        var normal = ProfileShading.Shades(Exit(), profile).Single(s => s.IsAboveRailTop);
        var mirrored = ProfileShading.Shades(Exit(ProfileOrientation.Mirrored), profile).Single(s => s.IsAboveRailTop);
        Assert.AreEqual(-normal.Corners.Average(c => c.Y), mirrored.Corners.Average(c => c.Y), Precision);
    }
}
