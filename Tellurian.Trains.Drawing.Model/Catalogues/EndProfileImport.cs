using Tellurian.Trains.Schedules.Model.Geometry;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.Model.Catalogues;

/// <summary>
/// Importing into the user's catalogue (UC-2, CA-2, CA-3). Each imported end profile is matched with an existing one,
/// by id or else by designation and scale, and compared, so the user can choose which to keep. The other entries of a
/// catalogue file, its scales, standards, add-ons and overrides, are taken in as they are (<see cref="Entries"/>).
/// </summary>
public static class EndProfileImport
{
    /// <summary>The kinds of files that contain end profiles: end profile files, catalogues and modules (which embed theirs).</summary>
    public static IReadOnlyCollection<DrawingFileKind> Kinds { get; } = [DrawingFileKind.EndProfile, DrawingFileKind.Catalogue, DrawingFileKind.Module];

    /// <summary>Reads the end profiles in imported files; files whose content can't be read are reported.</summary>
    public static (IReadOnlyList<EndProfile> Profiles, IReadOnlyList<ImportProblem> Problems) Read(IEnumerable<ImportedFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var profiles = new List<EndProfile>();
        var problems = new List<ImportProblem>();
        foreach (var file in files)
        {
            try
            {
                profiles.AddRange(file.Header.Kind switch
                {
                    DrawingFileKind.EndProfile => [DrawingFileSerializer.DeserializeEndProfile(file.Json)],
                    DrawingFileKind.Catalogue => DrawingFileSerializer.DeserializeCatalogue(file.Json).EndProfiles,
                    DrawingFileKind.Module => DrawingFileSerializer.DeserializeModule(file.Json).EndProfiles,
                    _ => [],
                });
            }
            catch (DrawingFileException ex)
            {
                problems.Add(new(file.Name, ex.Message));
            }
        }
        return (profiles, problems);
    }

    /// <summary>
    /// The other entries of the catalogue files among <paramref name="files"/>: their scales, standards, add-ons and
    /// overrides, without end profiles, which the user chooses one by one (<see cref="Compare"/>). Several catalogue
    /// files are taken in order, a later entry replacing an earlier one with the same id. Files that can't be read are
    /// left out here; <see cref="Read"/> reports them.
    /// </summary>
    public static Catalogue Entries(IEnumerable<ImportedFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var entries = Catalogue.Empty;
        foreach (var file in files.Where(f => f.Header.Kind == DrawingFileKind.Catalogue))
        {
            try
            {
                entries = Catalogue.Merge(entries, DrawingFileSerializer.DeserializeCatalogue(file.Json) with { EndProfiles = [] });
            }
            catch (DrawingFileException)
            {
                // Reported by Read.
            }
        }
        return entries;
    }

