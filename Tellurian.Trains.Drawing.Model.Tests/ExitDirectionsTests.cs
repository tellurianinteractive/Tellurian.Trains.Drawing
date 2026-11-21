using Tellurian.Trains.Schedules.Model.Geometry;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>Up and down are worked out from the left and right exits (ModuleDrawing §4.1, task B2).</summary>
[TestClass]
public sealed class ExitDirectionsTests
{
    private static readonly Point Left = Point.Origin;
    private static readonly Point Right = new(1000, 0);

    private static Pose Heading(double degrees) => new(new(500, 0), new(degrees));

    // The viewer has the left exit on the left, so the side away from the viewer is +Y when left to right is +X.
    [TestMethod]
    [DataRow(90, ExitSide.Up)]
    [DataRow(46, ExitSide.Up)]
    [DataRow(134, ExitSide.Up)]
    [DataRow(-90, ExitSide.Down)]
    [DataRow(270, ExitSide.Down)]
    [DataRow(-46, ExitSide.Down)]
    [DataRow(0, ExitSide.Undefined)]
    [DataRow(45, ExitSide.Undefined)]
    [DataRow(135, ExitSide.Undefined)]
    [DataRow(180, ExitSide.Undefined)]
    [DataRow(-15, ExitSide.Undefined)]
    public void AnExitAcrossTheLeftRightDirectionIsUpOrDown(double heading, ExitSide expected) =>
        Assert.AreEqual(expected, ExitDirections.Infer(Heading(heading), Left, Right));

    [TestMethod]
    public void UpFollowsTheModuleHoweverItIsDrawn()
    {
        // Left to right is +Y here, so away from the viewer is −X.
        Assert.AreEqual(ExitSide.Up, ExitDirections.Infer(Heading(180), Point.Origin, new(0, 1000)));
        Assert.AreEqual(ExitSide.Down, ExitDirections.Infer(Heading(0), Point.Origin, new(0, 1000)));
    }

    [TestMethod]
    public void NothingIsInferredWhenLeftAndRightAreAtTheSamePlace() =>
        Assert.AreEqual(ExitSide.Undefined, ExitDirections.Infer(Heading(90), Left, Left));

    [TestMethod]
    public void ABranchAtARightAngleBecomesUp()
    {
        var drawing = WithThirdExit(ExitSide.Undefined, 90).WithInferredExitDirections();
        CollectionAssert.AreEqual(
            new[] { ExitSide.Left, ExitSide.Right, ExitSide.Up },
            drawing.Exits.Select(e => e.Direction).ToArray());
    }

    [TestMethod]
    public void AChosenDirectionIsNotChanged()
    {
        var drawing = WithThirdExit(ExitSide.Right, 90);
        Assert.AreSame(drawing, drawing.WithInferredExitDirections());
    }

    [TestMethod]
    public void AnExitAlongTheModuleStaysUndefined()
    {
        var drawing = WithThirdExit(ExitSide.Undefined, 10);
        Assert.AreSame(drawing, drawing.WithInferredExitDirections());
    }

    [TestMethod]
    public void NothingIsInferredWithoutBothLeftAndRight()
    {
        var drawing = WithThirdExit(ExitSide.Undefined, 90);
        var withoutRight = drawing.With(edited => edited.Exits = [.. drawing.Exits.Select(e => e.Direction == ExitSide.Right ? e.With(edited => edited.Direction = ExitSide.Undefined) : e)]);
        Assert.AreSame(withoutRight, withoutRight.WithInferredExitDirections());
    }

    [TestMethod]
    public void AutoDrawKeepsTheDirectionsItIsGiven()
    {
        var exits = TestModules.Junction().Drawing.Exits;
        CollectionAssert.AreEqual(
            new[] { ExitSide.Left, ExitSide.Right, ExitSide.Right },
            exits.Select(e => e.Direction).ToArray());
    }

    /// <summary>The junction with its third exit turned to <paramref name="heading"/> and given <paramref name="direction"/>.</summary>
    private static ModuleDrawing WithThirdExit(ExitSide direction, double heading)
    {
        var drawing = TestModules.Junction().Drawing;
        var third = drawing.Exits[2].With(edited => { edited.Direction = direction; edited.Pose = new(new(750, 250), new(heading)); });
        return drawing.With(edited => edited.Exits = [drawing.Exits[0], drawing.Exits[1], third]);
    }
}
