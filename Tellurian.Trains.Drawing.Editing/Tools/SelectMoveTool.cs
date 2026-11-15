using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Editing.Selection;
using Tellurian.Trains.Drawing.Editing.Snapping;
using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Editing.Tools;

/// <summary>
/// Selects items by clicking (Ctrl/Cmd toggles), selects by rubber band on empty space, and moves the
/// selection by dragging, with snapping (MD-12). A move is one undoable command, applied on release;
/// meanwhile <see cref="DragOffset"/> is the preview. Escape abandons a drag.
/// </summary>
public sealed class SelectMoveTool<TDocument, TId> : ITool where TDocument : class where TId : notnull
{
    /// <summary>A press that moves less than this many tolerances is a click, not a drag.</summary>
    public const double DragThreshold = 0.5;

    private readonly EditorDocument<TDocument> _document;
    private readonly SelectionSet<TId> _selection;
    private readonly ISelectableScene<TDocument, TId> _scene;
    private readonly SnapEngine _snap;
    private State _state = State.Idle;
    private Point _start;
    private Point _grab;
    private TId? _pressed;
    private bool _hasPressed;
    private bool _pressedWasSelected;

    public SelectMoveTool(EditorDocument<TDocument> document, SelectionSet<TId> selection, ISelectableScene<TDocument, TId> scene, SnapEngine snap)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _snap = snap ?? throw new ArgumentNullException(nameof(snap));
    }

    /// <summary>The offset of the selection while it is being dragged; null otherwise.</summary>
    public Vector? DragOffset { get; private set; }

    /// <summary>The rubber band while it is being drawn; null otherwise.</summary>
    public BoundingBox? RubberBand { get; private set; }

    /// <summary>What the dragged selection snapped to, for a marker in the view; null when not snapped.</summary>
    public SnapResult? Snapped { get; private set; }

    public void PointerDown(ToolPointer pointer)
    {
        if (pointer.Button != PointerButton.Primary) return;
        _start = pointer.World;
        _hasPressed = _scene.TryHitTest(_document.Current, pointer.World, pointer.Tolerance, out var pressed);
        if (!_hasPressed)
        {
            _state = State.PressedOnEmpty;
            return;
        }
        _pressed = pressed;
        _pressedWasSelected = _selection.Contains(pressed);
        if (pointer.IsAdditive) _selection.Toggle(pressed);
        else if (!_pressedWasSelected) _selection.Select(pressed);
        _grab = GrabPoint(pointer);
        _state = _selection.Contains(pressed) ? State.PressedOnItem : State.Idle;
    }

    public void PointerMove(ToolPointer pointer)
    {
        switch (_state)
        {
            case State.PressedOnItem when IsDrag(pointer):
                _state = State.Dragging;
                Drag(pointer);
                break;
            case State.Dragging:
                Drag(pointer);
                break;
            case State.PressedOnEmpty when IsDrag(pointer):
            case State.RubberBand:
                _state = State.RubberBand;
                RubberBand = BoundingBox.FromPoints([_start, pointer.World]);
                break;
        }
    }

    public void PointerUp(ToolPointer pointer)
    {
        switch (_state)
        {
            case State.Dragging:
                Drag(pointer);
                if (DragOffset is { } offset && offset.Length > Tolerance.Length) _document.Apply(_scene.Move([.. _selection.Items], offset));
                break;
            case State.PressedOnItem when !pointer.IsAdditive && _pressedWasSelected && _hasPressed:
                // A click on an item of a multiple selection selects only that item.
                _selection.Select(_pressed!);
                break;
            case State.RubberBand when RubberBand is { } band:
                var document = _document.Current;
                _selection.SelectInside(band, _scene.Index(document), id => _scene.BoundsOf(document, id), pointer.IsAdditive);
                break;
            case State.PressedOnEmpty when !pointer.IsAdditive:
                _selection.Clear();
                break;
        }
        Reset();
    }

    public bool KeyDown(string key)
    {
        if (key != "Escape" || _state == State.Idle) return false;
        Cancel();
        return true;
    }

    public void Cancel() => Reset();

    private bool IsDrag(ToolPointer pointer) => pointer.World.DistanceTo(_start) > pointer.Tolerance * DragThreshold;

    private void Drag(ToolPointer pointer)
    {
        var raw = pointer.World - _start;
        if (pointer.IsConstrained) raw = Math.Abs(raw.X) >= Math.Abs(raw.Y) ? new(raw.X, 0) : new(0, raw.Y);
        Snapped = null;
        if (!pointer.IsSnapSuppressed)
        {
            var dragged = _selection.Items.ToHashSet();
            var result = _snap.Snap(_grab + raw, pointer.Tolerance, t => t.Source is TId id && dragged.Contains(id));
            if (result.IsSnapped)
            {
                raw = result.Point - _grab;
                Snapped = result;
            }
        }
        DragOffset = raw;
    }

    /// <summary>The point of the selection that snaps: the selection's own snap point nearest the press, else the press itself.</summary>
    private Point GrabPoint(ToolPointer pointer)
    {
        var document = _document.Current;
        return _selection.Items
            .SelectMany(id => _scene.SnapTargetsOf(document, id))
            .Where(t => t.Kind is SnapKind.Point or SnapKind.Connection)
            .Select(t => t.ClosestTo(pointer.World))
            .Where(p => p.DistanceTo(pointer.World) <= pointer.Tolerance)
            .OrderBy(p => p.DistanceTo(pointer.World))
            .Select(p => (Point?)p)
            .FirstOrDefault() ?? pointer.World;
    }

    private void Reset()
    {
        _state = State.Idle;
        _pressed = default;
        _hasPressed = false;
        DragOffset = null;
        RubberBand = null;
        Snapped = null;
    }

    private enum State
    {
        Idle,
        PressedOnItem,
        PressedOnEmpty,
        Dragging,
        RubberBand,
    }
}
