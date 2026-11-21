using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Editing.Snapping;
using Tellurian.Trains.Schedules.Model.Geometry;

namespace Tellurian.Trains.Drawing.Editing.Tools;

/// <summary>
/// What <see cref="SelectMoveTool{TDocument, TId}"/> needs to know about a document: which item is where,
/// what it snaps by, and how to move items. Each editor (end profile, module, hall) implements it.
/// </summary>
public interface ISelectableScene<TDocument, TId> where TDocument : class where TId : notnull
{
    /// <summary>Finds the item at <paramref name="point"/> within <paramref name="tolerance"/>, the topmost first.</summary>
    bool TryHitTest(TDocument document, Point point, double tolerance, out TId id);

    BoundingBox BoundsOf(TDocument document, TId id);

    /// <summary>The items of <paramref name="document"/> in a spatial index, for rubber-band selection.</summary>
    SpatialIndex<TId> Index(TDocument document);

    /// <summary>The snap targets of an item, e.g. its corners and edges; used to snap other items to it.</summary>
    IEnumerable<SnapTarget> SnapTargetsOf(TDocument document, TId id);

    /// <summary>
    /// True if <paramref name="target"/> moves with the <paramref name="dragged"/> items, so a drag mustn't snap to it:
    /// by default the targets of the dragged items themselves. A scene overrides it when a target depends on
    /// other items, e.g. a wall on its corners.
    /// </summary>
    bool MovesWith(SnapTarget target, IReadOnlySet<TId> dragged) => target.Source is TId id && dragged.Contains(id);

    /// <summary>The command that moves <paramref name="ids"/> by <paramref name="offset"/>.</summary>
    IEditCommand<TDocument> Move(IReadOnlyList<TId> ids, Vector offset);
}
