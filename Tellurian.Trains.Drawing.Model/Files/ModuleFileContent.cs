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
}
