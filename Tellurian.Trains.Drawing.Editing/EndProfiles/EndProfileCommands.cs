using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Editing.EndProfiles;

/// <summary>The commands of the end profile editor (Phase 5); each is one undoable change.</summary>
public static class EndProfileCommands
{
    public const double DefaultScrewHoleDiameter = 8;
    public const double DefaultDowelDiameter = 6;

    /// <summary>
    /// Moves items; tracks move only sideways, keeping their rail top at Y = 0. Moving the outline moves the whole
    /// plate; moving plate points reshapes its upper edge, unless the points would no longer go from left to right.
    /// </summary>
    public static IEditCommand<EndProfile> Move(IReadOnlyList<EndProfileItem> ids, Vector offset) =>
        new EditCommand<EndProfile>("Move", profile =>
        {
            var moved = profile with
            {
                Tracks = Map(profile.Tracks, EndProfileItemKind.Track, ids, t => t with { Offset = t.Offset + offset.X }),
                ScrewHoles = Map(profile.ScrewHoles, EndProfileItemKind.ScrewHole, ids, h => h with { Center = h.Center + offset }),
                Dowels = Map(profile.Dowels, EndProfileItemKind.Dowel, ids, d => d with { Center = d.Center + offset }),
                GroundLine = ids.Contains(EndProfileItem.GroundLine) ? Moved(profile.GroundLine, offset) : profile.GroundLine,
            };
            if (PlateShape.From(profile) is not { } plate)
                return moved with { Outline = ids.Contains(EndProfileItem.Outline) ? Moved(profile.Outline, offset) : profile.Outline };
            if (ids.Contains(EndProfileItem.Outline)) return plate.Moved(offset) is { } whole ? WithPlate(moved, whole) : moved;
            var shape = plate;
            foreach (var index in ids.Where(i => i.Kind == EndProfileItemKind.PlatePoint).Select(i => i.Index).Order())
                if (index < shape.UpperEdge.Count && shape.WithVertex(index, shape.UpperEdge[index] + offset) is { } changed) shape = changed;
            return ReferenceEquals(shape, plate) ? moved : WithPlate(moved, shape);
        });

    /// <summary>Sets the plate's shape: its outline and, from its upper edge, the ground line.</summary>
    public static IEditCommand<EndProfile> SetPlate(PlateShape plate) =>
        new EditCommand<EndProfile>("Change plate", profile => WithPlate(profile, plate));

    /// <summary>
    /// Sets the plate's upper edge drawn from left to right (points are sorted by X), keeping the bottom if it
    /// is below the new edge, otherwise putting it <paramref name="defaultDepth"/> below the edge's lowest point.
    /// </summary>
    public static IEditCommand<EndProfile> SetUpperEdge(IReadOnlyList<Point> points, double defaultDepth) =>
        new EditCommand<EndProfile>("Draw upper edge", profile =>
        {
            var edge = points.Count > 0 && points[^1].X < points[0].X ? points.Reverse().ToList() : points.ToList();
            if (edge.Count < 2) return profile;
            var lowest = edge.Min(p => p.Y);
            var bottom = PlateShape.From(profile)?.Bottom is { } current && current < lowest ? current : lowest - defaultDepth;
            return PlateShape.TryCreate(edge, bottom) is { } shape ? WithPlate(profile, shape) : profile;
        });

    /// <summary>Moves vertex <paramref name="index"/> of the upper edge (0 the start); a fillet's worked-out end can't be moved.</summary>
    public static IEditCommand<EndProfile> SetPlatePoint(int index, Point point) =>
        ChangePlate("Change plate point", plate => plate.WithVertex(index, point));

    /// <summary>Replaces segment <paramref name="index"/> of the upper edge, e.g. to make a line an arc or a fillet.</summary>
    public static IEditCommand<EndProfile> SetPlateSegment(int index, PlateSegment segment) =>
        ChangePlate("Change plate segment", plate => plate.WithSegment(index, segment));

