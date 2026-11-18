using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Measures;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// The content of a module file (spec §6): the module with all variants, the end profiles its exits and
/// internal joints use, and the derived measures per variant, which the Module Registry can take over (spec §10).
/// </summary>
public sealed record ModuleFileContent
{
    public required Module Module { get; init; }

    /// <summary>Every end profile the module refers to, so a receiver can show and check the ends.</summary>
    public IReadOnlyList<EndProfile> EndProfiles { get; init; } = [];

    /// <summary>Written for receivers; ignored on load, where measures are derived again.</summary>
    public IReadOnlyList<VariantMeasures> Measures { get; init; } = [];

    /// <summary>
    /// The file content for <paramref name="module"/>: embeds its end profiles and derives the measures.
    /// </summary>
    /// <exception cref="KeyNotFoundException">An end profile the module refers to isn't available.</exception>
    public static ModuleFileContent Create(Module module, Func<Guid, EndProfile?> endProfiles)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(endProfiles);
        var profiles = module.Variants
            .SelectMany(v => v.Exits.Select(e => e.EndProfile).Concat(v.InternalJoints.Select(j => j.EndProfile).OfType<Catalogue.CatalogueReference>()))
            .DistinctBy(r => r.Id)
            .Select(r => endProfiles(r.Id) ?? throw new KeyNotFoundException($"End profile {r.Name} ({r.Id}) isn't available."))
            .ToList();
        return new()
        {
            Module = module,
            EndProfiles = profiles,
            Measures = [.. module.Variants.Select(v => new VariantMeasures(v.Id, ModuleMeasurement.Derive(v, endProfiles)))],
        };
    }

    /// <summary>Looks up the embedded end profiles.</summary>
    public EndProfile? FindEndProfile(Guid id) => EndProfiles.FirstOrDefault(p => p.Id == id);

    /// <summary>
    /// The content with each embedded end profile replaced by the one with the same id in <paramref name="current"/>
    /// where they differ, e.g. after the end profile was changed in the catalogue, and the measures derived again.
    /// Embedded end profiles that <paramref name="current"/> doesn't have are kept.
    /// </summary>
    /// <returns>The content, unchanged when nothing differs, and the end profiles that replaced embedded ones.</returns>
    public (ModuleFileContent Content, IReadOnlyList<EndProfile> Updated) WithCurrentEndProfiles(Func<Guid, EndProfile?> current)
    {
        ArgumentNullException.ThrowIfNull(current);
        var updated = EndProfiles
            .Select(embedded => current(embedded.Id) is { } now && !Same(now, embedded) ? now : null)
            .OfType<EndProfile>()
            .ToList();
        if (updated.Count == 0) return (this, []);
        EndProfile? Lookup(Guid id) => updated.FirstOrDefault(p => p.Id == id) ?? FindEndProfile(id) ?? current(id);
        try
        {
            return (Create(Module, Lookup), updated);
        }
        catch (KeyNotFoundException)
        {
            // An end profile the module refers to was never embedded: replace the embedded ones only.
            return (this with { EndProfiles = [.. EndProfiles.Select(p => updated.FirstOrDefault(u => u.Id == p.Id) ?? p)] }, updated);
        }
    }

    /// <summary>Whether two end profiles have the same content, as written to a file.</summary>
    private static bool Same(EndProfile a, EndProfile b) =>
        ReferenceEquals(a, b) || DrawingFileSerializer.Serialize(a) == DrawingFileSerializer.Serialize(b);
}
