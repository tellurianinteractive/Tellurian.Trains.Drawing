using System.Globalization;

namespace Tellurian.Trains.Drawing.Editing.Input;

/// <summary>
/// Parses and formats numbers in property fields (Phase 4). People type numbers the way their language
/// writes them, so both comma and point are accepted as the decimal separator, spaces as digit grouping,
/// and a unit after the number: lengths in mm, cm or m (stored in mm), angles with or without °.
/// </summary>
public static class NumberInput
{
    private static readonly char[] GroupSeparators = [' ', ' ', ' ', '\''];

    /// <summary>Parses <paramref name="text"/> as a <paramref name="quantity"/>; false if it isn't a number in an accepted unit.</summary>
    public static bool TryParse(string? text, Quantity quantity, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();
        var (number, factor) = SplitUnit(trimmed, quantity);
        if (number is null) return false;
        number = string.Concat(number.Where(c => !GroupSeparators.Contains(c))).Replace('−', '-');
        if (number.Length == 0) return false;

        // A single comma is a decimal comma. With both separators, the last one is the decimal separator.
        var lastComma = number.LastIndexOf(',');
        var lastPoint = number.LastIndexOf('.');
        if (lastComma >= 0 && lastPoint >= 0)
        {
            var decimalIndex = Math.Max(lastComma, lastPoint);
            var groupSeparator = decimalIndex == lastComma ? '.' : ',';
            number = string.Concat(number.Remove(decimalIndex, 1).Insert(decimalIndex, "#").Where(c => c != groupSeparator)).Replace('#', '.');
        }
        else
        {
            number = number.Replace(',', '.');
        }
        if (!double.TryParse(number, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed) || !double.IsFinite(parsed)) return false;
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

    private static (string? Number, double Factor) SplitUnit(string text, Quantity quantity)
    {
        var end = text.Length;
        while (end > 0 && (char.IsLetter(text[end - 1]) || text[end - 1] == '°')) end--;
        var unit = text[end..].Trim().ToLowerInvariant();
        var number = text[..end].Trim();
        double? factor = (quantity, unit) switch
        {
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