    /// <summary>Sets the Y of the plate's bottom edge; refused if it isn't below the upper edge.</summary>
    public static IEditCommand<EndProfile> SetPlateBottom(double bottom) =>
        ChangePlate("Change plate bottom", plate => plate.WithBottom(bottom));

    /// <summary>
    /// Splits the segment after vertex <paramref name="index"/> in two: a line at its middle, an arc at the middle of
    /// the arc, keeping its centre. A fillet can't be split, as its end is worked out.
    /// </summary>
    public static IEditCommand<EndProfile> AddPlatePoint(int index) =>
        ChangePlate("Add plate point", plate =>
        {
            var at = Math.Clamp(index, 0, plate.Segments.Count - 1);
            var segment = plate.Segments[at];
            Point from = plate.UpperEdge[at], to = plate.UpperEdge[at + 1];
            var middle = Point.Lerp(from, to, 0.5);
            return segment.Kind switch
            {
                PlateSegmentKind.Arc when segment.Center is { } center && (middle - center).Length > Tolerance.Length =>
                    plate.WithSegmentInserted(at, PlateSegment.Arc(center, center + (middle - center).Normalized * segment.EffectiveRadius)),
                PlateSegmentKind.Fillet => null,
                _ => plate.WithSegmentInserted(at, PlateSegment.Line(middle)),
            };
        });

    private static IEditCommand<EndProfile> ChangePlate(string description, Func<PlateShape, PlateShape?> change) =>
        new EditCommand<EndProfile>(description, profile =>
            PlateShape.From(profile) is { } plate && change(plate) is { } changed ? WithPlate(profile, changed) : profile);

    private static EndProfile WithPlate(EndProfile profile, PlateShape plate) =>
        profile with { Plate = plate, Outline = plate.Outline(), GroundLine = plate.GroundLine() };

    public static IEditCommand<EndProfile> SetOutline(PathDrawing outline) =>
        // A drawn outline replaces the entered plate; the plate is read from the outline again when needed.
        new EditCommand<EndProfile>("Draw outline", profile => profile with { Outline = outline, Plate = null });

    public static IEditCommand<EndProfile> SetGroundLine(PathDrawing groundLine) =>
        new EditCommand<EndProfile>("Draw ground line", profile => profile with { GroundLine = groundLine });

    /// <summary>Adds a screw hole with the diameter of the existing ones, or the default.</summary>
    public static IEditCommand<EndProfile> AddScrewHole(Point center) =>
        new EditCommand<EndProfile>("Add screw hole", profile => profile with
        {
            ScrewHoles = [.. profile.ScrewHoles, new(center, profile.ScrewHoles.LastOrDefault()?.Diameter ?? DefaultScrewHoleDiameter)],
        });

    public static IEditCommand<EndProfile> AddDowel(Point center) =>
        new EditCommand<EndProfile>("Add dowel", profile => profile with
        {
            Dowels = [.. profile.Dowels, new(center, profile.Dowels.LastOrDefault()?.Diameter ?? DefaultDowelDiameter)],
        });

    /// <summary>Adds a track at <paramref name="offset"/> with the gauge and rail system of the existing ones, or of <paramref name="defaults"/>.</summary>
    public static IEditCommand<EndProfile> AddTrack(double offset, EndProfileTrack defaults) =>
        new EditCommand<EndProfile>("Add track", profile =>
        {
            var template = profile.Tracks.LastOrDefault() ?? defaults;
            return profile with { Tracks = [.. profile.Tracks, template with { Offset = offset, Label = null }] };
        });

