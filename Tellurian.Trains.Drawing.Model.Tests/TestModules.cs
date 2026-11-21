using static Tellurian.Trains.Schedules.Model.Layouts.LayoutPartCopying;
using OperationLocationKind = Tellurian.Trains.Schedules.Model.Layouts.OperationLocationKind;
using TrackDirection = Tellurian.Trains.Schedules.Model.Layouts.TrackDirection;
using Tellurian.Trains.Drawing.Model.AutoDraw;
using Tellurian.Trains.Drawing.Model.Modules;
using Tellurian.Trains.Drawing.Model.Tracks;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>Modules for file format tests, auto-drawn from <see cref="TestCatalogue"/>.</summary>
internal static class TestModules
{
    private const string Fremo = "FREMO/H0-RE";

    private static readonly AutoDrawExit West = new() { Label = "W", Direction = ExitDirection.Left, EndProfile = "FREMO-H0-B96" };
    private static readonly AutoDrawExit East = new() { Label = "E", Direction = ExitDirection.Right, EndProfile = "FREMO-H0-B96" };

    public static Module Straight() => Draw(new() { Name = "Straight", Standard = Fremo, Straight = 1200, Exits = [West, East] });

    public static Module Curve() => Draw(new() { Name = "Curve", Standard = Fremo, Radius = 2000, Angle = 30, Exits = [West, East] });

    public static Module Terminus() => Draw(new() { Name = "Terminus", Standard = Fremo, Straight = 1200, NumberOfThroughTracks = 2, IsEndOfLine = true, Exits = [West with { EndProfile = "FREMO-H0-2E96" }] });

    public static Module Station() => Draw(new() { Name = "Station", Standard = Fremo, Straight = 3000, Width = 600, NumberOfSections = 3, IsOperationPlace = true, IsDuckunder = true, Exits = [West, East] });

    public static Module Junction() => Draw(new() { Name = "Junction", Standard = Fremo, Straight = 1500, Exits = [West, East, East with { Label = "E2" }] });

    public static Module Transition() => Draw(new()
    {
        Name = "Transition",
        Standard = Fremo,
        Straight = 1000,
        NumberOfThroughTracks = 2,
        Exits = [West with { EndProfile = "FREMO-H0-2E96" }, East with { EndProfile = "TEST-2-55" }],
    });

    /// <summary>A module with one track element of every kind, owners, labels and an operation location.</summary>
    public static Module WithEverything()
    {
        var module = Straight();
        var drawing = module.Drawing;
        var start = new Pose(new(100, 0), Angle.Zero);
        Pose At(double x, double heading = 0) => new(new(x, 0), new(heading));
        var tracks = new TrackElement[]
        {
            new StraightTrack { Start = start, Length = 100, StationTrackId = TestStation.Track1 },
            new CurvedTrack { Start = At(200), Radius = 1000, Angle = -10 },
            new Turnout { Start = At(300), Kind = TurnoutKind.Left, Length = 230, Radius = 900, Angle = 12 },
            new Turnout { Start = At(600), Kind = TurnoutKind.CurvedRight, Length = 230, Radius = 1200, Angle = 15, InnerRadius = 800, InnerAngle = 20 },
            new Crossing { Center = At(900, 5), Length = 200, Angle = 15, IsNarrowGauge = true },
            new DoubleSlip { Center = At(1000), Length = 200, Angle = 15 },
            new BufferStop { Pose = At(1100, 180) },
        };
        var withEverything = drawing with
        {
            TrackElements = [.. drawing.TrackElements, .. tracks],
            TracksWithEnteredLength = [TestStation.Track1],
            Labels = [new() { Kind = LabelKind.StationName, Text = "Tellby", Pose = new(new(600, 100), Angle.Zero) }, new() { Kind = LabelKind.PlatformEdge, Pose = At(400), Length = 300 }],
            PublicSides = [new() { Attachment = new() { SideIndex = 1, From = 100, To = 500 } }],
            AutoDraw = null,
        };
        return module.With(c =>
        {
            c.Number = 12;
            c.Prefix = "AOw";
            c.ModuleOwners = "A. Owner, Club";
            c.PackageLabel = "Box 1";
            c.Tracks = [new() { Id = TestStation.Track1, Designation = "1", DisplayOrder = 1, IsMain = true, IsScheduled = true, Length = 0.1, PlatformLength = 0.3, Direction = TrackDirection.LeftToRight, SpeedLimit = 40, Usage = "Passenger trains" }];
            c.ExternalReferences = TestReferences.Registry("""{ "stationId": 1234, "moduleId": 5678 }""");
            c.Theme = "Sweden 1970s";
            c.RepresentsFromYear = 1965;
            c.RepresentsUptoYear = 1980;
            c.Note = "Note with \"quotes\", åäö and ‰.";
            c.Revision = 4;
            c.OperationLocation = new Tellurian.Trains.Schedules.Model.Layouts.Station
            {
                Name = "Tellby",
                Signature = "Tby",
                CountryCode = "SE",
                HasPassengerExchange = true,
                HasCargoExchange = true,
                IsChangingTrainDirectionPossible = true,
                IsKeyRequired = true,
                Centre = 0.6,
            };
            c.Drawing = withEverything;
        });
    }

    private static Module Draw(AutoDrawParameters parameters) =>
        ModuleAutoDraw.Create(parameters, TestCatalogue.Instance).Module ?? throw new InvalidOperationException($"{parameters.Name} couldn't be drawn.");
}
