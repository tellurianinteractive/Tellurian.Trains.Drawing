using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Editing.Snapping;
using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model;

namespace Tellurian.Trains.Drawing.Editing.Tools;

/// <summary>
/// Draws a path of lines and tangent arcs by clicking (Phase 4): end-plate outlines, ground lines, module
/// outlines and hall walls. Points snap (MD-12); Shift keeps the direction to <see cref="AngleStep"/> steps.
/// Clicking the start closes the path; Enter finishes an open one; Backspace removes the last step;
/// Escape abandons it. The finished path becomes a command from <see cref="CreateCommand"/>.
/// </summary>
public sealed class PathTool<TDocument> : ITool where TDocument : class
{
    /// <summary>Directions are constrained to multiples of this many degrees with Shift.</summary>
    public const double AngleStep = 15;

    private readonly EditorDocument<TDocument> _document;
    private readonly SnapEngine _snap;
    private readonly Func<PathDrawing, IEditCommand<TDocument>> _createCommand;
    private readonly List<PathStep> _steps = [];
    private Point? _start;
    private Point? _pointer;

    public PathTool(EditorDocument<TDocument> document, SnapEngine snap, Func<PathDrawing, IEditCommand<TDocument>> createCommand)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _snap = snap ?? throw new ArgumentNullException(nameof(snap));
        _createCommand = createCommand ?? throw new ArgumentNullException(nameof(createCommand));
    }

    public PathStepMode Mode { get; set; }

    /// <summary>True if the path must be closed, e.g. an outline; Enter then does nothing.</summary>
    public bool RequiresClosed { get; init; }

    /// <summary>True if the path may be closed by clicking its start; false for e.g. ground lines.</summary>
    public bool AllowsClosed { get; init; } = true;

    /// <summary>The command that adds a finished path to the document.</summary>
    public Func<PathDrawing, IEditCommand<TDocument>> CreateCommand => _createCommand;

    public bool IsDrawing => _start is not null;

    /// <summary>The path so far, without the step to the pointer; null before the first click or with no steps.</summary>
    public PathDrawing? Drawn => _start is { } start && _steps.Count > 0 ? new() { Start = start, Steps = [.. _steps] } : null;

    /// <summary>The path so far with the step to the pointer, for the preview.</summary>
    public PathDrawing? Preview
    {
        get
        {
            if (_start is not { } start || _pointer is not { } pointer || pointer.IsCloseTo(End)) return Drawn;
            return new() { Start = start, Steps = [.. _steps, NextStep(pointer)] };
        }
    }

    /// <summary>What the pointer snapped to, for a marker; null when not snapped.</summary>
    public SnapResult? Snapped { get; private set; }

    /// <summary>True when a click now would close the path.</summary>
    public bool WouldClose { get; private set; }

    private Point End => _steps.Count == 0 ? _start!.Value : Drawn!.ToPath().End;

    public void PointerDown(ToolPointer pointer)
    {
        if (pointer.Button != PointerButton.Primary) return;
        var point = Locate(pointer);
        if (_start is null)
        {
            _start = point;
            return;
        }
        if (WouldClose)
        {
            Finish(close: true);
            return;
        }
        if (point.IsCloseTo(End, Tolerance.Length)) return;
        _steps.Add(NextStep(point));
    }

    public void PointerMove(ToolPointer pointer) => _pointer = Locate(pointer);

    public void PointerUp(ToolPointer pointer)
    {
    }

    public bool KeyDown(string key)
    {
        switch (key)
        {
            case "Escape" when IsDrawing:
                Cancel();
                return true;
            case "Enter" when IsDrawing && !RequiresClosed:
                Finish(close: false);
                return true;
            case "Backspace" when IsDrawing:
                if (_steps.Count > 0) _steps.RemoveAt(_steps.Count - 1);
                else Cancel();
                return true;
            default:
                return false;
        }
    }

    public void Cancel()
    {
        _steps.Clear();
        _start = null;
        _pointer = null;
        Snapped = null;
        WouldClose = false;
    }

    /// <summary>
    /// The arc from <paramref name="start"/>, heading <paramref name="heading"/>, to <paramref name="end"/>; null when
    /// the end lies straight ahead or behind, where no arc fits.
    /// </summary>
    public static ArcStep? TangentArc(Point start, Angle heading, Point end)
    {
        var chord = end - start;
        if (chord.Length < Tolerance.Length) return null;
        var turn = (chord.Direction - heading).Signed.Degrees;
        if (Math.Abs(turn) < Tolerance.Angle || Math.Abs(Math.Abs(turn) - 180) < Tolerance.Angle) return null;
        var radius = chord.Length / (2 * Math.Sin(Math.Abs(turn) * Math.PI / 180));
        var center = start + Vector.FromAngle(heading).LeftNormal * (radius * Math.Sign(turn));
        return new(center, 2 * turn);
    }

    private PathStep NextStep(Point point)
    {
        if (Mode == PathStepMode.TangentArc && _steps.Count > 0 && TangentArc(End, Drawn!.ToPath().EndPose.Heading, point) is { } arc) return arc;
        return new LineStep(point);
    }

    private Point Locate(ToolPointer pointer)
    {
        Snapped = null;
        WouldClose = false;
        var point = pointer.World;
        if (pointer.IsConstrained && _start is not null) point = Constrain(End, point);
        else if (!pointer.IsSnapSuppressed)
        {
            var result = _snap.Snap(point, pointer.Tolerance);
            if (result.IsSnapped)
            {
                Snapped = result;
                point = result.Point;
            }
        }
        // The start may itself be snapped (e.g. to the grid), so both the pointer and its snapped point can close.
        if (_start is { } start && AllowsClosed && _steps.Count >= 2 &&
            (pointer.World.DistanceTo(start) <= pointer.Tolerance || point.DistanceTo(start) <= pointer.Tolerance))
        {
            WouldClose = true;
            Snapped = null;
            return start;
        }
        return point;
    }

    private static Point Constrain(Point from, Point to)
    {
        var vector = to - from;
        if (vector.Length < Tolerance.Length) return to;
        var degrees = Math.Round(vector.Direction.Degrees / AngleStep) * AngleStep;
        var direction = Vector.FromAngle(new Angle(degrees));
        return from + direction * vector.Dot(direction);
    }

    private void Finish(bool close)
    {
        if (_start is { } start && _steps.Count > 0 && (close ? _steps.Count >= 2 : !RequiresClosed))
        {
            var steps = _steps.ToList();
            // A closed path's last step ends at the start; a closing line is added unless the last step already does.
            if (close && !End.IsCloseTo(start)) steps.Add(new LineStep(start));
            _document.Apply(_createCommand(new() { Start = start, Steps = steps, IsClosed = close }));
        }
        Cancel();
    }
}
