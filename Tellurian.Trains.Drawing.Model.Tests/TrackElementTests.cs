using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.Tracks;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class TrackElementTests
{
    private const double Precision = 1e-9;

    private static void AssertPose(Pose2 expected, Pose2 actual) =>
        Assert.IsTrue(expected.IsCloseTo(actual, 1e-9, 1e-9), $"Expected {expected}, was {actual}.");

    [TestMethod]
    public void StraightTrack()
    {
        var track = new StraightTrack { Start = new(new(100, 0), new Angle(90)), Length = 300 };
        Assert.AreEqual(300, track.TrackLength, Precision);
        var points = track.ConnectionPoints();
        AssertPose(new(new(100, 0), new Angle(270)), points[0]);
        AssertPose(new(new(100, 300), new Angle(90)), points[1]);
    }

    [TestMethod]
    public void CurvedTrack()
    {
        var track = new CurvedTrack { Start = Pose2.Origin, Radius = 1000, Angle = -90 };
        Assert.AreEqual(1000 * Math.PI / 2, track.TrackLength, Precision);
        AssertPose(new(new(1000, -1000), new Angle(-90)), track.ConnectionPoints()[1]);
    }

    [TestMethod]
    public void FlexTrackFollowsItsPath()
    {
        var path = new PathBuilder(Pose2.Origin).Straight(100).Curve(500, 20).Build();
        var track = new FlexTrack { Path = PathDrawing.From(path) };
        AssertPose(Pose2.Origin.Reversed(), track.ConnectionPoints()[0]);
        AssertPose(path.EndPose, track.ConnectionPoints()[1]);
        Assert.AreEqual(path.Length, track.TrackLength, Precision);
    }

    [TestMethod]
    public void LeftTurnoutDivergingRouteEndsLevelWithStraight()
    {
        var turnout = new Turnout { Start = Pose2.Origin, Kind = TurnoutKind.Left, Length = 230, Radius = 900, Angle = 12 };
        var routes = turnout.Routes();
        Assert.HasCount(2, routes);
        Assert.AreEqual(230, routes[0].End.X, Precision);
        Assert.AreEqual(230, routes[1].End.X, 1e-9);
        Assert.IsGreaterThan(0, routes[1].End.Y);
        Assert.AreEqual(12, routes[1].EndPose.Heading.Degrees, 1e-9);
        Assert.HasCount(3, turnout.ConnectionPoints());
        AssertPose(Pose2.Origin.Reversed(), turnout.ConnectionPoints()[0]);
    }

    [TestMethod]
    public void RightTurnoutMirrorsLeft()
    {
        var left = new Turnout { Start = Pose2.Origin, Kind = TurnoutKind.Left, Length = 230, Radius = 900, Angle = 12 };
        var right = left with { Kind = TurnoutKind.Right };
        var l = left.Routes()[1].End;
        var r = right.Routes()[1].End;
        Assert.AreEqual(l.X, r.X, Precision);
        Assert.AreEqual(-l.Y, r.Y, Precision);
    }

    [TestMethod]
    public void ShortTurnoutHasNoStraightAfterTheArc()
    {
        // The arc alone reaches beyond Length, so no extension is added.
        var turnout = new Turnout { Start = Pose2.Origin, Kind = TurnoutKind.Left, Length = 100, Radius = 900, Angle = 12 };
        Assert.HasCount(1, turnout.Routes()[1].Segments);
    }

    [TestMethod]
    public void WyeSplitsTheAngle()
    {
        var wye = new Turnout { Start = Pose2.Origin, Kind = TurnoutKind.Wye, Length = 200, Radius = 1200, Angle = 15 };
        var routes = wye.Routes();
        Assert.AreEqual(7.5, routes[0].EndPose.Heading.Signed.Degrees, 1e-9);
        Assert.AreEqual(-7.5, routes[1].EndPose.Heading.Signed.Degrees, 1e-9);
        Assert.AreEqual(routes[0].End.Y, -routes[1].End.Y, Precision);
    }

    [TestMethod]
    public void ThreeWayHasThreeRoutes()
    {
        var turnout = new Turnout { Start = Pose2.Origin, Kind = TurnoutKind.ThreeWay, Length = 250, Radius = 900, Angle = 12 };
        Assert.HasCount(3, turnout.Routes());
        Assert.HasCount(4, turnout.ConnectionPoints());
    }

    [TestMethod]
    [DataRow(TurnoutKind.CurvedLeft, 1)]
    [DataRow(TurnoutKind.CurvedRight, -1)]
    public void CurvedTurnoutRoutesCurveTheSameWay(TurnoutKind kind, int sign)
    {
        var turnout = new Turnout { Start = Pose2.Origin, Kind = kind, Length = 0, Radius = 2000, Angle = 10, InnerRadius = 1200, InnerAngle = 15 };
        var routes = turnout.Routes();
        Assert.AreEqual(sign * 10, routes[0].EndPose.Heading.Signed.Degrees, 1e-9);
        Assert.AreEqual(sign * 15, routes[1].EndPose.Heading.Signed.Degrees, 1e-9);
    }

    [TestMethod]
    public void CurvedTurnoutNeedsInnerRoute() =>
        Assert.ThrowsExactly<InvalidOperationException>(() => new Turnout { Start = Pose2.Origin, Kind = TurnoutKind.CurvedLeft, Length = 0, Radius = 2000, Angle = 10 }.Routes());

    [TestMethod]
    public void CrossingRoutesMeetAtTheCentre()
    {
        var crossing = new Crossing { Center = new(new(500, 100), Angle.Zero), Length = 200, Angle = 30 };
        var routes = crossing.Routes();
        Assert.IsTrue(routes[0].PoseAt(100).Position.IsCloseTo(new(500, 100)));
        Assert.IsTrue(routes[1].PoseAt(100).Position.IsCloseTo(new(500, 100)));
        var points = crossing.ConnectionPoints();
        Assert.HasCount(4, points);
        AssertPose(new(new(400, 100), Angle.Half), points[0]);
        AssertPose(new(new(600, 100), Angle.Zero), points[1]);
        AssertPose(new(new Point2(500, 100) + Vector2.FromAngle(new Angle(30), 100), new Angle(30)), points[3]);
    }

    [TestMethod]
    public void DoubleSlipRoutesAreTangentToBothTracks()
    {
        var slip = new DoubleSlip { Center = Pose2.Origin, Length = 200, Angle = 15 };
        var routes = slip.Routes();
        Assert.HasCount(4, routes);
        var points = slip.ConnectionPoints();
        // Slip 1 runs from the start of track 1 to the end of track 2, slip 2 from the start of track 2 to the end of track 1.
        AssertPose(points[3], routes[2].EndPose);
        AssertPose(points[1], routes[3].EndPose);
        Assert.AreEqual(100 / Math.Tan(7.5 * Math.PI / 180), slip.SlipRadius, Precision);
    }

    [TestMethod]
    public void BufferStopHasOneConnectionAndNoTrack()
    {
        var stop = new BufferStop { Pose = new(new(1150, 0), Angle.Half) };
        Assert.IsEmpty(stop.Routes());
        Assert.HasCount(1, stop.ConnectionPoints());
        Assert.AreEqual(0, stop.TrackLength);
    }

    [TestMethod]
    public void ElementsGetUniqueIds() =>
        Assert.AreNotEqual(new BufferStop { Pose = Pose2.Origin }.Id, new BufferStop { Pose = Pose2.Origin }.Id);
}
