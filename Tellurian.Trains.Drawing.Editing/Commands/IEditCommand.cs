namespace Tellurian.Trains.Drawing.Editing.Commands;

/// <summary>
/// A change to a document. Documents are immutable records, so a command returns the changed document;
/// undo restores the previous one.
/// </summary>
public interface IEditCommand<TDocument> where TDocument : class
{
    /// <summary>Shown in the undo and redo menus, e.g. "Move 3 items".</summary>
    string Description { get; }

    TDocument Apply(TDocument document);
}
