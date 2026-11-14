using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>
/// Importing end profiles into the user's catalogue (UC-2, CA-2). Each imported profile is matched with an
/// existing one, by id or else by designation and scale, and compared, so the user can choose which to keep.
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

    /// <summary>Matches and compares <paramref name="imported"/> with the profiles in <paramref name="catalogue"/>.</summary>
    public static IReadOnlyList<EndProfileImportItem> Compare(IEnumerable<EndProfile> imported, DrawingCatalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(imported);
        ArgumentNullException.ThrowIfNull(catalogue);
        return [.. imported.DistinctBy(p => p.Id).Select(profile =>
        {
            var existing = catalogue.FindEndProfile(profile.Id) ?? catalogue.FindEndProfile(profile.Designation, profile.Scale);
            var changes = existing is null ? EndProfileChanges.None : Changes(existing, profile);
            var status = existing is null ? EndProfileImportStatus.New : changes == EndProfileChanges.None ? EndProfileImportStatus.Unchanged : EndProfileImportStatus.Changed;
            return new EndProfileImportItem(profile, existing, status, changes);
        })];
    }

    /// <summary>
    /// Adds the <paramref name="chosen"/> profiles to the user's catalogue. A profile that replaces an existing
    /// one takes its id, so modules that refer to the existing profile now use the imported drawing.
    /// </summary>
    public static DrawingCatalogue Apply(DrawingCatalogue user, IEnumerable<EndProfileImportItem> chosen)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(chosen);
        var added = chosen.Where(i => i.Status != EndProfileImportStatus.Unchanged).Select(i => i.Imported with { Id = i.Existing?.Id ?? i.Imported.Id }).ToList();
        var ids = added.Select(p => p.Id).ToHashSet();
        return user with { EndProfiles = [.. user.EndProfiles.Where(p => !ids.Contains(p.Id)), .. added] };
    }

    /// <summary>What differs between two drawings of an end profile.</summary>
    public static EndProfileChanges Changes(EndProfile existing, EndProfile imported)
    {
        var changes = EndProfileChanges.None;
        if (!Same(existing.Designation, imported.Designation)) changes |= EndProfileChanges.Designation;
        if (!Same(existing.Scale, imported.Scale)) changes |= EndProfileChanges.Scale;
        if (!existing.Standards.Select(s => s.Id).ToHashSet().SetEquals(imported.Standards.Select(s => s.Id))) changes |= EndProfileChanges.Standards;
        if (EndProfileComparison.CompareTracks(existing.Tracks, imported.Tracks, Tolerance.Length) != FitDifferences.None) changes |= EndProfileChanges.Tracks;
        if (CirclesDiffer(existing.ScrewHoles, imported.ScrewHoles) || existing.ScrewHolePattern != imported.ScrewHolePattern) changes |= EndProfileChanges.ScrewHoles;
        if (CirclesDiffer(existing.Dowels, imported.Dowels)) changes |= EndProfileChanges.Dowels;
        if (PathsDiffer(existing.Outline, imported.Outline)) changes |= EndProfileChanges.Outline;
        if (PathsDiffer(existing.GroundLine, imported.GroundLine)) changes |= EndProfileChanges.GroundLine;
        if (existing.Source != imported.Source || existing.RegistryId != imported.RegistryId) changes |= EndProfileChanges.Source;
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
