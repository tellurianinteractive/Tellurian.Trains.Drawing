using System.Globalization;

namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>
/// Checks user catalogue entries before they are saved (CA-2), against the catalogue they will be part of.
/// An entry may replace the catalogue entry with the same id, so it isn't compared with that one.
/// </summary>
public static class CatalogueValidation
{
    public static IReadOnlyList<CatalogueProblem> Validate(ModuleStandard standard, DrawingCatalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(standard);
        ArgumentNullException.ThrowIfNull(catalogue);
        var problems = new List<CatalogueProblem>();
        if (string.IsNullOrWhiteSpace(standard.ShortName)) problems.Add(new(CatalogueProblemCode.ShortNameMissing, nameof(standard.ShortName)));
        else if (catalogue.Standards.Any(s => s.Id != standard.Id && Same(s.ShortName, standard.ShortName))) problems.Add(new(CatalogueProblemCode.ShortNameInUse, nameof(standard.ShortName)));
        if (catalogue.FindScale(standard.Scale) is null) problems.Add(new(CatalogueProblemCode.ScaleUnknown, nameof(standard.Scale)));
        Positive(standard.NormalGauge, nameof(standard.NormalGauge));
        Positive(standard.NarrowGauge, nameof(standard.NarrowGauge));
        Positive(standard.RailTopHeight, nameof(standard.RailTopHeight));
        Positive(standard.OperatorSpaceDepth, nameof(standard.OperatorSpaceDepth));
        Positive(standard.DefaultCurveRadius, nameof(standard.DefaultCurveRadius));
        Positive(standard.MinimumReverseCurveRadius, nameof(standard.MinimumReverseCurveRadius));
        if (standard.MinimumClearance < 0) problems.Add(new(CatalogueProblemCode.NotPositive, nameof(standard.MinimumClearance)));
        if (!IsHexColor(standard.TrackColor)) problems.Add(new(CatalogueProblemCode.TrackColorInvalid, nameof(standard.TrackColor)));
        OfStandard(standard.DefaultSingleTrackEndProfileId, nameof(standard.DefaultSingleTrackEndProfileId));
        OfStandard(standard.DefaultDoubleTrackEndProfileId, nameof(standard.DefaultDoubleTrackEndProfileId));
        return problems;

        void Positive(double? value, string property)
        {
            if (value is <= 0) problems.Add(new(CatalogueProblemCode.NotPositive, property));
        }

        void OfStandard(Guid? id, string property)
        {
            if (id is { } profileId && catalogue.FindEndProfile(profileId)?.Standards.Any(s => s.Id == standard.Id) != true)
                problems.Add(new(CatalogueProblemCode.EndProfileNotOfStandard, property));
        }
    }

    public static IReadOnlyList<CatalogueProblem> Validate(StandardAddOn addOn, DrawingCatalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(addOn);
        ArgumentNullException.ThrowIfNull(catalogue);
        var problems = new List<CatalogueProblem>();
        if (string.IsNullOrWhiteSpace(addOn.ShortName)) problems.Add(new(CatalogueProblemCode.ShortNameMissing, nameof(addOn.ShortName)));
        else if (catalogue.AddOns.Any(a => a.Id != addOn.Id && a.BaseStandardId == addOn.BaseStandardId && Same(a.ShortName, addOn.ShortName)))
            problems.Add(new(CatalogueProblemCode.ShortNameInUse, nameof(addOn.ShortName)));
        if (catalogue.FindStandard(addOn.BaseStandardId) is null) problems.Add(new(CatalogueProblemCode.BaseStandardUnknown, nameof(addOn.BaseStandardId)));
        return problems;
    }

    /// <summary>Validates an override; <paramref name="replacing"/> is the override it replaces when edited, if any.</summary>
    public static IReadOnlyList<CatalogueProblem> Validate(EndProfileCompatibilityOverride entry, DrawingCatalogue catalogue, EndProfileCompatibilityOverride? replacing = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(catalogue);
        var problems = new List<CatalogueProblem>();
        var first = catalogue.FindEndProfile(entry.FirstEndProfileId);
        var second = catalogue.FindEndProfile(entry.SecondEndProfileId);
        if (first is null) problems.Add(new(CatalogueProblemCode.EndProfileUnknown, nameof(entry.FirstEndProfileId)));
        if (second is null) problems.Add(new(CatalogueProblemCode.EndProfileUnknown, nameof(entry.SecondEndProfileId)));
        if (entry.FirstEndProfileId == entry.SecondEndProfileId) problems.Add(new(CatalogueProblemCode.SameEndProfile, nameof(entry.SecondEndProfileId)));
        else if (first is not null && second is not null && !Same(first.Scale, second.Scale)) problems.Add(new(CatalogueProblemCode.DifferentScales, nameof(entry.SecondEndProfileId)));
        var isSamePair = replacing?.Applies(entry.FirstEndProfileId, entry.SecondEndProfileId) == true;
        if (!isSamePair && catalogue.Overrides.Any(o => o.Applies(entry.FirstEndProfileId, entry.SecondEndProfileId)))
            problems.Add(new(CatalogueProblemCode.OverrideExists, nameof(entry.SecondEndProfileId)));
        return problems;
    }

    /// <summary>A CSS hex colour: <c>#rgb</c> or <c>#rrggbb</c>.</summary>
    public static bool IsHexColor(string? value) =>
        value is { Length: 4 or 7 } && value[0] == '#' && value[1..].All(char.IsAsciiHexDigit)
        && int.TryParse(value[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);

    private static bool Same(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
