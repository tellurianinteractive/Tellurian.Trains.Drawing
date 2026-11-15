using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Editing.Snapping;
using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Editing.Tools;

/// <summary>
/// Places something with one click at a snapped point, e.g. a screw hole or a track; the editor's factory
/// turns the point into a command. <see cref="Constrain"/> can restrict the point, e.g. tracks to rail top.
/// </summary>
public sealed class PlaceTool<TDocument>(EditorDocument<TDocument> document, SnapEngine snap, Func<Point, IEditCommand<TDocument>> createCommand) : ITool
    where TDocument : class
{
    /// <summary>Restricts the placed point, e.g. to Y = 0 for tracks; the identity by default.</summary>
    public Func<Point, Point> Constrain { get; init; } = point => point;

    /// <summary>Where a click now would place, for the preview.</summary>
    public Point? Preview { get; private set; }

    public SnapResult? Snapped { get; private set; }

    public void PointerDown(ToolPointer pointer)
    {
        if (pointer.Button != PointerButton.Primary) return;
        document.Apply(createCommand(Locate(pointer)));
    }

    public void PointerMove(ToolPointer pointer) => Preview = Locate(pointer);

    public void PointerUp(ToolPointer pointer)
    {
    }

    public bool KeyDown(string key)
    {
        if (key != "Escape" || Preview is null) return false;
        Cancel();
        return true;
    }

    public void Cancel()
    {
        Preview = null;
        Snapped = null;
    }

    private Point Locate(ToolPointer pointer)
    {
        Snapped = null;
        var point = pointer.World;
        if (!pointer.IsSnapSuppressed)
        {
            var result = snap.Snap(point, pointer.Tolerance);
            if (result.IsSnapped)
            {
                Snapped = result;
                point = result.Point;
            }
        }
        return Constrain(point);
    }
}
