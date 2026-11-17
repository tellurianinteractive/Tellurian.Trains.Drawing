using System.Globalization;

namespace Tellurian.Trains.Drawing.Editing.Input;

/// <summary>
/// Parses and formats numbers in property fields (Phase 4) the way the user's culture writes them: its decimal
/// separator and its digit grouping, e.g. <c>1 200,5</c> in Swedish, <c>1.200,5</c> in German and <c>1,200.5</c> in
/// English. Spaces and apostrophes are accepted as grouping in every culture. A grouping separator must separate
/// groups of three digits, so <c>12.5</c> in German or <c>1,5</c> in English is refused rather than read as 125 or 15.
/// A unit may follow the number: lengths in mm, cm or m (stored in mm), angles with or without °.
/// </summary>
public static class NumberInput
{
    /// <summary>Grouping separators accepted in every culture besides spaces: apostrophes, as in Swiss numbers.</summary>
    private static readonly char[] Apostrophes = ['\'', '’'];

    /// <summary>
    /// Parses <paramref name="text"/> as a <paramref name="quantity"/> written in <paramref name="culture"/>; false if
    /// it isn't a number in that culture or not in an accepted unit. A length is returned in millimetres; without a
    /// unit written, it is in <paramref name="lengthUnit"/>.
    /// </summary>
    public static bool TryParse(string? text, Quantity quantity, CultureInfo culture, out double value, LengthUnit lengthUnit = LengthUnit.Millimetre)
    {
        ArgumentNullException.ThrowIfNull(culture);
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var (number, factor) = SplitUnit(text.Trim(), quantity, lengthUnit);
        if (number is null) return false;
        // Spaces of every kind (Swedish groups with a no-break space) and apostrophes only group digits.
        number = string.Concat(number.Where(c => !char.IsWhiteSpace(c) && !Apostrophes.Contains(c))).Replace('−', '-');
        if (number.Length == 0) return false;

        var format = culture.NumberFormat;
        var decimalSeparator = format.NumberDecimalSeparator;
        var groupSeparator = string.IsNullOrWhiteSpace(format.NumberGroupSeparator) || Apostrophes.Contains(format.NumberGroupSeparator[0]) ? null : format.NumberGroupSeparator;

        var negative = number.StartsWith('-');
        if (negative || number.StartsWith('+')) number = number[1..];
        var decimalIndex = number.IndexOf(decimalSeparator, StringComparison.Ordinal);
        var integerPart = decimalIndex < 0 ? number : number[..decimalIndex];
        var fraction = decimalIndex < 0 ? string.Empty : number[(decimalIndex + decimalSeparator.Length)..];
        if (groupSeparator is not null && integerPart.Contains(groupSeparator, StringComparison.Ordinal))
        {
            var groups = integerPart.Split(groupSeparator);
            if (groups[0].Length is < 1 or > 3 || groups.Skip(1).Any(g => g.Length != 3)) return false;
            integerPart = string.Concat(groups);
        }
        if (integerPart.Length == 0 && fraction.Length == 0) return false;
        if (!integerPart.All(char.IsAsciiDigit) || !fraction.All(char.IsAsciiDigit)) return false;
        var invariant = $"{(negative ? "-" : "")}{(integerPart.Length == 0 ? "0" : integerPart)}{(fraction.Length == 0 ? "" : "." + fraction)}";
        if (!double.TryParse(invariant, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed) || !double.IsFinite(parsed)) return false;
        value = parsed * factor;
        return true;
    }

    /// <summary>Formats a value for a field in <paramref name="culture"/>, with at most <paramref name="decimals"/> decimals and no grouping.</summary>
    public static string Format(double value, int decimals, CultureInfo culture)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(decimals);
        var rounded = Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        if (rounded == 0) rounded = 0; // never "-0"
        return rounded.ToString($"0.{new string('#', decimals)}", culture);
    }

    private static (string? Number, double Factor) SplitUnit(string text, Quantity quantity, LengthUnit lengthUnit)
    {
        var end = text.Length;
        while (end > 0 && (char.IsLetter(text[end - 1]) || text[end - 1] == '°')) end--;
        var unit = text[end..].Trim().ToLowerInvariant();
        var number = text[..end].Trim();
        double? factor = (quantity, unit) switch
        {
            (Quantity.Length, "") => lengthUnit.Millimetres(),
            (_, "") => 1,
            (Quantity.Length, "mm") => 1,
            (Quantity.Length, "cm") => 10,
            (Quantity.Length, "m") => 1000,
            (Quantity.Angle, "°" or "deg") => 1,
            _ => null,
        };
        return factor is null ? (null, 0) : (number, factor.Value);
    }
}
