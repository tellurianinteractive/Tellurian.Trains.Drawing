namespace Tellurian.Trains.Drawing.Model.Catalogues;

/// <summary>
/// Imported standards, e.g. the Module Registry's, merged into the user's catalogue: a catalogue never has two standards
/// with the same short name (decided 2026-11-26). An imported standard replaces the user's with the same id or the same
/// short name; its values take precedence, and the user's fill in only what it doesn't give. End profiles and add-ons
/// that referred to a replaced standard refer to the imported one.
/// </summary>
public static class StandardMerge
{
    /// <summary>
    /// The user's standards with <paramref name="imported"/> merged in, and the ids of the user's standards that were
    /// replaced, with the id of the standard each became.
    /// </summary>
    public static (IReadOnlyList<ModuleStandard> Standards, IReadOnlyDictionary<Guid, Guid> Moved) Merge(IEnumerable<ModuleStandard> user, IEnumerable<ModuleStandard> imported)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(imported);
        var standards = user.ToList();
        var moved = new Dictionary<Guid, Guid>();
        foreach (var standard in imported)
        {
            var same = standards.Where(s => (standard.Id is not null && s.Id == standard.Id) || Same(s.ShortName, standard.ShortName)).ToList();
            var merged = same.Aggregate(standard.Copy(), FilledFrom);
            if (standard.Id is { } id)
                foreach (var replaced in same)
                    if (replaced.Id is { } former && former != id) moved[former] = id;
            standards = [.. standards.Except(same), merged];
        }
        return (standards, moved);
    }

    /// <summary>The catalogue with the references to the <paramref name="moved"/> standards (former id, id now) moved.</summary>
    public static Catalogue WithMovedStandards(Catalogue catalogue, IReadOnlyDictionary<Guid, Guid> moved)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        ArgumentNullException.ThrowIfNull(moved);
        if (moved.Count == 0) return catalogue;
        CatalogueReference Moved(CatalogueReference reference) =>
            moved.TryGetValue(reference.Id, out var id) ? new(id, catalogue.FindStandard(id)?.ShortName ?? reference.Name) : reference;
        return catalogue with
        {
            Standards = [.. catalogue.Standards.Select(s => s.ExtendsStandardId is { } extended && moved.TryGetValue(extended, out var id) ? s.With(edited => edited.ExtendsStandardId = id) : s)],
            EndProfiles = [.. catalogue.EndProfiles.Select(p => p.Standards.Any(s => moved.ContainsKey(s.Id))
                ? p.With(edited => edited.Standards = [.. p.Standards.Select(Moved).DistinctBy(s => s.Id)])
                : p)],
            AddOns = [.. catalogue.AddOns.Select(a => moved.TryGetValue(a.BaseStandardId, out var id) ? a.With(edited => edited.BaseStandardId = id) : a)],
        };
    }

    /// <summary>
    /// <paramref name="merged"/> with what it doesn't give taken from <paramref name="user"/>. The countries are always
    /// the imported ones, since none means that the standard applies everywhere. External references of both are kept,
    /// the imported ones taking precedence.
    /// </summary>
    private static ModuleStandard FilledFrom(ModuleStandard merged, ModuleStandard user) => merged.With(m =>
    {
        m.Scale ??= user.Scale;
        m.TrackSystem ??= user.TrackSystem;
        m.RailSystem ??= user.RailSystem;
        m.Gauges ??= user.Gauges;
        m.Couplings ??= user.Couplings;
        m.Electricity ??= user.Electricity;
        m.Wheelset ??= user.Wheelset;
        m.RailTopHeight ??= user.RailTopHeight;
        m.MinimumClearance ??= user.MinimumClearance;
        m.OperatorSpaceDepth ??= user.OperatorSpaceDepth;
        m.TrackColor ??= user.TrackColor;
        m.FoulingDistance ??= user.FoulingDistance;
        m.MinimumReverseCurveRadius ??= user.MinimumReverseCurveRadius;
        m.DefaultCurveRadius ??= user.DefaultCurveRadius;
        m.DefaultSingleTrackEndProfileId ??= user.DefaultSingleTrackEndProfileId;
        m.DefaultDoubleTrackEndProfileId ??= user.DefaultDoubleTrackEndProfileId;
        m.ExtendsStandardId ??= user.ExtendsStandardId;
        m.Source ??= user.Source;
        if (user.ExternalReferences is { Count: > 0 } references)
        {
            var all = new Dictionary<string, System.Text.Json.JsonElement>(references);
            foreach (var (system, entry) in merged.ExternalReferences ?? []) all[system] = entry;
            m.ExternalReferences = all;
        }
    });

    private static bool Same(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
