namespace Tellurian.Trains.Drawing.Editing.Commands;

/// <summary>A command given as a function from the old document to the new one.</summary>
public sealed record EditCommand<TDocument>(string Description, Func<TDocument, TDocument> Change) : IEditCommand<TDocument>
    where TDocument : class
{
    public TDocument Apply(TDocument document) => Change(document);
}