    /// <summary>Deletes items; the outline and ground line are removed, tracks and holes dropped from their lists.</summary>
    public static IEditCommand<EndProfile> Delete(IReadOnlyList<EndProfileItem> ids) =>
        new EditCommand<EndProfile>("Delete", profile =>
        {
            var deleted = profile with
            {
                Tracks = Keep(profile.Tracks, EndProfileItemKind.Track, ids),
                ScrewHoles = Keep(profile.ScrewHoles, EndProfileItemKind.ScrewHole, ids),
                Dowels = Keep(profile.Dowels, EndProfileItemKind.Dowel, ids),
                Outline = ids.Contains(EndProfileItem.Outline) ? null : profile.Outline,
                Plate = ids.Contains(EndProfileItem.Outline) ? null : profile.Plate,
                GroundLine = ids.Contains(EndProfileItem.GroundLine) || ids.Contains(EndProfileItem.Outline) ? null : profile.GroundLine,
            };
            // Plate points can be deleted as long as the upper edge keeps two points.
            if (deleted.Outline is not null && ids.Any(i => i.Kind == EndProfileItemKind.PlatePoint) && PlateShape.From(profile) is { } plate)
            {
                // From the last vertex backwards, so the indexes of the remaining ones stay valid.
                var shape = plate;
                foreach (var index in ids.Where(i => i.Kind == EndProfileItemKind.PlatePoint).Select(i => i.Index).OrderDescending())
                {
                    var without = index == 0
                        ? shape.Segments.Count > 1 && shape.Segments[0].Kind != PlateSegmentKind.Fillet ? PlateShape.TryCreate(shape.UpperEdge[1], shape.Segments.Skip(1), shape.Bottom) : null
                        : index <= shape.Segments.Count && shape.Segments.Count > 1 ? shape.WithoutSegment(index - 1) : null;
                    if (without is not null) shape = without;
                }
                deleted = WithPlate(deleted, shape);
            }
            return deleted;
        });

    public static IEditCommand<EndProfile> SetTrack(int index, EndProfileTrack track) =>
        new EditCommand<EndProfile>("Change track", profile => profile with { Tracks = Replace(profile.Tracks, index, track) });

    public static IEditCommand<EndProfile> SetScrewHole(int index, Circle hole) =>
        new EditCommand<EndProfile>("Change screw hole", profile => profile with { ScrewHoles = Replace(profile.ScrewHoles, index, hole) });

    public static IEditCommand<EndProfile> SetDowel(int index, Circle dowel) =>
        new EditCommand<EndProfile>("Change dowel", profile => profile with { Dowels = Replace(profile.Dowels, index, dowel) });

    /// <summary>Changes the profile's own properties: designation, pattern, source.</summary>
    public static IEditCommand<EndProfile> SetProperties(Func<EndProfile, EndProfile> change) =>
        new EditCommand<EndProfile>("Change properties", change);

    /// <summary>Mirrors the screw holes of one side to the other, making them symmetric; existing mirrored holes aren't duplicated.</summary>
    public static IEditCommand<EndProfile> MirrorScrewHoles(double tolerance = Tolerance.Length) =>
        new EditCommand<EndProfile>("Mirror screw holes", profile =>
        {
            var holes = profile.ScrewHoles.ToList();
            foreach (var hole in profile.ScrewHoles.Where(h => Math.Abs(h.Center.X) > tolerance))
            {
                var mirrored = hole.MirroredX();
                if (!holes.Any(h => h.Center.IsCloseTo(mirrored.Center, tolerance))) holes.Add(mirrored);
            }
            return profile with { ScrewHoles = holes };
        });

    private static IReadOnlyList<T> Map<T>(IReadOnlyList<T> items, EndProfileItemKind kind, IReadOnlyList<EndProfileItem> ids, Func<T, T> change) =>
        ids.Any(i => i.Kind == kind) ? [.. items.Select((item, i) => ids.Contains(new(kind, i)) ? change(item) : item)] : items;

    private static IReadOnlyList<T> Keep<T>(IReadOnlyList<T> items, EndProfileItemKind kind, IReadOnlyList<EndProfileItem> ids) =>
        ids.Any(i => i.Kind == kind) ? [.. items.Where((_, i) => !ids.Contains(new(kind, i)))] : items;

    private static IReadOnlyList<T> Replace<T>(IReadOnlyList<T> items, int index, T item) =>
        [.. items.Select((existing, i) => i == index ? item : existing)];

    private static PathDrawing? Moved(PathDrawing? path, Vector offset) =>
        path is null ? null : PathDrawing.From(path.ToPath().Transformed(new Transform(Angle.Zero, offset)));
}
