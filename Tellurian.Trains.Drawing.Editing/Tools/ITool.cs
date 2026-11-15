namespace Tellurian.Trains.Drawing.Editing.Tools;

/// <summary>
/// An editor tool as a state machine driven by pointer and key events (Phase 4). Tools change the document
/// only through commands, so everything they do can be undone. The view draws a tool's preview from its state.
/// </summary>
public interface ITool
{
    void PointerDown(ToolPointer pointer);
    void PointerMove(ToolPointer pointer);
    void PointerUp(ToolPointer pointer);

    /// <summary>Handles a key, e.g. "Escape" or "Delete"; returns true if the tool used it.</summary>
    bool KeyDown(string key);

    /// <summary>Abandons what the tool is doing, e.g. when another tool is chosen.</summary>
    void Cancel();
}
