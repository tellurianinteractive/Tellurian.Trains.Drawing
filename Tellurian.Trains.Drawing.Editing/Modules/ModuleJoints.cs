using static Tellurian.Trains.Schedules.Model.Layouts.LayoutPartCopying;
using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Schedules.Model.Geometry;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Editing.Modules;

/// <summary>
/// The commands that define the sections of a module (Phase 6, MD-4, MD-4a) by its section joints: polylines across
/// the module with their ends on its outline. They work on the default drawing; after each change the sections are cut
/// again from the whole outline. A change that wouldn't cut the module into sections, e.g. a joint crossing another,
/// is refused and leaves the module as it was. Internal joints always fit: nothing here compares end profiles.
/// </summary>
public static class ModuleJoints
{
    /// <summary>The whole outline of the edited drawing: its end plates and sides, or its only section; null if it has none.</summary>
    public static SegmentPath? Outline(ModuleFileContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var drawing = content.Module.Drawing;
        try
        {
            if (ModuleGeometry.Outline(drawing, content.FindEndProfile) is { } outline) return outline;
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            // Sides that don't fit their end plates; a single section can still be split.
        }
        return drawing.Sections is [var only] ? only.Outline.ToPath() : null;
    }

    /// <summary>Adds a straight joint across the module, at right angles to the track, at the point on it nearest <paramref name="at"/>.</summary>
    public static IEditCommand<ModuleFileContent> Add(Point at) =>
        Change("Add section joint", (drawing, outline, joints) =>
            ModuleSections.Across(outline, drawing.MainPath, at) is { } joint ? [.. joints, joint] : null);

    /// <summary>
    /// Divides the module into <paramref name="count"/> sections of the same length along the track, by straight joints at
    /// right angles to it; on a curve the sections also have the same angle. The joints there were are replaced, bent
    /// ones too. Refused for fewer than one section or a module without a main path.
    /// </summary>
    public static IEditCommand<ModuleFileContent> Divide(int count) =>
        Change("Divide into sections", (drawing, outline, _) =>
            drawing.MainPath is { } main ? ModuleSections.EqualSections(outline, main, count) : null);

    /// <summary>
    /// Moves joint <paramref name="joint"/> of a curved module so it crosses the track where the curve has turned
    /// <paramref name="degrees"/> from its start, as <see cref="SetDistance"/> does at that distance. Refused when the
    /// module isn't a curve (<see cref="ModuleSections.IsCurve"/>).
    /// </summary>
    public static IEditCommand<ModuleFileContent> SetAngle(int joint, double degrees) =>
        Change("Move section joint", (drawing, outline, joints) =>
            drawing.MainPath is { } main && ModuleSections.DistanceAtAngle(main, degrees) is { } distance ? MovedTo(joints, joint, main, outline, distance) : null);

    /// <summary>
    /// Moves joint <paramref name="joint"/> so it crosses the track <paramref name="distance"/> from its start: a straight
    /// joint is drawn across the track there again, a bent one is moved as a whole with its ends kept on the outline.
    /// </summary>
    public static IEditCommand<ModuleFileContent> SetDistance(int joint, double distance) =>
        Change("Move section joint", (drawing, outline, joints) =>
            drawing.MainPath is { } main ? MovedTo(joints, joint, main, outline, distance) : null);

    /// <summary>The joints with joint <paramref name="joint"/> moved to cross the track <paramref name="distance"/> from its start; null if there is no such joint.</summary>
    private static IReadOnlyList<InternalJoint>? MovedTo(IReadOnlyList<InternalJoint> joints, int joint, PathDrawing main, SegmentPath outline, double distance)
    {
        if (joint >= joints.Count) return null;
        var current = joints[joint];
        if (current.Bends is not { Count: > 0 })
            return ModuleSections.AcrossAt(outline, main, distance) is { } straight ? Replace(joints, joint, straight) : null;
        var path = main.ToPath();
        var offset = path.PoseAt(Math.Clamp(distance, 0, path.Length)).Position - path.PoseAt(ModuleSections.DistanceAlongTrack(current, main)).Position;
        return Replace(joints, joint, Moved(current, offset, outline));
    }

    /// <summary>Moves point <paramref name="point"/> of a joint; an end is kept on the module's outline.</summary>
    public static IEditCommand<ModuleFileContent> SetPoint(int joint, int point, Point at) =>
        Change("Move section joint point", (_, outline, joints) =>
            joint < joints.Count && point < joints[joint].Points.Count ? Replace(joints, joint, WithPoint(joints[joint], point, at, outline)) : null);

