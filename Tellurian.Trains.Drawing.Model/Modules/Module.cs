using Tellurian.Trains.Drawing.Model.Catalogue;

namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// One physical module, drawn as one drawing with one or more variants (Registry <c>Module</c>).
/// Properties are shared by all variants; geometry is per variant.
/// </summary>
public sealed record Module
{
    public required Guid Id { get; init; }

    /// <summary>Registry <c>FullName</c>.</summary>
    public required string Name { get; init; }

    public required CatalogueReference Standard { get; init; }

    /// <summary>The short name of the scale, from the standard.</summary>
    public required string Scale { get; init; }

    /// <summary>The add-ons of the standard that the module meets.</summary>
    public IReadOnlyList<CatalogueReference> AddOns { get; init; } = [];

    public IReadOnlyList<Owner> Owners { get; init; } = [];
    public string? Theme { get; init; }
    public int? RepresentsFromYear { get; init; }
    public int? RepresentsUptoYear { get; init; }

    /// <summary>As entered; compared with the drawing.</summary>
    public int? NumberOfThroughTracks { get; init; }

    public bool HasNormalGauge { get; init; } = true;
    public bool HasNarrowGauge { get; init; }
    public bool IsDuckunder { get; init; }
    public bool IsTurntable { get; init; }
    public bool IsEndOfLine { get; init; }
    public bool IsEndProfileSwitcher { get; init; }
    public bool IsSignalModule { get; init; }
    public bool IsStandAlone { get; init; }

    /// <summary>Registry functional state code.</summary>
    public int? FunctionalState { get; init; }

    /// <summary>Registry landscape state code.</summary>
    public int? LandscapeState { get; init; }

    public string? Note { get; init; }
    public string? PackageLabel { get; init; }

    /// <summary>Incremented on every save.</summary>
    public int Revision { get; init; }

    public required IReadOnlyList<ModuleVariant> Variants { get; init; }

    /// <summary>The variant marked as default, otherwise the first.</summary>
    public ModuleVariant DefaultVariant => Variants.FirstOrDefault(v => v.IsDefault) ?? Variants[0];

    public override string ToString() => Name;
}
