using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class ModuleGeometryTests
{
    private static readonly EndProfile Single = TestProfiles.FremoSingle();
    private static readonly EndProfile Double = TestProfiles.Double(45);
    private static readonly CatalogueReference SingleRef = new(Single.Id, Single.Designation);

    private static EndProfile? Lookup(Guid id) => id == Single.Id ? Single : id == Double.Id ? Double : null;

    private static ModuleExit Exit(string label, Pose pose, EndProfile? profile = null, ProfileOrientation orientation = ProfileOrientation.Normal) => new()
    {
        Label = label,
        Pose = pose,
        EndProfile = profile is null ? SingleRef : new(profile.Id, profile.Designation),
        Orientation = orientation,
    };

    /// <summary>A 1000 mm straight module, 500 mm wide, drawn by hand.</summary>
    private static ModuleVariant Straight()
    {
        var west = Exit("W", new(Point.Origin, Angle.Half));
        var east = Exit("E", new(new(1000, 0), Angle.Zero));
        return new()
        {
            Exits = [west, east],
            MainPath = PathDrawing.Polyline([new(0, 0), new(1000, 0)]),
            Sides =
            [
                new() { FromExit = "W", ToExit = "E", Path = PathDrawing.Polyline([new(0, -250), new(1000, -250)]) },
                new() { FromExit = "E", ToExit = "W", Path = PathDrawing.Polyline([new(1000, 250), new(0, 250)]) },
            ],
        };
    }

    [TestMethod]
    public void EndPlateCornersAreSeenFromOutside()
    {
        // West exit faces −X; standing outside (west of it) looking east, the right corner is to the south.
        var plate = ModuleGeometry.EndPlate(Exit("W", new(Point.Origin, Angle.Half)), 500);
        Assert.IsTrue(plate.RightCorner.IsCloseTo(new(0, -250)));
        Assert.IsTrue(plate.LeftCorner.IsCloseTo(new(0, 250)));
        Assert.AreEqual(500, ModuleGeometry.EndPlate(Exit("W", Pose.Origin), Single).Width, Precision);
    }

    [TestMethod]
    public void StraightModuleOutline()
    {
        var outline = ModuleGeometry.Outline(Straight(), Lookup);
        Assert.IsNotNull(outline);
        Assert.IsTrue(outline.IsClosed);
        var polygon = outline.Tessellate();
        Assert.AreEqual(1000 * 500, polygon.SignedArea, Tolerance.Length);
        Assert.IsTrue(polygon.IsCounterClockwise);
        Assert.HasCount(4, outline.Segments);
    }

    [TestMethod]
    public void CurvedModuleOutlineHasConcentricSides()
    {
        // R 2000, 30° to the left; sides at R ∓ 250.
        var main = new PathBuilder(Pose.Origin).Curve(2000, 30).Build();
        var west = Exit("W", Pose.Origin.Reversed());
        var east = Exit("E", main.EndPose);
        var right = main.Offset(-250);
        var left = main.Offset(250).Reversed();
        var variant = new ModuleVariant
        {
            Exits = [west, east],
            MainPath = PathDrawing.From(main),
            Sides = [new() { FromExit = "W", ToExit = "E", Path = PathDrawing.From(right) }, new() { FromExit = "E", ToExit = "W", Path = PathDrawing.From(left) }],
        };
        var polygon = ModuleGeometry.Outline(variant, Lookup)!.Tessellate(0.01);
        var expectedArea = Math.PI / 12 * (2250 * 2250 - 1750 * 1750) / 2 * 2;
        Assert.AreEqual(expectedArea, polygon.SignedArea, expectedArea * 1e-4);
    }

    [TestMethod]
    public void EndOfLineModuleHasOneSide()
    {
        var exit = Exit("A", new(Point.Origin, Angle.Half));
        var variant = new ModuleVariant
        {
            Exits = [exit],
            Sides = [new() { FromExit = "A", ToExit = "A", Path = PathDrawing.Polyline([new(0, -250), new(1200, -250), new(1200, 250), new(0, 250)]) }],
        };
        Assert.AreEqual(ModuleKind.EndOfLine, variant.Kind);
        Assert.AreEqual(1200 * 500, ModuleGeometry.Outline(variant, Lookup)!.Tessellate().SignedArea, Tolerance.Length);
    }

    [TestMethod]
    public void SideOffTheCornerIsRejected()
    {
        var variant = Straight();
        variant = variant with { Sides = [variant.Sides[0] with { Path = PathDrawing.Polyline([new(0, -240), new(1000, -250)]) }, variant.Sides[1]] };
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => ModuleGeometry.Outline(variant, Lookup));
        Assert.Contains("right corner of exit W", exception.Message);
    }

    [TestMethod]
    public void TinyCornerGapIsBridged()
    {
        var variant = Straight();
        variant = variant with { Sides = [variant.Sides[0] with { Path = PathDrawing.Polyline([new(0, -250.005), new(1000, -250)]) }, variant.Sides[1]] };
        Assert.IsNotNull(ModuleGeometry.Outline(variant, Lookup));
    }

    [TestMethod]
    public void UnknownExitAndMissingProfileAreReported()
    {
        var variant = Straight();
        Assert.ThrowsExactly<InvalidOperationException>(() => ModuleGeometry.Outline(variant with { Sides = [variant.Sides[0] with { ToExit = "X" }, variant.Sides[1]] }, Lookup));
        Assert.ThrowsExactly<KeyNotFoundException>(() => ModuleGeometry.Outline(variant, _ => null));
    }

    [TestMethod]
    public void VariantWithoutSidesHasNoOutline() =>
        Assert.IsNull(ModuleGeometry.Outline(new ModuleVariant(), Lookup));

    [TestMethod]
    public void TrackPositionsFollowTheEndView()
    {
        // East exit facing +X: its left-hand side in plan (+Y) is the end view's right (+X).
        var exit = Exit("E", new(new(1000, 0), Angle.Zero), Double);
        var positions = ModuleGeometry.TrackPositions(exit, Double);
        Assert.HasCount(2, positions);
        Assert.IsTrue(positions[0].Position.IsCloseTo(new(1000, -22.5)));
        Assert.IsTrue(positions[1].Position.IsCloseTo(new(1000, 22.5)));
        Assert.IsTrue(positions[0].Heading.IsCloseTo(Angle.Zero));
    }

    [TestMethod]
    public void MirroredOrientationMirrorsTrackPositions()
    {
        var offCentre = Single with { Tracks = [new() { Offset = 20, Gauge = 16.5 }] };
        var normal = ModuleGeometry.TrackPositions(Exit("E", Pose.Origin, offCentre), offCentre);
        var mirrored = ModuleGeometry.TrackPositions(Exit("E", Pose.Origin, offCentre, ProfileOrientation.Mirrored), offCentre);
        Assert.AreEqual(20, normal[0].Position.Y, Precision);
        Assert.AreEqual(-20, mirrored[0].Position.Y, Precision);
    }

    [TestMethod]
    [DataRow(0, ModuleKind.Invalid)]
    [DataRow(1, ModuleKind.EndOfLine)]
    [DataRow(2, ModuleKind.PassThrough)]
    [DataRow(3, ModuleKind.Junction)]
    [DataRow(4, ModuleKind.Junction)]
    public void KindFollowsNumberOfExits(int exits, ModuleKind expected)
    {
        var variant = new ModuleVariant { Exits = [.. Enumerable.Range(0, exits).Select(i => Exit($"{i}", Pose.Origin))] };
        Assert.AreEqual(expected, variant.Kind);
    }

    [TestMethod]
    public void DefaultVariantAndExitLookup()
    {
        var first = Straight() with { Name = "Short" };
        var second = Straight() with { Name = "Long", IsDefault = true };
        var module = new Module { Id = Guid.NewGuid(), Name = "Test", Standard = new(Guid.NewGuid(), "FREMO/H0-RE"), Scale = "H0", Variants = [first, second] };
        Assert.AreEqual("Long", module.DefaultVariant.Name);
        Assert.AreEqual("Short", (module with { Variants = [first] }).DefaultVariant.Name);
        Assert.IsNotNull(first.Exit("E"));
        Assert.IsNull(first.Exit("X"));
    }
}
