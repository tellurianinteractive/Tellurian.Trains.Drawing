namespace Tellurian.Trains.Drawing.Model.Catalogues;

/// <summary>
/// Moves a module to end profiles that were merged in the catalogue (<see cref="EndProfileImportStatus.Merged"/>): a
/// drawn profile that became the Module Registry's took the Registry's id, so a module drawn before refers to an id the
/// catalogue no longer has. Its exits and internal joints are given the new id, and its embedded profile the merged one.
/// </summary>
public static class EndProfileMerges
{
    /// <summary>
    /// The content with the end profiles of <paramref name="merges"/> (former id, id now) replaced by those that
    /// <paramref name="catalogue"/> has with the new ids; unchanged when the module uses none of them.
    /// </summary>
    /// <returns>The content and the end profiles the module was moved to.</returns>
    public static (ModuleFileContent Content, IReadOnlyList<EndProfile> Moved) WithMergedEndProfiles(
        this ModuleFileContent content, IReadOnlyDictionary<Guid, Guid> merges, Func<Guid, EndProfile?> catalogue)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(merges);
        ArgumentNullException.ThrowIfNull(catalogue);
        if (merges.Count == 0) return (content, []);

        var drawing = content.Module.Drawing;
        var moved = drawing.Exits.Select(e => e.EndProfile)
            .Concat(drawing.InternalJoints.Select(j => j.EndProfile).OfType<CatalogueReference>())
            .Select(r => merges.TryGetValue(r.Id, out var id) ? catalogue(id) : null)
            .OfType<EndProfile>()
            .DistinctBy(p => p.Id)
            .ToDictionary(p => p.Id);
        if (moved.Count == 0) return (content, []);

        CatalogueReference? Moved(CatalogueReference? reference) =>
            reference is not null && merges.TryGetValue(reference.Id, out var id) && moved.TryGetValue(id, out var profile)
                ? new CatalogueReference(profile.Id, profile.Designation)
                : null;
        var module = content.Module.With(m => m.Drawing = drawing.With(d =>
        {
            d.Exits = [.. drawing.Exits.Select(e => Moved(e.EndProfile) is { } reference ? e.With(x => x.EndProfile = reference) : e)];
            d.InternalJoints = [.. drawing.InternalJoints.Select(j => Moved(j.EndProfile) is { } reference ? j.With(x => x.EndProfile = reference) : j)];
        }));
        EndProfile? Lookup(Guid id) => moved.GetValueOrDefault(id) ?? content.FindEndProfile(id) ?? catalogue(id);
        try
        {
            return (ModuleFileContent.Create(module, Lookup), [.. moved.Values]);
        }
        catch (KeyNotFoundException)
        {
            // An end profile the module refers to was never embedded: replace the merged ones only.
            var embedded = content.EndProfiles.Select(p => merges.TryGetValue(p.Id, out var id) && moved.TryGetValue(id, out var profile) ? profile : p);
            return (content with { Module = module, EndProfiles = [.. embedded.DistinctBy(p => p.Id)] }, [.. moved.Values]);
        }
    }
}
