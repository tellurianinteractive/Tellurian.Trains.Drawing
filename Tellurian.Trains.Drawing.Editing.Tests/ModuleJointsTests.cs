using Tellurian.Trains.Drawing.Editing.Modules;
using Tellurian.Trains.Drawing.Model.AutoDraw;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class ModuleJointsTests
{
    private const double Length = 2000;
    private const double Reach = 20;

    private static readonly AutoDrawExit West = new() { Label = "W", Direction = ExitDirection.Left, EndProfile = "FREMO-H0-B96" };
    private static readonly AutoDrawExit East = new() { Label = "E", Direction = ExitDirection.Right, EndProfile = "FREMO-H0-B96" };

    private static ModuleFileContent Straight()
    {
        var catalogue = BuiltInCatalogue.Instance;
        var module = ModuleAutoDraw.Create(new() { Name = "Straight", Standard = "FREMO/H0-RE", Straight = Length, Exits = [West, East] }, catalogue).Module!;
        return ModuleFileContent.Create(module, catalogue.FindEndProfile);
    }

    private static ModuleDrawing Drawing(ModuleFileContent content) => content.Module.Drawing;

    private static Point OnTrack(ModuleFileContent content, double distance) => Drawing(content).MainPath!.ToPath().PoseAt(distance).Position;

    private static double[] SectionLengths(ModuleFileContent content) =>
        [.. Drawing(content).Sections.Select(s => Math.Round(ModuleSections.LengthAlongTrack(s, Drawing(content).MainPath!)))];

    private static ModuleFileContent WithJointAt(double distance) => ModuleJoints.Add(OnTrack(Straight(), distance)).Apply(Straight());

    [TestMethod]
    public void AStraightModuleHasAnOutlineToDivide() => Assert.IsNotNull(ModuleJoints.Outline(Straight()));

    [TestMethod]
    public void AddedJointDividesTheModuleWhereItIsClicked()
    {
        var content = WithJointAt(800);
        Assert.HasCount(1, Drawing(content).InternalJoints);
        CollectionAssert.AreEqual(new[] { 800.0, 1200.0 }, SectionLengths(content));
        Assert.AreEqual(800, ModuleSections.DistanceAlongTrack(Drawing(content).InternalJoints[0], Drawing(content).MainPath!), Precision);
    }

    [TestMethod]
    public void TypedDistanceMovesTheJoint()
    {
        var content = ModuleJoints.SetDistance(0, 1250).Apply(WithJointAt(800));
        CollectionAssert.AreEqual(new[] { 1250.0, 750.0 }, SectionLengths(content));
    }

    [TestMethod]
    public void JointGetsABendThatCanBePlacedExactly()
    {
        var content = ModuleJoints.InsertPoint(0, 0).Apply(WithJointAt(1000));
        var joint = Drawing(content).InternalJoints[0];
        Assert.HasCount(3, joint.Points);
        var bend = OnTrack(content, 1100);
        content = ModuleJoints.SetPoint(0, 1, bend).Apply(content);
        Assert.IsTrue(Drawing(content).InternalJoints[0].Points[1].IsCloseTo(bend, Precision));
        Assert.HasCount(2, Drawing(content).Sections, "Still two sections, along the bent joint.");
    }

    [TestMethod]
    public void MovedEndStaysOnTheEdge()
    {
        var content = WithJointAt(1000);
        var joint = Drawing(content).InternalJoints[0];
        var outline = ModuleJoints.Outline(content)!;
        content = ModuleJoints.SetPoint(0, 0, joint.Start + new Vector(150, -300)).Apply(content);
        var start = Drawing(content).InternalJoints[0].Start;
        Assert.AreEqual(0, outline.Project(start).Pose.Position.DistanceTo(start), Precision);
    }

    [TestMethod]
    public void JointsMayNotCross()
    {
        var content = ModuleJoints.Add(OnTrack(Straight(), 1500)).Apply(WithJointAt(500));
        var first = Drawing(content).InternalJoints[0];
        // Moving the end of the joint at 500 past the one at 1500 would cross it.
        var crossed = ModuleJoints.SetPoint(0, 0, first.Start + (OnTrack(content, 1800) - OnTrack(content, 500))).Apply(content);
        Assert.AreSame(content, crossed);
    }

    [TestMethod]
    public void DeletedJointJoinsItsSections()
    {
        var content = ModuleJoints.Delete([ModuleItem.OfJoint(0)]).Apply(WithJointAt(800));
        Assert.HasCount(1, Drawing(content).Sections);
        Assert.IsEmpty(Drawing(content).InternalJoints);
    }

    [TestMethod]
    public void DeletedBendMakesTheJointStraightAgain()
    {
        var content = ModuleJoints.InsertPoint(0, 0).Apply(WithJointAt(1000));
        content = ModuleJoints.Delete([ModuleItem.OfPoint(0, 1)]).Apply(content);
        Assert.IsNull(Drawing(content).InternalJoints[0].Bends);
    }

    [TestMethod]
    public void EndsCantBeDeletedAsBends()
    {
        var content = WithJointAt(1000);
        Assert.AreSame(content, ModuleJoints.DeletePoint(0, 0).Apply(content));
    }

    [TestMethod]
    public void HitTestFindsPointsBeforeTheJoint()
    {
        var content = WithJointAt(1000);
        var scene = new ModuleScene();
        var joint = Drawing(content).InternalJoints[0];
        Assert.IsTrue(scene.TryHitTest(content, joint.Start, Reach, out var end));
        Assert.AreEqual(ModuleItem.OfPoint(0, 0), end);
        Assert.IsTrue(scene.TryHitTest(content, Point.Lerp(joint.Start, joint.End, 0.5), Reach, out var line));
        Assert.AreEqual(ModuleItem.OfJoint(0), line);
    }

    [TestMethod]
    public void DraggingAJointMovesItAlongTheModule()
    {
        var content = WithJointAt(800);
        var offset = OnTrack(content, 1000) - OnTrack(content, 800);
        content = ModuleJoints.Move([ModuleItem.OfJoint(0)], offset).Apply(content);
        CollectionAssert.AreEqual(new[] { 1000.0, 1000.0 }, SectionLengths(content));
    }

    [TestMethod]
    public void ItemsRoundTripAsText()
    {
        Assert.IsTrue(ModuleItem.TryParse(ModuleItem.OfPoint(2, 3).ToString(), out var item));
        Assert.AreEqual(ModuleItem.OfPoint(2, 3), item);
    }
}
