using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Schedules.Model.Layouts;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>
/// Halls and hall plans through this app's serializer. The hall plan is the shared model's, a layout in a hall; its
/// lines, placement and file format are tested where they are, in the Schedules repository.
/// </summary>
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
        Assert.AreEqual(plan.Anchors[0].Pose, read.Anchors[0].Pose);
        Assert.AreEqual(plan.Acknowledgements[0].Time, read.Acknowledgements[0].Time);
        Assert.AreEqual(plan.Id, read.Layout.SourceId, "the layout has the hall plan's identity");
        Assert.AreEqual("Club meeting", HallPlanFile.ReadLayout(json).Name, "the planner reads the layout as written");
    }

    [TestMethod]
    public void HallPlanIsNotReadAsAHall() =>
        Assert.AreEqual(DrawingFileError.WrongKind,
            Assert.ThrowsExactly<DrawingFileException>(() => DrawingFileSerializer.DeserializeHall(DrawingFileSerializer.Serialize(Plan()))).Error);

    [TestMethod]
    public void TheModulesArePlacedInLinesWithIdsOfTheLayouts()
    {
        var layout = Plan().Layout;
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, layout.Modules.Select(m => m.Id).ToArray());
        Assert.AreEqual(4, layout.Entries.Count());
        Assert.IsTrue(layout.FindEntry(2)!.IsTurned);
        Assert.AreSame(layout.Lines.Last(), layout.LineOf(4));
        Assert.IsTrue(layout.Lines.Last().IsBranch);
        CollectionAssert.AreEqual(layout.Lines.ToArray(), layout.GroupOf(layout.MainLines.Single()).ToArray());
    }

    [TestMethod]
    public void AnExitIsFoundBySideAndNumberWhateverItsLabel()
    {
        var junction = Plan().Layout.FindModule(3)!.Module.Drawing;
        var second = new ExitPlace(ExitSide.Right, 2).In(junction)!;
        Assert.AreEqual("E2", second.Label);
        Assert.AreEqual(new ExitPlace(ExitSide.Right, 2), ExitPlace.Of(junction, second));
        var relabelled = junction.With(edited => edited.Exits = [.. junction.Exits.Select(e => e.With(edited => edited.Label = $"x{e.Label}"))]);
        Assert.AreEqual("xE2", new ExitPlace(ExitSide.Right, 2).In(relabelled)!.Label);
        Assert.IsNull(new ExitPlace(ExitSide.Right, 3).In(junction));
    }
}
