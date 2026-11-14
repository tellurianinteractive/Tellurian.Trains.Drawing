using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.HallPlans;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class HallPlanTests
{
    private static HallPlan Plan() => TestHalls.Plan(TestCatalogue.Instance);

    [TestMethod]
    public void HallRoundTripsIdentically()
    {
        var json = DrawingFileSerializer.Serialize(TestHalls.Club());
        var read = DrawingFileSerializer.DeserializeHall(json);
        Assert.AreEqual(json, DrawingFileSerializer.Serialize(read));
        Assert.AreEqual(DrawingFileKind.Hall, DrawingFileSerializer.ReadHeader(json).Kind);
    }

    [TestMethod]
    public void HallPlanRoundTripsIdentically()
    {
        var plan = Plan();
        var json = DrawingFileSerializer.Serialize(plan);
        var read = DrawingFileSerializer.DeserializeHallPlan(json);
        Assert.AreEqual(json, DrawingFileSerializer.Serialize(read));
        Assert.AreEqual(plan.Layouts[0].AnchorPose, read.Layouts[0].AnchorPose);
        Assert.AreEqual(plan.Acknowledgements[0].Time, read.Acknowledgements[0].Time);
    }

    [TestMethod]
    public void HallPlanIsNotReadAsAHall() =>
        Assert.AreEqual(DrawingFileError.WrongKind,
            Assert.ThrowsExactly<DrawingFileException>(() => DrawingFileSerializer.DeserializeHall(DrawingFileSerializer.Serialize(Plan()))).Error);

    [TestMethod]
    public void FlipSwapsTheExits()
    {
        var entry = new LineEntry { ModuleId = Guid.NewGuid(), EntryExit = "W", ExitExit = "E" };
        var flipped = entry.Flipped();
        Assert.AreEqual("E", flipped.EntryExit);
        Assert.AreEqual("W", flipped.ExitExit);
        Assert.AreEqual(entry, flipped.Flipped());
    }

    [TestMethod]
    public void EndOfLineEntryHasOneExit()
    {
        var entry = new LineEntry { ModuleId = Guid.NewGuid(), EntryExit = "W" };
        Assert.AreEqual(new ExitReference(entry.ModuleId, "W"), entry.Entry);
        Assert.IsNull(entry.Exit);
    }

    [TestMethod]
    public void FindsEntriesInAllLines()
    {
        var plan = Plan();
        var terminus = plan.Modules[3];
        Assert.AreEqual(4, plan.Entries().Count());
        Assert.AreEqual("W", plan.FindEntry(terminus.Id)?.EntryExit);
        Assert.IsTrue(plan.Layouts[0].Lines[1].IsBranch);
        Assert.IsFalse(plan.Layouts[0].Lines[0].IsBranch);
        Assert.IsEmpty(plan.ModulesPlacedMoreThanOnce());
    }

    [TestMethod]
    public void ModulePlacedTwiceIsFound()
    {
        var plan = Plan();
        var layout = plan.Layouts[0];
        var main = layout.Lines[0];
        var twice = plan with { Layouts = [layout with { Lines = [main with { Entries = [.. main.Entries, main.Entries[0]] }, layout.Lines[1]] }] };
        CollectionAssert.AreEqual(new[] { main.Entries[0].ModuleId }, twice.ModulesPlacedMoreThanOnce().ToArray());
    }

    [TestMethod]
    public void JointsAreFoundInEitherOrder()
    {
        var plan = Plan();
        var joint = plan.Joints[0];
        Assert.AreSame(joint, plan.FindJoint(joint.First, joint.Second));
        Assert.AreSame(joint, plan.FindJoint(joint.Second, joint.First));
        Assert.IsNull(plan.FindJoint(joint.First, joint.First));
    }

    [TestMethod]
    public void PrimaryStandardIsAccepted()
    {
        var plan = Plan();
        var withDuplicate = plan with { AcceptedStandards = [plan.PrimaryStandard, new(TestCatalogue.MmmId, "MMM-2R")] };
        Assert.AreEqual(2, withDuplicate.AllAcceptedStandards().Count());
        Assert.IsTrue(plan.IsAccepted(TestCatalogue.FremoId));
        Assert.IsFalse(plan.IsAccepted(TestCatalogue.MmmId));
        Assert.IsTrue(withDuplicate.IsAccepted(TestCatalogue.MmmId));
    }

    [TestMethod]
    public void EmbeddedDataIsFound()
    {
        var plan = Plan();
        Assert.AreSame(plan.Modules[0], plan.FindModule(plan.Modules[0].Id));
        Assert.IsNotNull(plan.FindEndProfile(TestCatalogue.FremoDouble.Id));
        Assert.IsNotNull(plan.FindStandard(TestCatalogue.FremoId));
    }

    [TestMethod]
    public void WalkingPathWidthDefaultsToThePlan()
    {
        var settings = new HallPlanSettings();
        var path = new WalkingPath { CentreLine = PathDrawing.Polyline([new(0, 0), new(1000, 0)]) };
        Assert.AreEqual(HallPlanSettings.DefaultWalkingPathWidth, path.EffectiveWidth(settings));
        Assert.AreEqual(800, (path with { Width = 800 }).EffectiveWidth(settings));
    }
}
