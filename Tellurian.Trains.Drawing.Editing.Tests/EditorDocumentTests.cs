using Tellurian.Trains.Drawing.Editing.Commands;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class EditorDocumentTests
{
    private sealed record Sketch(string Name, int Count);

    private static EditCommand<Sketch> Rename(string name) => new($"Rename to {name}", s => s with { Name = name });

    private static EditCommand<Sketch> Increment() => new("Increment", s => s with { Count = s.Count + 1 });

    [TestMethod]
    public void ApplyUndoAndRedo()
    {
        var document = new EditorDocument<Sketch>(new("A", 0));
        document.Apply(Rename("B"));
        document.Apply(Increment());

        Assert.AreEqual(new Sketch("B", 1), document.Current);
        Assert.AreEqual("Increment", document.UndoDescription);

        Assert.IsTrue(document.Undo());
        Assert.AreEqual(new Sketch("B", 0), document.Current);
        Assert.AreEqual("Increment", document.RedoDescription);

        Assert.IsTrue(document.Undo());
        Assert.AreEqual(new Sketch("A", 0), document.Current);
        Assert.IsFalse(document.CanUndo);
        Assert.IsFalse(document.Undo());

        Assert.IsTrue(document.Redo());
        Assert.IsTrue(document.Redo());
        Assert.AreEqual(new Sketch("B", 1), document.Current);
        Assert.IsFalse(document.Redo());
    }

    [TestMethod]
    public void NewCommandClearsRedo()
    {
        var document = new EditorDocument<Sketch>(new("A", 0));
        document.Apply(Increment());
        document.Undo();
        document.Apply(Rename("C"));
        Assert.IsFalse(document.CanRedo);
        Assert.AreEqual(new Sketch("C", 0), document.Current);
    }

    [TestMethod]
    public void CommandThatChangesNothingIsNotRecorded()
    {
        var document = new EditorDocument<Sketch>(new("A", 0));
        var changes = 0;
        document.Changed += (_, _) => changes++;
        Assert.IsFalse(document.Apply(Rename("A")));
        Assert.IsFalse(document.CanUndo);
        Assert.AreEqual(0, changes);
    }

    [TestMethod]
    public void HistoryIsLimited()
    {
        var document = new EditorDocument<Sketch>(new("A", 0), historyLimit: 3);
        for (var i = 0; i < 5; i++) document.Apply(Increment());
        var undone = 0;
        while (document.Undo()) undone++;
        Assert.AreEqual(3, undone);
        Assert.AreEqual(2, document.Current.Count);
    }

    [TestMethod]
    public void ModifiedFollowsTheSavedDocument()
    {
        var document = new EditorDocument<Sketch>(new("A", 0));
        Assert.IsFalse(document.IsModified);
        document.Apply(Increment());
        Assert.IsTrue(document.IsModified);
        document.MarkSaved();
        Assert.IsFalse(document.IsModified);
        document.Undo();
        Assert.IsTrue(document.IsModified);
        document.Redo();
        Assert.IsFalse(document.IsModified);
    }

    [TestMethod]
    public void ReplaceClearsTheHistoryAndRaisesChanged()
    {
        var document = new EditorDocument<Sketch>(new("A", 0));
        document.Apply(Increment());
        var changes = 0;
        document.Changed += (_, _) => changes++;

        document.Replace(new("Z", 9));

        Assert.AreEqual(1, changes);
        Assert.IsFalse(document.CanUndo);
        Assert.IsFalse(document.IsModified);
    }
}
