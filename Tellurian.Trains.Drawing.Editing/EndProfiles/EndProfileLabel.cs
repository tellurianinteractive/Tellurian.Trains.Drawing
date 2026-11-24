using Tellurian.Trains.Schedules.Model.Geometry;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Schedules.Model.Layouts;

namespace Tellurian.Trains.Drawing.Editing.EndProfiles;

/// <summary>A label's text, centre and text height, in end-view millimetres.</summary>
public sealed record LabelPlacement(string Text, Point Center, double Size)
{
    /// <summary>The area the text is expected to cover.</summary>
    public BoundingBox Bounds
    {
        get
        {
            var half = new Vector(Text.Length * EndProfileLabel.CharacterWidth * Size / 2, Size / 2);
            return new(Center - half, Center + half);
        }
    }
}

/// <summary>
/// The title printed on an end profile drawing: standard, scale and designation, large and centred below the
/// plate and its lowest dimension.
/// </summary>
public static class EndProfileLabel
{
    /// <summary>The largest text height, mm.</summary>
    public const double MaximumSize = 30;

    /// <summary>The smallest text height, mm, used however narrow the plate is.</summary>
    public const double MinimumSize = 5;

    /// <summary>The share of the plate's width the text may take.</summary>
    public const double WidthShare = 0.7;

    /// <summary>The average width of a character relative to the text height, for a sans-serif font.</summary>
    public const double CharacterWidth = 0.6;

    /// <summary>
    /// "Standard Scale Designation", e.g. "FREMO H0 B96"; standards are separated by commas. Words of the scale and
    /// the designation that the text already has are left out, so that standard FREMO/H0-RE, scale H0 and designation
    /// "FREMO H0 B96L" give "FREMO/H0-RE B96L" (<see cref="Wording.JoinWithoutRepeatedWords"/> of the shared model).
    /// </summary>
    public static string Text(EndProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var standards = string.Join(", ", profile.Standards.Select(s => s.Name?.Trim()).Where(n => !string.IsNullOrEmpty(n)));
        return Wording.JoinWithoutRepeatedWords(standards, profile.Scale, profile.Designation);
    }

    /// <summary>
    /// The label centred below the plate and the lowest of <paramref name="dimensions"/>, as large as
    /// <see cref="WidthShare"/> of the plate's width allows, between <see cref="MinimumSize"/> and
    /// <see cref="MaximumSize"/>. Null if the profile has no outline or nothing to say.
    /// </summary>
    /// <param name="dimensionTextSize">The text height of the dimensions, mm, as their text may hang below their lines.</param>
    public static LabelPlacement? Place(EndProfile profile, IEnumerable<Dimension> dimensions, double dimensionTextSize)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(dimensions);
        var text = Text(profile);
        if (text.Length == 0 || profile.Outline is not { } outline) return null;
        var plate = outline.ToPath().Bounds;
        var size = Math.Clamp(plate.Width * WidthShare / (text.Length * CharacterWidth), MinimumSize, MaximumSize);
        var lowest = plate.Min.Y;
        foreach (var dimension in dimensions)
        {
            var (start, end) = dimension.Line();
            lowest = Math.Min(lowest, Math.Min(start.Y, end.Y) - dimensionTextSize * EndProfileSheet.DimensionTextRoom);
        }
        // Half a text height of space above the text.
        return new(text, new(plate.Center.X, lowest - size), size);
    }
}
