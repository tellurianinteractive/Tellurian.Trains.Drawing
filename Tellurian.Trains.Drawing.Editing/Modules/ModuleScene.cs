using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Editing.Snapping;
using Tellurian.Trains.Drawing.Editing.Tools;
using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.Editing.Modules;

/// <summary>
/// What the select and move tool needs for the module editor's section joints (Phase 6): joint points on top, then
/// the joints' lines; snap targets on the joints' points and on the module's outline; and moves. A joint's points
/// move with the joint, so dragging a joint doesn't snap it to itself.
/// </summary>
public sealed class ModuleScene : ISelectableScene<ModuleFileContent, ModuleItem>
{
    public const double IndexCellSize = 500;

    public bool TryHitTest(ModuleFileContent document, Point point, double tolerance, out ModuleItem id)
    {
        var joints = document.Module.Drawing.InternalJoints;
        for (var j = joints.Count - 1; j >= 0; j--)
        {
            var points = joints[j].Points;
            for (var p = points.Count - 1; p >= 0; p--)
                if (point.DistanceTo(points[p]) <= tolerance) return Found(ModuleItem.OfPoint(j, p), out id);
        }
        for (var j = joints.Count - 1; j >= 0; j--)
            if (Measure.PointToPolyline(point, new Polyline(joints[j].Points, isClosed: false)) <= tolerance) return Found(ModuleItem.OfJoint(j), out id);
        id = default;
        return false;
    }

    public BoundingBox BoundsOf(ModuleFileContent document, ModuleItem id)
    {
        var joints = document.Module.Drawing.InternalJoints;
        if (id.Joint >= joints.Count) return BoundingBox.Empty;
        var points = joints[id.Joint].Points;
        return id.Kind == ModuleItemKind.Joint ? BoundingBox.FromPoints(points)
            : id.Point < points.Count ? new(points[id.Point], points[id.Point]) : BoundingBox.Empty;
    }

    public SpatialIndex<ModuleItem> Index(ModuleFileContent document)
    {
        var index = new SpatialIndex<ModuleItem>(IndexCellSize);
        foreach (var item in Items(document))
        {
            var bounds = BoundsOf(document, item);
            if (!bounds.IsEmpty) index.Set(item, bounds);
        }
        return index;
    }

    public IEnumerable<SnapTarget> SnapTargetsOf(ModuleFileContent document, ModuleItem id)
    {
        var joints = document.Module.Drawing.InternalJoints;
        if (id.Joint >= joints.Count) yield break;
        var points = joints[id.Joint].Points;
        if (id.Kind == ModuleItemKind.JointPoint && id.Point < points.Count) yield return new PointTarget(points[id.Point], id);
        if (id.Kind == ModuleItemKind.Joint)
            for (var p = 0; p < points.Count; p++) yield return new PointTarget(points[p], ModuleItem.OfPoint(id.Joint, p));
    }

    /// <summary>The joints' points, and the corners and straight edges of the module's outline.</summary>
    public IEnumerable<SnapTarget> AllSnapTargets(ModuleFileContent document)
    {
        foreach (var item in Items(document).Where(i => i.Kind == ModuleItemKind.JointPoint))
            foreach (var target in SnapTargetsOf(document, item)) yield return target;
        if (ModuleJoints.Outline(document) is not { } outline) yield break;
        foreach (var segment in outline.Segments)
        {
            yield return new PointTarget(segment.Start, OutlineSource);
            if (segment is LineSegment line) yield return new EdgeTarget(line.Start, line.End, OutlineSource);
        }
    }

    /// <summary>A joint point moves with itself and with its joint.</summary>
    public bool MovesWith(SnapTarget target, IReadOnlySet<ModuleItem> dragged) =>
        target.Source is ModuleItem item && (dragged.Contains(item) || dragged.Contains(ModuleItem.OfJoint(item.Joint)));

    public IEditCommand<ModuleFileContent> Move(IReadOnlyList<ModuleItem> ids, Vector offset) => ModuleJoints.Move(ids, offset);

    public static IEnumerable<ModuleItem> Items(ModuleFileContent document)
    {
        var joints = document.Module.Drawing.InternalJoints;
        for (var j = 0; j < joints.Count; j++)
        {
            yield return ModuleItem.OfJoint(j);
            for (var p = 0; p < joints[j].Points.Count; p++) yield return ModuleItem.OfPoint(j, p);
        }
    }

    /// <summary>The source of the outline's snap targets, which nothing drags.</summary>
    private const string OutlineSource = "outline";

    private static bool Found(ModuleItem item, out ModuleItem id)
    {
        id = item;
        return true;
    }
}
