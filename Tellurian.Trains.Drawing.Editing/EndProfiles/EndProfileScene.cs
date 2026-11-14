using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Editing.Snapping;
using Tellurian.Trains.Drawing.Editing.Tools;
using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Editing.EndProfiles;

/// <summary>
/// What the select and move tool needs for an end profile (Phase 5): hit testing with the small items
/// (screw holes, dowels, tracks) on top, snap targets, and moves. Tracks only move sideways, since their
/// rail top is Y = 0 by definition.
/// </summary>
public sealed class EndProfileScene : ISelectableScene<EndProfile, EndProfileItem>
{
    public const double IndexCellSize = 50;

    public bool TryHitTest(EndProfile document, Point point, double tolerance, out EndProfileItem id)
    {
        for (var i = document.ScrewHoles.Count - 1; i >= 0; i--)
            if (point.DistanceTo(document.ScrewHoles[i].Center) <= document.ScrewHoles[i].Diameter / 2 + tolerance) return Found(EndProfileItem.ScrewHole(i), out id);
        for (var i = document.Dowels.Count - 1; i >= 0; i--)
            if (point.DistanceTo(document.Dowels[i].Center) <= document.Dowels[i].Diameter / 2 + tolerance) return Found(EndProfileItem.Dowel(i), out id);
        for (var i = document.Tracks.Count - 1; i >= 0; i--)
            if (TrackBounds(document.Tracks[i]).Inflated(tolerance).Contains(point)) return Found(EndProfileItem.Track(i), out id);
        if (document.GroundLine is { } ground && Measure.PointToPolyline(point, ground.Tessellate()) <= tolerance) return Found(EndProfileItem.GroundLine, out id);
        if (document.Outline is { } outline)
        {
            var polyline = outline.Tessellate();
            if (Measure.PointToPolyline(point, polyline) <= tolerance || polyline.Contains(point)) return Found(EndProfileItem.Outline, out id);
        }
        id = default;
        return false;
    }

    public BoundingBox BoundsOf(EndProfile document, EndProfileItem id) => id.Kind switch
    {
        EndProfileItemKind.Track when id.Index < document.Tracks.Count => TrackBounds(document.Tracks[id.Index]),
        EndProfileItemKind.ScrewHole when id.Index < document.ScrewHoles.Count => CircleBounds(document.ScrewHoles[id.Index]),
        EndProfileItemKind.Dowel when id.Index < document.Dowels.Count => CircleBounds(document.Dowels[id.Index]),
        EndProfileItemKind.Outline when document.Outline is { } outline => outline.ToPath().Bounds,
        EndProfileItemKind.GroundLine when document.GroundLine is { } ground => ground.ToPath().Bounds,
        _ => BoundingBox.Empty,
    };

    public SpatialIndex<EndProfileItem> Index(EndProfile document)
    {
        var index = new SpatialIndex<EndProfileItem>(IndexCellSize);
        foreach (var item in Items(document))
        {
            var bounds = BoundsOf(document, item);
            if (!bounds.IsEmpty) index.Set(item, bounds);
        }
        return index;
    }

    public IEnumerable<SnapTarget> SnapTargetsOf(EndProfile document, EndProfileItem id)
    {
        switch (id.Kind)
        {
            case EndProfileItemKind.Track when id.Index < document.Tracks.Count:
                var track = document.Tracks[id.Index];
                yield return new PointTarget(new(track.Offset, 0), id);
                yield return new PointTarget(new(track.Offset - track.Gauge / 2, 0), id);
                yield return new PointTarget(new(track.Offset + track.Gauge / 2, 0), id);
                break;
            case EndProfileItemKind.ScrewHole when id.Index < document.ScrewHoles.Count:
                yield return new PointTarget(document.ScrewHoles[id.Index].Center, id);
                break;
            case EndProfileItemKind.Dowel when id.Index < document.Dowels.Count:
                yield return new PointTarget(document.Dowels[id.Index].Center, id);
                break;
            case EndProfileItemKind.Outline when document.Outline is { } outline:
                foreach (var target in PathTargets(outline, id)) yield return target;
                break;
            case EndProfileItemKind.GroundLine when document.GroundLine is { } ground:
                foreach (var target in PathTargets(ground, id)) yield return target;
                break;
        }
    }

    /// <summary>Every snap target of the drawing, plus the rail top line and the centre line.</summary>
    public IEnumerable<SnapTarget> AllSnapTargets(EndProfile document)
    {
        var width = Math.Max(document.Outline?.ToPath().Bounds.Width ?? 0, 1000);
        yield return new EdgeTarget(new(-width, 0), new(width, 0), "railTop");
        yield return new PointTarget(Point.Origin, "centre");
        foreach (var item in Items(document))
            foreach (var target in SnapTargetsOf(document, item)) yield return target;
    }

    public IEditCommand<EndProfile> Move(IReadOnlyList<EndProfileItem> ids, Vector offset) =>
        EndProfileCommands.Move(ids, offset);

    public static IEnumerable<EndProfileItem> Items(EndProfile document)
    {
        if (document.Outline is not null) yield return EndProfileItem.Outline;
        if (document.GroundLine is not null) yield return EndProfileItem.GroundLine;
        for (var i = 0; i < document.Tracks.Count; i++) yield return EndProfileItem.Track(i);
        for (var i = 0; i < document.ScrewHoles.Count; i++) yield return EndProfileItem.ScrewHole(i);
        for (var i = 0; i < document.Dowels.Count; i++) yield return EndProfileItem.Dowel(i);
    }

    /// <summary>The area a track is drawn in: the gauge wide, a little above and below rail top.</summary>
    public static BoundingBox TrackBounds(EndProfileTrack track)
    {
        var height = track.Gauge * 0.3;
        return new(new(track.Offset - track.Gauge / 2 - track.Gauge * 0.1, -height), new(track.Offset + track.Gauge / 2 + track.Gauge * 0.1, height));
    }

    private static BoundingBox CircleBounds(Circle circle) =>
        new BoundingBox(circle.Center, circle.Center).Inflated(circle.Diameter / 2);

    private static IEnumerable<SnapTarget> PathTargets(PathDrawing drawing, EndProfileItem id)
    {
        var path = drawing.ToPath();
        foreach (var segment in path.Segments)
        {
            yield return new PointTarget(segment.Start, id);
            if (segment is LineSegment line) yield return new EdgeTarget(line.Start, line.End, id);
        }
        if (!path.IsClosed) yield return new PointTarget(path.End, id);
    }

    private static bool Found(EndProfileItem item, out EndProfileItem id)
    {
        id = item;
        return true;
    }
}
