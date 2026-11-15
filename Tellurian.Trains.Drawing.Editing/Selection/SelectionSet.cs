using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Editing.Selection;

/// <summary>
/// The selected items of an editor (HP-6): single selection, toggling with a modifier key, and rubber-band
/// selection. Items are identified by <typeparamref name="TId"/>; the order of selection is kept.
/// </summary>
public sealed class SelectionSet<TId> where TId : notnull
{
    private readonly List<TId> _items = [];

    /// <summary>Raised when the selection changes.</summary>
    public event EventHandler? Changed;

    public IReadOnlyList<TId> Items => _items;
    public int Count => _items.Count;
    public bool IsEmpty => _items.Count == 0;

    /// <summary>The only selected item, or default if none or several are selected.</summary>
    public TId? Single => _items.Count == 1 ? _items[0] : default;

    public bool Contains(TId id) => _items.Contains(id);

    /// <summary>Selects only <paramref name="id"/>.</summary>
    public void Select(TId id) => Set([id]);

    /// <summary>Adds <paramref name="id"/>, or removes it if it is selected (Ctrl/Cmd + click).</summary>
    public void Toggle(TId id)
    {
        if (!_items.Remove(id)) _items.Add(id);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear() => Set([]);

    /// <summary>Selects exactly <paramref name="ids"/>.</summary>
    public void Set(IEnumerable<TId> ids)
    {
        var distinct = ids.Distinct().ToList();
        if (distinct.SequenceEqual(_items)) return;
        _items.Clear();
        _items.AddRange(distinct);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Rubber-band selection: the items whose bounds lie wholly inside <paramref name="area"/>, using
    /// <paramref name="index"/> to find candidates. With <paramref name="additive"/>, they are added to the selection.
    /// </summary>
    public void SelectInside(BoundingBox area, SpatialIndex<TId> index, Func<TId, BoundingBox> boundsOf, bool additive = false)
    {
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(boundsOf);
        var inside = index.Query(area).Where(id => IsInside(boundsOf(id), area));
        Set(additive ? _items.Concat(inside) : inside);
    }

    private static bool IsInside(BoundingBox bounds, BoundingBox area) =>
        !bounds.IsEmpty && area.Contains(bounds.Min) && area.Contains(bounds.Max);
}
