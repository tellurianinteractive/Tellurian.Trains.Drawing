using static Tellurian.Trains.Schedules.Model.Layouts.LayoutPartCopying;
using Tellurian.Trains.Drawing.Model.Modules;
using Tellurian.Trains.Drawing.Model.Tracks;
using StationTrack = Tellurian.Trains.Schedules.Model.Layouts.StationTrack;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>
/// A station on a straight module of 1200 mm, drawn by hand: main track 1 along the centre line from exit to exit,
/// with a turnout near each end, and track 2 as a loop above it between the turnouts.
/// <code>
///            ____________ track 2 ____________
///           /                                 \
/// exit ----T1----------- track 1 -------------T2---- exit
///  0      100  330                      870  1100    1200
/// </code>
/// </summary>
internal sealed class TestStation
{
    public const double Radius = 900;
    public const double TurnoutAngle = 12;
    public const double TurnoutLength = 230;
    public const double FirstPoints = 100;
    public const double SecondPoints = 1100;

    public const int Track1 = 1;
    public const int Track2 = 2;

    public TestStation()
    {
        var module = TestModules.Straight();
        FirstTurnout = new() { Start = At(FirstPoints, 0), Kind = TurnoutKind.Left, Length = TurnoutLength, Radius = Radius, Angle = TurnoutAngle, StationTrackId = Track1 };
        SecondTurnout = new() { Start = At(SecondPoints, 180), Kind = TurnoutKind.Right, Length = TurnoutLength, Radius = Radius, Angle = TurnoutAngle, StationTrackId = Track1 };
        var diverging = FirstTurnout.Routes()[1].EndPose;
        LoopStraight = SecondTurnout.Routes()[1].End.X - diverging.Position.X - 2 * Radius * Math.Sin(TurnoutAngle * Math.PI / 180);
        Loop = new() { Path = PathDrawing.From(new PathBuilder(diverging).Curve(Radius, -TurnoutAngle).Straight(LoopStraight).Curve(Radius, -TurnoutAngle).Build()), StationTrackId = Track2 };
        LeadIn = new() { Start = At(0, 0), Length = FirstPoints, StationTrackId = Track1 };
        Between = new() { Start = At(FirstPoints + TurnoutLength, 0), Length = SecondPoints - FirstPoints - 2 * TurnoutLength, StationTrackId = Track1 };
        LeadOut = new() { Start = At(SecondPoints, 0), Length = 1200 - SecondPoints, StationTrackId = Track1 };
        Module = module.With(c => { c.Tracks = [
                new() { Id = Track1, Designation = "1", DisplayOrder = 1, IsMain = true, IsScheduled = true },
                new() { Id = Track2, Designation = "2", DisplayOrder = 2, IsScheduled = true },
            ]; c.Drawing = module.Drawing.With(edited => { edited.TrackElements = [LeadIn, FirstTurnout, Between, SecondTurnout, LeadOut, Loop]; edited.AutoDraw = null; }); });
    }

    public Module Module { get; }
    public ModuleDrawing Drawing => Module.Drawing;
    public StraightTrack LeadIn { get; }
    public Turnout FirstTurnout { get; }
    public StraightTrack Between { get; }
    public Turnout SecondTurnout { get; }
    public StraightTrack LeadOut { get; }
    public FlexTrack Loop { get; }

    /// <summary>The length of the straight part of the loop.</summary>
    public double LoopStraight { get; }

    /// <summary>How far the straight part of the loop is from the main track.</summary>
    public double LoopOffset => Loop.Path.ToPath().PoseAt(Loop.Path.ToPath().Length / 2).Position.Y;

    public static Pose At(double x, double heading, double y = 0) => new(new(x, y), new(heading));

    public Module With(params TrackElement[] added) => Module.With(c => c.Drawing = Drawing.With(edited => edited.TrackElements = [.. Drawing.TrackElements, .. added]));

    public Module WithTrack(StationTrack track, params TrackElement[] elements) =>
        With(elements).With(c => c.Tracks = [.. Module.Tracks, track]);
}
