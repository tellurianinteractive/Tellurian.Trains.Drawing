using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Editing.Snapping;

/// <summary>
/// Snaps points to grid, edges, points and connection points (MD-12). Targets are kept in a spatial index,
/// so only the ones near the pointer are considered. Within reach, a stronger kind wins over a weaker one
/// (connection, point, edge, grid); among targets of the same kind, the nearest wins.
/// </summary>
public sealed class SnapEngine
{
    /// <summary>The snap distance on screen, in pixels; divide by the scale for millimetres.</summary>
    public const double DefaultTolerancePixels = 8;

    private readonly SpatialIndex<SnapTarget> _index;

    public SnapEngine(double cellSize = 500) => _index = new(cellSize);

    /// <summary>The grid spacing in millimetres; null or zero turns grid snapping off.</summary>
    public double? GridSpacing { get; set; }

    /// <summary>The kinds to snap to; all by default.</summary>
    public ISet<SnapKind> EnabledKinds { get; } = new HashSet<SnapKind>(Enum.GetValues<SnapKind>().Where(k => k != SnapKind.None));

    public int Count => _index.Count;

    public void Add(SnapTarget target) => _index.Set(target, target.Bounds);

    public void AddRange(IEnumerable<SnapTarget> targets)
    {
        foreach (var target in targets) Add(target);
    }

    public bool Remove(SnapTarget target) => _index.Remove(target);

    public void Clear() => _index.Clear();

    /// <summary>
    /// Snaps <paramref name="point"/> within <paramref name="tolerance"/> millimetres, ignoring targets for which
    /// <paramref name="exclude"/> returns true, e.g. those of the items being dragged.
    /// </summary>
    public SnapResult Snap(Point point, double tolerance, Func<SnapTarget, bool>? exclude = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tolerance);
        var area = new BoundingBox(point, point);
        var best = _index.QueryNear(area, tolerance)
            .Where(t => EnabledKinds.Contains(t.Kind) && exclude?.Invoke(t) != true)
            .Select(t => (Target: t, Point: t.ClosestTo(point)))
            .Select(c => (c.Target, c.Point, Distance: c.Point.DistanceTo(point)))
            .Where(c => c.Distance <= tolerance)
            .OrderByDescending(c => c.Target.Kind)
            .ThenBy(c => c.Distance)
            .FirstOrDefault();
        if (best.Target is not null) return new(best.Point, best.Target.Kind, best.Target);

        if (EnabledKinds.Contains(SnapKind.Grid) && GridSpacing is > 0 and var spacing)
        {
            var grid = new Point(Math.Round(point.X / spacing) * spacing, Math.Round(point.Y / spacing) * spacing);
            if (grid.DistanceTo(point) <= tolerance) return new(grid, SnapKind.Grid);
        }
        return new(point, SnapKind.None);
    }
}