    /// <summary>
    /// The user's catalogue with the imported <paramref name="entries"/> (<see cref="Entries"/>) taken in, an entry with
    /// a known id replacing the user's (decision 12), and then the <paramref name="chosen"/> end profiles (<see cref="Apply"/>).
    /// </summary>
    public static Catalogue Apply(Catalogue user, Catalogue entries, IEnumerable<EndProfileImportItem> chosen)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(entries);
        var merged = Catalogue.Merge(user, entries with { EndProfiles = [] }) with { Version = user.Version };
        return Apply(merged, chosen);
    }

    /// <summary>Matches and compares <paramref name="imported"/> with the profiles in <paramref name="catalogue"/>.</summary>
    public static IReadOnlyList<EndProfileImportItem> Compare(IEnumerable<EndProfile> imported, Catalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(imported);
        ArgumentNullException.ThrowIfNull(catalogue);
        return [.. imported.DistinctBy(p => p.Id).Select(imported =>
        {
            var existing = catalogue.FindEndProfile(imported.Id) ?? catalogue.FindEndProfile(imported.Designation, imported.Scale);
            if (existing is not null && existing.HasOutline() && !imported.HasOutline())
            {
                if (existing.Id != imported.Id)
                {
                    // The same designation and scale, but another id: the same end profile only if the standard is the same too.
                    if (!HaveTheSameStandard(existing, imported)) return new EndProfileImportItem(imported, null, EndProfileImportStatus.New, EndProfileChanges.None);
                    var merged = Merged(existing, imported);
                    return new EndProfileImportItem(merged, existing, EndProfileImportStatus.Merged, Changes(existing, merged));
                }
                var kept = KeepingDrawing(existing, imported);
                var keptChanges = Changes(existing, kept);
                return new EndProfileImportItem(kept, existing, keptChanges == EndProfileChanges.None ? EndProfileImportStatus.Unchanged : EndProfileImportStatus.Changed, keptChanges);
            }
            var changes = existing is null ? EndProfileChanges.None : Changes(existing, imported);
            var status = existing is null ? EndProfileImportStatus.New : changes == EndProfileChanges.None ? EndProfileImportStatus.Unchanged : EndProfileImportStatus.Changed;
            return new EndProfileImportItem(imported, existing, status, changes);
        })];
    }

    /// <summary>
    /// An imported end profile without a shape, such as one the Module Registry has no drawing of, for an end profile
    /// that is drawn in the catalogue: the existing drawing with the imported designation, scale and standards, and the
    /// imported external references added to the existing ones. Importing never replaces a drawing with no drawing.
    /// </summary>
    public static EndProfile KeepingDrawing(EndProfile existing, EndProfile imported)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(imported);
        return existing.With(p =>
        {
            p.Designation = imported.Designation;
            p.Scale = imported.Scale;
            if (imported.Standards.Count > 0) p.Standards = [.. imported.Standards];
            if (imported.ExternalReferences is { Count: > 0 } references)
            {
                var merged = existing.ExternalReferences is null ? [] : new Dictionary<string, System.Text.Json.JsonElement>(existing.ExternalReferences);
                foreach (var (system, entry) in references) merged[system] = entry;
                p.ExternalReferences = merged;
            }
        });
    }

    /// <summary>
    /// A drawn end profile of the catalogue and an imported one without a shape with the same designation, scale and
    /// standard, but another id, as one end profile: the imported one, e.g. the Module Registry's, with the existing
    /// drawing. It has the imported id, so it is the same end profile as everyone else's from the same source, the
    /// standards of both, and the external references of both, the imported ones taking precedence (decided 2026-11-26).
    /// </summary>
    public static EndProfile Merged(EndProfile existing, EndProfile imported)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(imported);
        return KeepingDrawing(existing, imported).With(p =>
        {
            p.Id = imported.Id;
            p.Standards = [.. imported.Standards.Concat(existing.Standards).DistinctBy(s => s.Id)];
        });
    }

    /// <summary>
    /// Whether two end profiles are of the same standard: they share one, by id or by short name, since catalogues
    /// from different sources give the same standard different ids. A profile without standards matches any.
    /// </summary>
    public static bool HaveTheSameStandard(EndProfile one, EndProfile other)
    {
        ArgumentNullException.ThrowIfNull(one);
        ArgumentNullException.ThrowIfNull(other);
        return one.Standards.Count == 0 || other.Standards.Count == 0 ||
            one.Standards.Any(a => other.Standards.Any(b => a.Id == b.Id || Same(a.Name, b.Name)));
    }

    /// <summary>
    /// Adds the <paramref name="chosen"/> profiles to the user's catalogue. A profile that replaces an existing
    /// one takes its id, so modules that refer to the existing profile now use the imported drawing. A merged profile
    /// (<see cref="EndProfileImportStatus.Merged"/>) instead keeps the imported id, and the catalogue's overrides and
    /// standards that referred to the existing id refer to it (<see cref="Merges"/>).
    /// </summary>
    public static Catalogue Apply(Catalogue user, IEnumerable<EndProfileImportItem> chosen)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(chosen);
        var items = chosen.Where(i => i.Status != EndProfileImportStatus.Unchanged).ToList();
        var added = items.Select(i => i.Status == EndProfileImportStatus.Merged ? i.Imported : i.Imported.With(p => p.Id = i.Existing?.Id ?? i.Imported.Id)).ToList();
        var merges = Merges(items);
        var removed = added.Select(p => p.Id).Concat(merges.Keys).ToHashSet();
        Guid Moved(Guid id) => merges.GetValueOrDefault(id, id);
        Guid? MovedOrNull(Guid? id) => id is { } value ? Moved(value) : null;
        return user with
        {
            EndProfiles = [.. user.EndProfiles.Where(p => !removed.Contains(p.Id)), .. added],
            Overrides = merges.Count == 0 ? user.Overrides : [.. user.Overrides.Select(o => o with { FirstEndProfileId = Moved(o.FirstEndProfileId), SecondEndProfileId = Moved(o.SecondEndProfileId) })],
            Standards = merges.Count == 0 ? user.Standards : [.. user.Standards.Select(s => s.With(edited =>
            {
                edited.DefaultSingleTrackEndProfileId = MovedOrNull(s.DefaultSingleTrackEndProfileId);
                edited.DefaultDoubleTrackEndProfileId = MovedOrNull(s.DefaultDoubleTrackEndProfileId);
            }))],
        };
    }

    /// <summary>The ids that merged end profiles had in the catalogue, with the id each has now.</summary>
    public static IReadOnlyDictionary<Guid, Guid> Merges(IEnumerable<EndProfileImportItem> chosen)
    {
        ArgumentNullException.ThrowIfNull(chosen);
        return chosen.Where(i => i.Status == EndProfileImportStatus.Merged && i.Existing is not null && i.Existing.Id != i.Imported.Id)
            .DistinctBy(i => i.Existing!.Id)
            .ToDictionary(i => i.Existing!.Id, i => i.Imported.Id);
    }

    /// <summary>What differs between two drawings of an end profile.</summary>
    public static EndProfileChanges Changes(EndProfile existing, EndProfile imported)
    {
        var changes = EndProfileChanges.None;
        if (!Same(existing.Designation, imported.Designation) || !Same(existing.ShortNameOrDesignation, imported.ShortNameOrDesignation)) changes |= EndProfileChanges.Designation;
        if (!Same(existing.Scale, imported.Scale)) changes |= EndProfileChanges.Scale;
        if (!existing.Standards.Select(s => s.Id).ToHashSet().SetEquals(imported.Standards.Select(s => s.Id))) changes |= EndProfileChanges.Standards;
        if (EndProfileComparison.CompareTracks(existing.Tracks, imported.Tracks, Tolerance.Length) != FitDifferences.None) changes |= EndProfileChanges.Tracks;
        if (CirclesDiffer(existing.ScrewHoles, imported.ScrewHoles) || existing.ScrewHolePattern != imported.ScrewHolePattern) changes |= EndProfileChanges.ScrewHoles;
        if (CirclesDiffer(existing.Dowels, imported.Dowels)) changes |= EndProfileChanges.Dowels;
        if (PathsDiffer(existing.Outline, imported.Outline)) changes |= EndProfileChanges.Outline;
        if (PathsDiffer(existing.GroundLine, imported.GroundLine)) changes |= EndProfileChanges.GroundLine;
        if (existing.Source != imported.Source || !ExternalReferences.Same(existing.ExternalReferences, imported.ExternalReferences)) changes |= EndProfileChanges.Source;
        return changes;
    }

    private static bool CirclesDiffer(IReadOnlyList<Circle> a, IReadOnlyList<Circle> b) =>
        a.Count != b.Count
        || a.Zip(b).Any(p => Math.Abs(p.First.Diameter - p.Second.Diameter) > Tolerance.Length)
        || !EndProfileComparison.MatchCircles(a, b, Tolerance.Length).IsComplete;

    private static bool PathsDiffer(PathDrawing? a, PathDrawing? b)
    {
        if (a is null || b is null) return a is not null || b is not null;
        var pa = a.Tessellate().Points;
        var pb = b.Tessellate().Points;
        return pa.Count != pb.Count || pa.Zip(pb).Any(p => !p.First.IsCloseTo(p.Second));
    }

    private static bool Same(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
