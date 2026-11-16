using System.Globalization;
using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.Model.Catalogue;

namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// One physical module, drawn as one drawing with one or more variants (Registry <c>Module</c>).
/// Properties are shared by all variants; geometry is per variant.
/// </summary>
public sealed record Module
{
    public required Guid Id { get; init; }

    /// <summary>
    /// Registry <c>FullName</c>. May be empty when the module is identified by its FREMO designation
    /// (<see cref="FremoNumber"/> with the first owner's <see cref="Owner.Prefix"/>); see <see cref="ModuleValidation"/>.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The owner's FREMO module number, 1–9999 (Registry <c>FremoNumber</c>). Together with the first owner's
    /// <see cref="Owner.Prefix"/> it forms the FREMO designation, e.g. <c>SFjä 012</c>.
    /// </summary>
    public int? FremoNumber { get; init; }

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
    /// <summary>
    /// The module turns an asymmetric end profile over (German <i>Endewender</i>, Registry flag): an asymmetric
    /// feature, such as the high side of FREMO-H0-B96, is on one side of the track at one end and on the other side at the other end.
    /// </summary>
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

    /// <summary>Who saved the drawing, and the last date each of them did, newest first (<see cref="Files.DrawingRevisionLog"/>).</summary>
    public IReadOnlyList<Files.DrawingRevision> RevisionLog { get; init; } = [];

    public required IReadOnlyList<ModuleVariant> Variants { get; init; }

    /// <summary>The variant marked as default, otherwise the first.</summary>
    [JsonIgnore]
    public ModuleVariant DefaultVariant => Variants.FirstOrDefault(v => v.IsDefault) ?? Variants[0];

    /// <summary>The FREMO designation, e.g. <c>SFjä 012</c>, if both the number and the first owner's prefix are given.</summary>
    [JsonIgnore]
    public string? FremoDesignation => FremoDesignationOf(Owners.Count > 0 ? Owners[0].Prefix : null, FremoNumber);

    /// <summary>The FREMO designation and the name, whichever are given, e.g. <c>SFjä 012 Tellby</c>.</summary>
    [JsonIgnore]
    public string Title => TitleOf(Name, Owners.Count > 0 ? Owners[0].Prefix : null, FremoNumber);

    /// <summary>The FREMO designation : the prefix, a space and the number with at least three digits, as the Module Registry pads it.</summary>
    public static string? FremoDesignationOf(string? prefix, int? fremoNumber) =>
        string.IsNullOrWhiteSpace(prefix) || fremoNumber is not { } number
            ? null
            : string.Create(CultureInfo.InvariantCulture, $"{prefix.Trim()} {number:000}");

    /// <inheritdoc cref="Title"/>
    public static string TitleOf(string? name, string? prefix, int? fremoNumber) =>
        string.Join(' ', new[] { FremoDesignationOf(prefix, fremoNumber), name?.Trim() }.Where(t => !string.IsNullOrEmpty(t)));

    public override string ToString() => Title;
}
