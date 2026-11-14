using Tellurian.Trains.Drawing.Model.AutoDraw;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.HallPlans;
using Tellurian.Trains.Drawing.Model.Halls;
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

    private static readonly AutoDrawExit West = new() { Label = "W", Direction = ExitDirection.West, EndProfile = SingleTrack };
    private static readonly AutoDrawExit East = new() { Label = "E", Direction = ExitDirection.East, EndProfile = SingleTrack };

    /// <summary>A 12 × 8 m hall with two pillars, a door, an emergency exit and a no-go zone.</summary>
    public static Hall Club() => Hall.Rectangle("Club hall", Width, Depth) with
    {
        Address = "Station Road 1",
        MinimumAisleWidth = 1200,
        Obstacles =
        [
            Obstacle.RoundPillar(new(4000, 4000), 400) with { Label = "P1" },
            Obstacle.RectangularPillar(new(8000, 4000), 300, 500, new(15)),
            new() { Kind = ObstacleKind.Furniture, Area = PathDrawing.Rectangle(new(11000, 7000), 2000, 1000, Angle.Zero), Label = "Stage" },
        ],
        Doors =
        [
            new() { Position = new(new(1500, 0), new(90)), Width = 1000, Label = "Entrance" },
            new() { Kind = DoorKind.EmergencyExit, Position = new(new(Width, 4000), new(180)), Width = 1200, KeepFreeDepth = 1500 },
        ],
        NoGoZones = [new() { Area = PathDrawing.Rectangle(new(6000, 7800), 800, 400, Angle.Zero), Reason = "Fire hose cabinet" }],
    };

    /// <summary>
    /// A plan with a main line (straight, flipped curve, junction) anchored at the straight, a branch from the
    /// junction to a terminus, a joint override, a public area, a walking path and an acknowledgement.
    /// </summary>
    public static HallPlan Plan(DrawingCatalogue catalogue)
    {
        var straight = Draw(catalogue, new() { Name = "Straight", Standard = Fremo, Straight = 1200, Exits = [West, East] });
        var curve = Draw(catalogue, new() { Name = "Curve", Standard = Fremo, Radius = 2000, Angle = 30, Exits = [West, East] });
        var junction = Draw(catalogue, new() { Name = "Junction", Standard = Fremo, Straight = 1500, Exits = [West, East, East with { Label = "E2" }] });
        var terminus = Draw(catalogue, new() { Name = "Terminus", Standard = Fremo, Straight = 1200, NumberOfThroughTracks = 2, IsEndOfLine = true, Exits = [West with { EndProfile = DoubleTrack }] });
        Module[] modules = [straight, curve, junction, terminus];
        var hall = Club();
        var standard = catalogue.FindStandard(Fremo)!;

        return new()
        {
            Id = Guid.NewGuid(),
            Name = "Club meeting",
            Hall = hall,
            Scale = standard.Scale,
            PrimaryStandard = new(standard.Id, standard.ShortName),
            Layouts =
            [
                new()
                {
                    Name = "Main",
                    AnchorModuleId = straight.Id,
                    AnchorPose = new(new(2000, 2000), Angle.Zero),
                    IsAnchorLocked = true,
                    AnchorDimensions = new()
                    {
                        ReferencePoint = AnchorReferencePoint.PlateCorner("W", PlateSide.Right),
                        First = HallElementReference.Wall(0),
                        Second = HallElementReference.Obstacle(hall.Obstacles[0].Id),
                        AngleWall = 0,
                    },
                    Lines =
                    [
                        new()
                        {
                            Name = "Main line",
                            Entries =
                            [
                                Entry(straight),
                                Entry(curve).Flipped() with { LegAdjustment = 5, Note = "Bends the other way" },
                                Entry(junction, variantId: junction.DefaultVariant.Id),
                            ],
                        },
                        new()
                        {
                            Name = "Branch",
                            StartAt = new(junction.Id, "E2"),
                            Entries = [new() { ModuleId = terminus.Id, EntryExit = "W" }],
                        },
                    ],
                },
            ],
            Joints = [new() { First = new(straight.Id, "E"), Second = new(curve.Id, "E"), JoinMethodOverride = JoinMethod.Clamped, NumberOfClamps = 3, Note = "No screw-hole data" }],
            PublicAreas = [new() { Area = PathDrawing.Rectangle(new(6000, 6500), 4000, 1500, Angle.Zero), Label = "Visitors" }],
            WalkingPaths = [new() { CentreLine = PathDrawing.Polyline([new(1000, 1000), new(1000, 3500), new(5000, 3500)]), Width = 700, Label = "Drivers" }],
            Acknowledgements = [new() { FindingCode = "VA-11", ModuleIds = [junction.Id], Fingerprint = "sample", Note = "Operator space overlaps the stage edge", Time = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero) }],
            Modules = modules,
            EndProfiles = [.. modules.SelectMany(m => m.Variants).SelectMany(v => v.Exits).Select(e => e.EndProfile.Id).Distinct().Select(id => catalogue.FindEndProfile(id)!)],
            Standards = [standard],
        };
    }

    private static LineEntry Entry(Module module, Guid? variantId = null) =>
        new() { ModuleId = module.Id, VariantId = variantId, EntryExit = "W", ExitExit = "E" };

    private static Module Draw(DrawingCatalogue catalogue, AutoDrawParameters parameters) =>
        ModuleAutoDraw.Create(parameters, catalogue).Module ?? throw new InvalidOperationException($"{parameters.Name} couldn't be drawn.");
}
