namespace Tellurian.Trains.Drawing.Editing.Commands;

/// <summary>
/// A document being edited, with undo and redo (MD-11, HP-6). Each command produces a new immutable
/// document, and the history keeps the previous ones, so undo can't drift from what was applied.
/// </summary>
public sealed class EditorDocument<TDocument> where TDocument : class
{
    public const int DefaultHistoryLimit = 200;

    private readonly LinkedList<HistoryEntry> _undo = new();
    private readonly Stack<HistoryEntry> _redo = new();
    private TDocument _saved;

    public EditorDocument(TDocument document, int historyLimit = DefaultHistoryLimit)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentOutOfRangeException.ThrowIfLessThan(historyLimit, 1);
        Current = document;
        _saved = document;
        HistoryLimit = historyLimit;
    }

    /// <summary>Raised after every change of <see cref="Current"/>: apply, undo, redo or replace.</summary>
    public event EventHandler? Changed;

    public TDocument Current { get; private set; }
    public int HistoryLimit { get; }

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    /// <summary>The description of the command that undo reverts, or null.</summary>
    public string? UndoDescription => _undo.Last?.Value.Description;

    /// <summary>The description of the command that redo applies again, or null.</summary>
    public string? RedoDescription => _redo.TryPeek(out var entry) ? entry.Description : null;

    /// <summary>True if the document differs from the one last marked as saved.</summary>
    public bool IsModified => !ReferenceEquals(Current, _saved);

    /// <summary>Applies <paramref name="command"/>; a command that changes nothing isn't recorded. Returns true if the document changed.</summary>
    public bool Apply(IEditCommand<TDocument> command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var changed = command.Apply(Current);
        if (ReferenceEquals(changed, Current) || changed.Equals(Current)) return false;
        _undo.AddLast(new HistoryEntry(command.Description, Current, changed));
        if (_undo.Count > HistoryLimit) _undo.RemoveFirst();
        _redo.Clear();
        SetCurrent(changed);
        return true;
    }

    public bool Undo()
    {
        if (_undo.Last is not { } last) return false;
        _undo.RemoveLast();
        _redo.Push(last.Value);
        SetCurrent(last.Value.Before);
        return true;
    }

    public bool Redo()
    {
        if (!_redo.TryPop(out var entry)) return false;
        _undo.AddLast(entry);
        SetCurrent(entry.After);
        return true;
    }

    /// <summary>Records that the current document is saved, e.g. to a draft or a file.</summary>
    public void MarkSaved() => _saved = Current;

    /// <summary>Replaces the document and clears the history, e.g. after updating an embedded module (HP-16).</summary>
    public void Replace(TDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _undo.Clear();
        _redo.Clear();
        _saved = document;
        SetCurrent(document);
    }

    private void SetCurrent(TDocument document)
    {
        Current = document;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed record HistoryEntry(string Description, TDocument Before, TDocument After);
}