    /// <summary>Adds a bend in the middle of the part of a joint after point <paramref name="after"/>.</summary>
    public static IEditCommand<ModuleFileContent> InsertPoint(int joint, int after) =>
        Change("Add section joint point", (_, _, joints) =>
        {
            if (joint >= joints.Count) return null;
            var points = joints[joint].Points.ToList();
            var at = Math.Clamp(after, 0, points.Count - 2);
            points.Insert(at + 1, Point.Lerp(points[at], points[at + 1], 0.5));
            return Replace(joints, joint, WithPoints(joints[joint], points));
        });

    /// <summary>Removes bend <paramref name="point"/> of a joint; its ends can't be removed.</summary>
    public static IEditCommand<ModuleFileContent> DeletePoint(int joint, int point) =>
        Change("Delete section joint point", (_, _, joints) =>
        {
            if (joint >= joints.Count || point <= 0 || point >= joints[joint].Points.Count - 1) return null;
            var points = joints[joint].Points.ToList();
            points.RemoveAt(point);
            return Replace(joints, joint, WithPoints(joints[joint], points));
        });

    /// <summary>Deletes joints, and bends of joints that aren't deleted; the sections on either side of a deleted joint become one.</summary>
    public static IEditCommand<ModuleFileContent> Delete(IReadOnlyList<ModuleItem> items) =>
        Change("Delete", (_, _, joints) =>
        {
            var deleted = items.Where(i => i.Kind == ModuleItemKind.Joint).Select(i => i.Joint).ToHashSet();
            var kept = new List<InternalJoint>();
            for (var j = 0; j < joints.Count; j++)
            {
                if (deleted.Contains(j)) continue;
                var bends = items.Where(i => i.Kind == ModuleItemKind.JointPoint && i.Joint == j && i.Point > 0 && i.Point < joints[j].Points.Count - 1).Select(i => i.Point).ToHashSet();
                kept.Add(bends.Count == 0 ? joints[j] : WithPoints(joints[j], [.. joints[j].Points.Where((_, p) => !bends.Contains(p))]));
            }
            return kept;
        });

    /// <summary>Moves joints as a whole and joint points by <paramref name="offset"/>; ends are kept on the outline.</summary>
    public static IEditCommand<ModuleFileContent> Move(IReadOnlyList<ModuleItem> items, Vector offset) =>
        Change("Move", (_, outline, joints) =>
        {
            var moved = joints.ToList();
            foreach (var j in items.Where(i => i.Kind == ModuleItemKind.Joint).Select(i => i.Joint).Distinct().Where(j => j < moved.Count))
                moved[j] = Moved(moved[j], offset, outline);
            foreach (var item in items.Where(i => i.Kind == ModuleItemKind.JointPoint && i.Joint < moved.Count && !items.Contains(ModuleItem.OfJoint(i.Joint))))
                if (item.Point < moved[item.Joint].Points.Count)
                    moved[item.Joint] = WithPoint(moved[item.Joint], item.Point, joints[item.Joint].Points[item.Point] + offset, outline);
            return moved;
        });

    /// <summary>
    /// A command that changes the joints of the default drawing and cuts its sections again; refused, leaving the
    /// module as it was, when <paramref name="change"/> gives null or the joints wouldn't cut the module into sections.
    /// </summary>
    private static EditCommand<ModuleFileContent> Change(string description, Func<ModuleDrawing, SegmentPath, IReadOnlyList<InternalJoint>, IReadOnlyList<InternalJoint>?> change) =>
        new(description, content =>
        {
            if (Outline(content) is not { } outline) return content;
            var drawing = content.Module.Drawing;
            if (change(drawing, outline, drawing.InternalJoints) is not { } joints) return content;
            if (ModuleSections.WithJoints(drawing, outline, joints) is not { } changed) return content;
            return content with { Module = content.Module.With(c => c.Drawing = changed) };
        });

    private static InternalJoint WithPoint(InternalJoint joint, int point, Point at, SegmentPath outline)
    {
        var points = joint.Points.ToList();
        points[point] = point == 0 || point == points.Count - 1 ? ModuleSections.OnEdge(outline, at) : at;
        return WithPoints(joint, points);
    }

    private static InternalJoint Moved(InternalJoint joint, Vector offset, SegmentPath outline)
    {
        var points = joint.Points.Select(p => p + offset).ToList();
        points[0] = ModuleSections.OnEdge(outline, points[0]);
        points[^1] = ModuleSections.OnEdge(outline, points[^1]);
        return WithPoints(joint, points);
    }

    private static InternalJoint WithPoints(InternalJoint joint, IReadOnlyList<Point> points) =>
        joint.With(edited => { edited.Start = points[0]; edited.End = points[^1]; edited.Bends = points.Count > 2 ? [.. points.Skip(1).Take(points.Count - 2)] : null; });

    private static IReadOnlyList<InternalJoint> Replace(IReadOnlyList<InternalJoint> joints, int index, InternalJoint joint) =>
        [.. joints.Select((existing, i) => i == index ? joint : existing)];
}
