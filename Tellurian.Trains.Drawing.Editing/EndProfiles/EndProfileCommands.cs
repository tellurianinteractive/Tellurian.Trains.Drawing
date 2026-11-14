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

    /// <summary>Moves items; tracks move only sideways, keeping their rail top at Y = 0.</summary>
    public static IEditCommand<EndProfile> Move(IReadOnlyList<EndProfileItem> ids, Vector offset) =>
        new EditCommand<EndProfile>("Move", profile => profile with
        {
            Tracks = Map(profile.Tracks, EndProfileItemKind.Track, ids, t => t with { Offset = t.Offset + offset.X }),
            ScrewHoles = Map(profile.ScrewHoles, EndProfileItemKind.ScrewHole, ids, h => h with { Center = h.Center + offset }),
            Dowels = Map(profile.Dowels, EndProfileItemKind.Dowel, ids, d => d with { Center = d.Center + offset }),
            Outline = ids.Contains(EndProfileItem.Outline) ? Moved(profile.Outline, offset) : profile.Outline,
            GroundLine = ids.Contains(EndProfileItem.GroundLine) ? Moved(profile.GroundLine, offset) : profile.GroundLine,
        });

    public static IEditCommand<EndProfile> SetOutline(PathDrawing outline) =>
        new EditCommand<EndProfile>("Draw outline", profile => profile with { Outline = outline });

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
        new EditCommand<EndProfile>("Delete", profile => profile with
        {
            Tracks = Keep(profile.Tracks, EndProfileItemKind.Track, ids),
            ScrewHoles = Keep(profile.ScrewHoles, EndProfileItemKind.ScrewHole, ids),
            Dowels = Keep(profile.Dowels, EndProfileItemKind.Dowel, ids),
            Outline = ids.Contains(EndProfileItem.Outline) ? null : profile.Outline,
            GroundLine = ids.Contains(EndProfileItem.GroundLine) ? null : profile.GroundLine,
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
