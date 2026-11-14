using Tellurian.Trains.Drawing.Model.AutoDraw;
using Tellurian.Trains.Drawing.Model.Modules;
using Tellurian.Trains.Drawing.Model.Tracks;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>Modules for file format tests, auto-drawn from <see cref="TestCatalogue"/>.</summary>
internal static class TestModules
{
    private const string Fremo = "FREMO/H0-RE";

    private static readonly AutoDrawExit West = new() { Label = "W", Direction = ExitDirection.West, EndProfile = "FREMO-H0-B96" };
    private static readonly AutoDrawExit East = new() { Label = "E", Direction = ExitDirection.East, EndProfile = "FREMO-H0-B96" };

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

    /// <summary>A module with one track element of every kind, owners, labels and a second variant.</summary>
    public static Module WithEverything()
    {
        var module = Straight();
        var variant = module.DefaultVariant;
        var start = new Pose(new(100, 0), Angle.Zero);
        Pose At(double x, double heading = 0) => new(new(x, 0), new(heading));
        var tracks = new TrackElement[]
        {
            new StraightTrack { Start = start, Length = 100, Designation = "1", IsMainTrack = true },
            new CurvedTrack { Start = At(200), Radius = 1000, Angle = -10 },
            new Turnout { Start = At(300), Kind = TurnoutKind.Left, Length = 230, Radius = 900, Angle = 12 },
            new Turnout { Start = At(600), Kind = TurnoutKind.CurvedRight, Length = 230, Radius = 1200, Angle = 15, InnerRadius = 800, InnerAngle = 20 },
            new Crossing { Center = At(900, 5), Length = 200, Angle = 15, IsNarrowGauge = true },
            new DoubleSlip { Center = At(1000), Length = 200, Angle = 15 },
            new BufferStop { Pose = At(1100, 180) },
        };
        var withEverything = variant with
        {
            Tracks = [.. variant.Tracks, .. tracks],
            Labels = [new() { Kind = LabelKind.StationName, Text = "Tellby", Pose = new(new(600, 100), Angle.Zero) }, new() { Kind = LabelKind.PlatformEdge, Pose = At(400), Length = 300 }],
            PublicSides = [new() { Attachment = new() { SideIndex = 1, From = 100, To = 500 } }],
            AutoDraw = null,
        };
        return module with
        {
            Owners = [new() { Name = "A. Owner", Share = 0.5, RegistryPersonId = 17 }, new() { Name = "Club", RegistryGroupId = 3 }],
            Theme = "Sweden 1970s",
            RepresentsFromYear = 1965,
            RepresentsUptoYear = 1980,
            Note = "Note with \"quotes\", åäö and ‰.",
            Revision = 4,
            Variants = [withEverything, variant with { Id = Guid.NewGuid(), Name = "Alternative", IsDefault = false }],
        };
    }

    private static Module Draw(AutoDrawParameters parameters) =>
        ModuleAutoDraw.Create(parameters, TestCatalogue.Instance).Module ?? throw new InvalidOperationException($"{parameters.Name} couldn't be drawn.");
}
