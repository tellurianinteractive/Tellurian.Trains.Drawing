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
    [DataRow(90, ExitDirection.Up)]
    [DataRow(46, ExitDirection.Up)]
    [DataRow(134, ExitDirection.Up)]
    [DataRow(-90, ExitDirection.Down)]
    [DataRow(270, ExitDirection.Down)]
    [DataRow(-46, ExitDirection.Down)]
    [DataRow(0, ExitDirection.Undefined)]
    [DataRow(45, ExitDirection.Undefined)]
    [DataRow(135, ExitDirection.Undefined)]
    [DataRow(180, ExitDirection.Undefined)]
    [DataRow(-15, ExitDirection.Undefined)]
    public void AnExitAcrossTheLeftRightDirectionIsUpOrDown(double heading, ExitDirection expected) =>
        Assert.AreEqual(expected, ExitDirections.Infer(Heading(heading), Left, Right));

    [TestMethod]
    public void UpFollowsTheModuleHoweverItIsDrawn()
    {
        // Left to right is +Y here, so away from the viewer is −X.
        Assert.AreEqual(ExitDirection.Up, ExitDirections.Infer(Heading(180), Point.Origin, new(0, 1000)));
        Assert.AreEqual(ExitDirection.Down, ExitDirections.Infer(Heading(0), Point.Origin, new(0, 1000)));
    }

    [TestMethod]
    public void NothingIsInferredWhenLeftAndRightAreAtTheSamePlace() =>
        Assert.AreEqual(ExitDirection.Undefined, ExitDirections.Infer(Heading(90), Left, Left));

    [TestMethod]
    public void ABranchAtARightAngleBecomesUp()
    {
        var drawing = WithThirdExit(ExitDirection.Undefined, 90).WithInferredExitDirections();
        CollectionAssert.AreEqual(
            new[] { ExitDirection.Left, ExitDirection.Right, ExitDirection.Up },
            drawing.Exits.Select(e => e.Direction).ToArray());
    }

    [TestMethod]
    public void AChosenDirectionIsNotChanged()
    {
        var drawing = WithThirdExit(ExitDirection.Right, 90);
        Assert.AreSame(drawing, drawing.WithInferredExitDirections());
    }

    [TestMethod]
    public void AnExitAlongTheModuleStaysUndefined()
    {
        var drawing = WithThirdExit(ExitDirection.Undefined, 10);
        Assert.AreSame(drawing, drawing.WithInferredExitDirections());
    }

    [TestMethod]
    public void NothingIsInferredWithoutBothLeftAndRight()
    {
        var drawing = WithThirdExit(ExitDirection.Undefined, 90);
        var withoutRight = drawing with { Exits = [.. drawing.Exits.Select(e => e.Direction == ExitDirection.Right ? e with { Direction = ExitDirection.Undefined } : e)] };
        Assert.AreSame(withoutRight, withoutRight.WithInferredExitDirections());
    }

    [TestMethod]
    public void AutoDrawKeepsTheDirectionsItIsGiven()
    {
        var exits = TestModules.Junction().Drawing.Exits;
        CollectionAssert.AreEqual(
            new[] { ExitDirection.Left, ExitDirection.Right, ExitDirection.Right },
            exits.Select(e => e.Direction).ToArray());
    }

    /// <summary>The junction with its third exit turned to <paramref name="heading"/> and given <paramref name="direction"/>.</summary>
    private static ModuleDrawing WithThirdExit(ExitDirection direction, double heading)
    {
        var drawing = TestModules.Junction().Drawing;
        var third = drawing.Exits[2] with { Direction = direction, Pose = new(new(750, 250), new(heading)) };
        return drawing with { Exits = [drawing.Exits[0], drawing.Exits[1], third] };
    }
}
