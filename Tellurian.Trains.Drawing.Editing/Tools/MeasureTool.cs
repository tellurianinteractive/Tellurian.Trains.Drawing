using Tellurian.Trains.Drawing.Editing.Snapping;
using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Editing.Tools;

/// <summary>
/// Measures distance and angle between two snapped points (MD-12) without changing the document:
/// click the first point, move, and click the second; the last measurement stays until the next one.
/// </summary>
public sealed class MeasureTool(SnapEngine snap) : ITool
{
    private Point? _start;

    /// <summary>The current measurement: following the pointer while measuring, then the last one.</summary>
    public Measurement? Measurement { get; private set; }

    public bool IsMeasuring => _start is not null;

    public SnapResult? Snapped { get; private set; }

    public void PointerDown(ToolPointer pointer)
    {
        if (pointer.Button != PointerButton.Primary) return;
        var point = Locate(pointer);
        if (_start is { } start)
        {
            Measurement = new(start, point);
            _start = null;
        }
        else
        {
            _start = point;
            Measurement = new(point, point);
        }
    }

    public void PointerMove(ToolPointer pointer)
    {
        var point = Locate(pointer);
        if (_start is { } start) Measurement = new(start, point);
    }

    public void PointerUp(ToolPointer pointer)
    {
    }

    public bool KeyDown(string key)
    {
        if (key != "Escape" || (Measurement is null && _start is null)) return false;
        Cancel();
        return true;
    }

    public void Cancel()
    {
        _start = null;
        Measurement = null;
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
