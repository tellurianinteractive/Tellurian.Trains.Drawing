using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class ModuleSectionsTests
{
    private const double Length = 2000;
    private const double HalfWidth = 250;

    private static readonly PathDrawing Main = PathDrawing.Polyline([new(0, 0), new(Length, 0)]);
    private static readonly SegmentPath Outline = PathDrawing.Rectangle(Length, 2 * HalfWidth, new Transform(Angle.Zero, new(0, -HalfWidth))).ToPath();
    private static readonly ModuleVariant Variant = new() { MainPath = Main };

    private static InternalJoint Straight(double x) => new() { FirstSection = "", SecondSection = "", Start = new(x, -HalfWidth), End = new(x, HalfWidth) };

    [TestMethod]
    public void NoJointsIsOneSection()
    {
        var variant = ModuleSections.WithJoints(Variant, Outline, [])!;
        Assert.HasCount(1, variant.Sections);
        Assert.AreEqual(Length * 2 * HalfWidth, Math.Abs(variant.Sections[0].Outline.Tessellate().SignedArea), Precision);
    }

    [TestMethod]
    public void StraightJointsMakeSectionsOfTheirOwnLengths()
    {
        var variant = ModuleSections.WithJoints(Variant, Outline, [Straight(1200), Straight(500)])!;
        Assert.HasCount(3, variant.Sections);
        CollectionAssert.AreEqual(new[] { "1", "2", "3" }, variant.Sections.Select(s => s.Label).ToArray());
        Assert.AreEqual(500, ModuleSections.LengthAlongTrack(variant.Sections[0], Main), Precision);
        Assert.AreEqual(700, ModuleSections.LengthAlongTrack(variant.Sections[1], Main), Precision);
        Assert.AreEqual(800, ModuleSections.LengthAlongTrack(variant.Sections[2], Main), Precision);
        Assert.AreEqual(("2", "3"), (variant.InternalJoints[0].FirstSection, variant.InternalJoints[0].SecondSection), "The joint at 1200 m joins the second and third sections.");
        Assert.AreEqual(("1", "2"), (variant.InternalJoints[1].FirstSection, variant.InternalJoints[1].SecondSection));
    }

    [TestMethod]
    public void BentJointCutsAlongItsPolyline()
    {
        var joint = Straight(1000) with { Bends = [new(1000, -50), new(1200, -50), new(1200, 50), new(1000, 50)] };
        var variant = ModuleSections.WithJoints(Variant, Outline, [joint])!;
        var first = Math.Abs(variant.Sections[0].Outline.Tessellate().SignedArea);
        var second = Math.Abs(variant.Sections[1].Outline.Tessellate().SignedArea);
        Assert.AreEqual(1000 * 2 * HalfWidth + 200 * 100, first, Precision, "The tongue belongs to the first section.");
        Assert.AreEqual(Length * 2 * HalfWidth, first + second, Precision);
    }

    [TestMethod]
    public void JointsMustCutTheModule()
    {
        Assert.IsNull(ModuleSections.WithJoints(Variant, Outline, [Straight(1000) with { End = new(1000, 100) }]), "An end inside the module.");
        Assert.IsNull(ModuleSections.WithJoints(Variant, Outline, [Straight(1000) with { Bends = [new(1000, 900)] }]), "A bend outside the module.");
        var crossing = new InternalJoint { FirstSection = "", SecondSection = "", Start = new(500, -HalfWidth), End = new(1500, HalfWidth) };
        Assert.IsNull(ModuleSections.WithJoints(Variant, Outline, [Straight(1000), crossing]), "Joints crossing each other.");
    }

    [TestMethod]
    public void NewJointIsAcrossTheTrack()
    {
        var joint = ModuleSections.Across(Outline, Main, new(800, 120))!;
        Assert.IsTrue(joint.Start.IsCloseTo(new(800, -HalfWidth), Precision));
        Assert.IsTrue(joint.End.IsCloseTo(new(800, HalfWidth), Precision));
        Assert.AreEqual(800, ModuleSections.DistanceAlongTrack(joint, Main), Precision);
    }

    [TestMethod]
    public void JointAcrossACurvedModuleIsRadial()
    {
        // A quarter circle of radius 2000 about the origin, 500 wide.
        var main = new PathDrawing { Start = new(2000, 0), Steps = [new ArcStep(Point.Origin, 90)] };
        var outline = new SegmentPath(
        [
            new ArcSegment(Point.Origin, 1750, Angle.Zero, 90), new LineSegment(new(0, 1750), new(0, 2250)),
            new ArcSegment(Point.Origin, 2250, Angle.Quarter, -90), new LineSegment(new(2250, 0), new(1750, 0)),
        ], isClosed: true);
        var middle = main.ToPath().PoseAt(main.ToPath().Length / 2).Position;
        var joint = ModuleSections.Across(outline, main, middle)!;
        // From the right of the track to its left: on a left-hand curve, from the outer edge to the inner one.
        Assert.AreEqual(2250, joint.Start.DistanceTo(Point.Origin), ModuleSections.EdgeTolerance);
        Assert.AreEqual(1750, joint.End.DistanceTo(Point.Origin), ModuleSections.EdgeTolerance);
        Assert.AreEqual(45, (joint.Start - joint.End).Direction.Degrees, ArcAngleRounding, "Along the radius.");
        Assert.HasCount(2, ModuleSections.WithJoints(new ModuleVariant { MainPath = main }, outline, [joint])!.Sections);
    }

    [TestMethod]
    public void JointAtADistanceAlongTheTrack()
    {
        var joint = ModuleSections.AcrossAt(Outline, Main, 1234)!;
        Assert.AreEqual(1234, joint.Start.X, Precision);
        Assert.AreEqual(1234, joint.End.X, Precision);
    }

    [TestMethod]
    public void CentroidOfARectangle() =>
        Assert.IsTrue(Outline.Tessellate().Centroid.IsCloseTo(new(Length / 2, 0), Precision));
}
