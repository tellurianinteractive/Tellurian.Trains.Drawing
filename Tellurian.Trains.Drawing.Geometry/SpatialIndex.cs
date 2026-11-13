namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>
/// A uniform-grid spatial index of items by bounding box. It finds the candidates for distance
/// and overlap checks quickly (spec NF-2), so only nearby outlines are compared exactly.
/// </summary>
public sealed class SpatialIndex<T> where T : notnull
{
    private readonly Dictionary<(int X, int Y), List<T>> _cells = [];
    private readonly Dictionary<T, BoundingBox> _items = [];

    /// <param name="cellSize">The grid cell size in millimetres; about the size of a typical item works well.</param>
    public SpatialIndex(double cellSize)
    {
        if (cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "The cell size must be positive.");
        CellSize = cellSize;
    }

    public double CellSize { get; }
    public int Count => _items.Count;

    public bool Contains(T item) => _items.ContainsKey(item);

    /// <summary>Adds <paramref name="item"/>, or moves it if it is already in the index.</summary>
    public void Set(T item, BoundingBox bounds)
    {
        if (bounds.IsEmpty) throw new ArgumentException("An item needs a non-empty bounding box.", nameof(bounds));
        Remove(item);
        _items[item] = bounds;
        foreach (var cell in CellsOf(bounds))
        {
            if (!_cells.TryGetValue(cell, out var list)) _cells[cell] = list = [];
            list.Add(item);
        }
    }

    public bool Remove(T item)
    {
        if (!_items.Remove(item, out var bounds)) return false;
        foreach (var cell in CellsOf(bounds))
        {
            if (_cells.TryGetValue(cell, out var list) && list.Remove(item) && list.Count == 0) _cells.Remove(cell);
        }
        return true;
    }

    public void Clear()
    {
        _cells.Clear();
        _items.Clear();
    }

    /// <summary>The items whose bounding boxes intersect <paramref name="area"/>.</summary>
    public IReadOnlyList<T> Query(BoundingBox area)
    {
        if (area.IsEmpty) return [];
        var found = new HashSet<T>();
        var result = new List<T>();
        foreach (var cell in CellsOf(area))
        {
            if (!_cells.TryGetValue(cell, out var list)) continue;
            foreach (var item in list)
            {
                if (found.Add(item) && _items[item].Intersects(area)) result.Add(item);
            }
        }
        return result;
    }

    /// <summary>The items whose bounding boxes are within <paramref name="distance"/> of <paramref name="area"/>.</summary>
    public IReadOnlyList<T> QueryNear(BoundingBox area, double distance) =>
        [.. Query(area.Inflated(distance)).Where(item => Measure.BoxDistance(_items[item], area) <= distance)];

    private IEnumerable<(int X, int Y)> CellsOf(BoundingBox bounds)
    {
        var minX = Cell(bounds.Min.X);
        var minY = Cell(bounds.Min.Y);
        var maxX = Cell(bounds.Max.X);
        var maxY = Cell(bounds.Max.Y);
        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++) yield return (x, y);
        }
    }

    private int Cell(double coordinate) => (int)Math.Floor(coordinate / CellSize);
}
