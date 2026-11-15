using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Editing.Snapping;
using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model;

namespace Tellurian.Trains.Drawing.Editing.Tools;

/// <summary>
/// Adds a dimension (EP-10) with three clicks: the two snapped points, then where the dimension line goes.
/// Its kind follows the placement (beside: vertical, above or below: horizontal, otherwise aligned) unless
/// <see cref="Kind"/> fixes it. The dimension becomes a command from the editor's factory.
/// </summary>
public sealed class DimensionTool<TDocument>(EditorDocument<TDocument> document, SnapEngine snap, Func<Dimension, IEditCommand<TDocument>> createCommand) : ITool
    where TDocument : class
{
    private Point? _start;
    private Point? _end;

    /// <summary>A fixed kind, or null to follow the placement.</summary>
    public DimensionKind? Kind { get; set; }

    /// <summary>The dimension being placed, for the preview; null before two points are chosen.</summary>
    public Dimension? Preview { get; private set; }

    /// <summary>The first point while choosing the second.</summary>
    public Point? FirstPoint => _start;

    public SnapResult? Snapped { get; private set; }

    public void PointerDown(ToolPointer pointer)
    {
        if (pointer.Button != PointerButton.Primary) return;
        if (_start is null)
        {
            _start = Locate(pointer);
        }
        else if (_end is null)
        {
            var point = Locate(pointer);
            if (point.IsCloseTo(_start.Value, Tolerance.Length)) return;
            _end = point;
            Preview = Dimension.Place(_start.Value, point, pointer.World, Kind);
        }
        else
        {
            var dimension = Dimension.Place(_start.Value, _end.Value, pointer.World, Kind);
            Cancel();
            document.Apply(createCommand(dimension));
        }
    }

    public void PointerMove(ToolPointer pointer)
    {
        if (_start is { } start && _end is { } end) Preview = Dimension.Place(start, end, pointer.World, Kind);
        else Locate(pointer);
    }

    public void PointerUp(ToolPointer pointer)
    {
    }

    public bool KeyDown(string key)
    {
        if (key != "Escape" || _start is null) return false;
        Cancel();
        return true;
    }

    public void Cancel()
    {
        _start = null;
        _end = null;
        Preview = null;
        Snapped = null;
    }

    private Point Locate(ToolPointer pointer)
    {
        Snapped = null;
        if (pointer.IsSnapSuppressed) return pointer.World;
        var result = snap.Snap(pointer.World, pointer.Tolerance);
        if (result.IsSnapped) Snapped = result;
        return result.Point;
    }
}
