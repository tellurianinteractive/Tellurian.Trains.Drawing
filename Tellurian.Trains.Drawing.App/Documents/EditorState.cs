namespace Tellurian.Trains.Drawing.App.Documents;

/// <summary>
/// What an editor looked like, so it can be restored after a reload (AP-7): the view and the selection.
/// Selected items are stored by their ids as text.
/// </summary>
public sealed record EditorState(double CenterX, double CenterY, double Scale, IReadOnlyList<string> Selection)
{
    /// <summary>The workbench key of the state of the editor with <paramref name="key"/>.</summary>
    public static string StorageKey(string key) => $"editor-state:{key}";
}
