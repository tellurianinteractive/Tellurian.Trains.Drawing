using Tellurian.Trains.Drawing.Model.Catalogues;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Schedules.Model.Layouts;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>A hall and a hall plan with every kind of element, for file format tests and the schema examples.</summary>
internal static class TestHalls
{
    public const double Width = 12000;
    public const double Depth = 8000;

    private const string Fremo = "FREMO/H0-RE";
    private const string SingleTrack = "FREMO-H0-B96";
    private const string DoubleTrack = "FREMO-H0-2E96";

    private static readonly AutoDrawExit West = new() { Label = "W", Direction = ExitSide.Left, EndProfile = SingleTrack };
    private static readonly AutoDrawExit East = new() { Label = "E", Direction = ExitSide.Right, EndProfile = SingleTrack };

    /// <summary>A 12 × 8 m hall with two pillars, a door, an emergency exit and a no-go zone.</summary>
    public static Hall Club() => Hall.Rectangle("Club hall", Width, Depth).With(hall =>
    {
        hall.Address = "Station Road 1";
        hall.MinimumAisleWidth = 1200;
        hall.Add(Obstacle.RoundPillar(new(4000, 4000), 400).With(o => o.Label = "P1"));
        hall.Add(Obstacle.RectangularPillar(new(8000, 4000), 300, 500, new(15)));
        hall.Add(new Obstacle { Kind = ObstacleKind.Furniture, Area = PathDrawing.Rectangle(new(11000, 7000), 2000, 1000, Angle.Zero), Label = "Stage" });
        hall.Add(new Door { Position = new(new(1500, 0), new(90)), Width = 1000, Label = "Entrance", IsModuleEntrance = true });
        hall.Add(new Door { Kind = DoorKind.EmergencyExit, Position = new(new(Width, 4000), new(180)), Width = 1200, KeepFreeDepth = 1500 });
        hall.Add(new NoGoZone { Area = PathDrawing.Rectangle(new(6000, 7800), 800, 400, Angle.Zero), Reason = "Fire hose cabinet" });
    });

    /// <summary>
    /// A plan with a main line (straight, turned curve, junction) anchored at the straight, a branch from the junction to
    /// a terminus, a joint override, a public area, a walking path and an acknowledgement. The modules are placed in the
    /// layout in that order, so their ids are 1 to 4.
    /// </summary>
    public static HallPlan Plan(Catalogue catalogue)
    {
        var straight = Draw(catalogue, new() { Name = "Straight", Standard = Fremo, Straight = 1200, Exits = [West, East] });
        var curve = Draw(catalogue, new() { Name = "Curve", Standard = Fremo, Radius = 2000, Angle = 30, Exits = [West, East] });
        var junction = Draw(catalogue, new() { Name = "Junction", Standard = Fremo, Straight = 1500, Exits = [West, East, East with { Label = "E2" }] });
        var terminus = Draw(catalogue, new() { Name = "Terminus", Standard = Fremo, Straight = 1200, NumberOfThroughTracks = 2, IsEndOfLine = true, Exits = [West with { EndProfile = DoubleTrack }] });
        Module[] modules = [straight, curve, junction, terminus];
        var hall = Club();
        var standard = catalogue.FindStandard(Fremo)!;

        var layout = new Layout { PreferredStandard = standard };
        layout.Include(standard);
        foreach (var id in modules.SelectMany(m => m.Drawing.Exits).Select(e => e.EndProfile.Id).Distinct()) layout.Include(catalogue.FindEndProfile(id)!);
        var placed = modules.Select(m => layout.Place(m)).ToArray();
        var main = layout.Add(new Line
        {
            Name = "Main line",
            Entries =
            [
                Entry(placed[0]),
                Entry(placed[1]).Flipped().With(e => { e.LegAdjustment = 5; e.Note = "Bends the other way"; }),
                Entry(placed[2]),
            ],
        });
        layout.Add(new Line { Name = "Branch", StartAt = new(placed[2].Id, ExitSide.Right, 2), Entries = [Entry(placed[3])] });
        layout.Joints.Add(new Joint { First = new(placed[0].Id, ExitSide.Right), Second = new(placed[1].Id, ExitSide.Right), JoinMethodOverride = JoinMethod.Clamped, NumberOfClamps = 3, Note = "No screw-hole data" });

        var plan = new HallPlan
        {
            Id = Guid.NewGuid(),
            Name = "Club meeting",
            Hall = hall,
            Layout = layout,
            Anchors =
            [
                new()
                {
                    LineId = main.Id,
                    ModuleId = placed[0].Id,
                    Pose = new(new(2000, 2000), Angle.Zero),
                    IsLocked = true,
                    Dimensions = new()
                    {
                        ReferencePoint = AnchorReferencePoint.PlateCorner(ExitPlace.Left, PlateSide.Right),
                        First = HallElementReference.Wall(0),
                        Second = HallElementReference.Obstacle(hall.Obstacles[0].Id),
                        AngleWall = 0,
                    },
                },
            ],
        };
        plan.Add(new PublicArea { Area = PathDrawing.Rectangle(new(6000, 6500), 4000, 1500, Angle.Zero), Label = "Visitors" });
        plan.Add(new WalkingPath { CentreLine = PathDrawing.Polyline([new(1000, 1000), new(1000, 3500), new(5000, 3500)]), Width = 700, Label = "Drivers" });
        plan.Add(new Acknowledgement { FindingCode = "VA-11", ModuleIds = [placed[2].Id], Fingerprint = "sample", Note = "Operator space overlaps the stage edge", Time = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero) });
        return plan;
    }

    private static LineEntry Entry(LayoutModule module) => new() { ModuleId = module.Id };

    private static Module Draw(Catalogue catalogue, AutoDrawParameters parameters) =>
        ModuleAutoDraw.Create(parameters, catalogue).Module ?? throw new InvalidOperationException($"{parameters.Name} couldn't be drawn.");
}
