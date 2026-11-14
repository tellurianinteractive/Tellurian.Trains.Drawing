using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Editing.Snapping;
using Tellurian.Trains.Drawing.Editing.Tools;
using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model;

namespace Tellurian.Trains.Drawing.App.Sandbox;

/// <summary>Hit testing, snap targets and moves for the sandbox shapes.</summary>
public sealed class SandboxScene : ISelectableScene<SandboxDocument, Guid>
{
    public const double IndexCellSize = 500;

    public bool TryHitTest(SandboxDocument document, Point point, double tolerance, out Guid id)
    {
        var hit = document.Shapes.LastOrDefault(s => Hits(s.Path, point, tolerance));
        id = hit?.Id ?? Guid.Empty;
        return hit is not null;
    }

    public BoundingBox BoundsOf(SandboxDocument document, Guid id) => document.Find(id)?.Path.ToPath().Bounds ?? BoundingBox.Empty;

    public SpatialIndex<Guid> Index(SandboxDocument document)
    {
        var index = new SpatialIndex<Guid>(IndexCellSize);
        foreach (var shape in document.Shapes) index.Set(shape.Id, shape.Path.ToPath().Bounds);
        return index;
    }

    public IEnumerable<SnapTarget> SnapTargetsOf(SandboxDocument document, Guid id)
    {
        if (document.Find(id) is not { } shape) yield break;
        var path = shape.Path.ToPath();
        foreach (var segment in path.Segments)
        {
            yield return new PointTarget(segment.Start, id);
            if (segment is LineSegment line) yield return new EdgeTarget(line.Start, line.End, id);
        }
        if (!path.IsClosed) yield return new PointTarget(path.End, id);
    }

    public IEditCommand<SandboxDocument> Move(IReadOnlyList<Guid> ids, Vector offset) =>
        new EditCommand<SandboxDocument>("Move", document => document with
        {
            Shapes = [.. document.Shapes.Select(s => ids.Contains(s.Id) ? s with { Path = Moved(s.Path, offset) } : s)],
        });

    public static PathDrawing Moved(PathDrawing path, Vector offset) =>
        PathDrawing.From(path.ToPath().Transformed(new Transform(Angle.Zero, offset)));

    private static bool Hits(PathDrawing path, Point point, double tolerance)
    {
        var outline = path.Tessellate();
        return (outline.IsClosed && outline.Contains(point)) || Measure.PointToPolyline(point, outline) <= tolerance;
    }
}
