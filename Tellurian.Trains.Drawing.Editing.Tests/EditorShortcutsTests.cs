using Tellurian.Trains.Schedules.Model.Geometry;
using Tellurian.Trains.Drawing.Editing.Input;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class EditorShortcutsTests
{
    private static EditorAction? Action(KeyPress press) => EditorShortcuts.Map(press)?.Action;

    [TestMethod]
    public void UndoAndRedoWorkWithCtrlAndCommand()
    {
        Assert.AreEqual(EditorAction.Undo, Action(new("z", Ctrl: true)));
        Assert.AreEqual(EditorAction.Undo, Action(new("Z", Meta: true)));
        Assert.AreEqual(EditorAction.Redo, Action(new("y", Ctrl: true)));
        Assert.AreEqual(EditorAction.Redo, Action(new("Z", Ctrl: true, Shift: true)));
        Assert.IsNull(Action(new("a", Ctrl: true)), "Other Ctrl keys are left to the browser.");
    }

    [TestMethod]
    public void DeleteAndBackspaceDelete()
    {
        Assert.AreEqual(EditorAction.Delete, Action(new("Delete")));
        Assert.AreEqual(EditorAction.Delete, Action(new("Backspace")));
    }

    [TestMethod]
    public void EscapeCancelsAndEnterFinishes()
    {
        Assert.AreEqual(EditorAction.Cancel, Action(new("Escape")));
        Assert.AreEqual(EditorAction.Finish, Action(new("Enter")));
    }

    [TestMethod]
    public void DigitsChooseToolsAndZeroFits()
    {
        Assert.AreEqual(new EditorShortcut(EditorAction.Tool, "1", Tool: 1), EditorShortcuts.Map(new("1")));
        Assert.AreEqual(9, EditorShortcuts.Map(new("9"))!.Tool);
        Assert.AreEqual(EditorAction.Fit, Action(new("0")));
        Assert.IsNull(Action(new("1", Alt: true)));
        Assert.IsNull(Action(new("v")), "Letters are not shortcuts: tool names differ between languages.");
    }

    [TestMethod]
    public void ArrowsNudgeWithYUp()
    {
        Assert.AreEqual(new Vector(-EditorShortcuts.NudgeStep, 0), EditorShortcuts.Map(new("ArrowLeft"))!.Nudge);
        Assert.AreEqual(new Vector(0, EditorShortcuts.NudgeStep), EditorShortcuts.Map(new("ArrowUp"))!.Nudge);
        Assert.AreEqual(new Vector(EditorShortcuts.LargeNudgeStep, 0), EditorShortcuts.Map(new("ArrowRight", Shift: true))!.Nudge);
        Assert.AreEqual(new Vector(0, -EditorShortcuts.LargeNudgeStep), EditorShortcuts.Map(new("ArrowDown", Shift: true))!.Nudge);
    }

    [TestMethod]
    public void AToolsTooltipNamesItsKey()
    {
        Assert.AreEqual("Select (1)", EditorShortcuts.WithKey("Select", 1));
        Assert.AreEqual("Tenth", EditorShortcuts.WithKey("Tenth", 10));
    }
}
