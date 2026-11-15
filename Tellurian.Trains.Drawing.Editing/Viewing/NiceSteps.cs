namespace Tellurian.Trains.Drawing.Editing.Viewing;

/// <summary>Round lengths (1, 2 or 5 × 10ⁿ mm) for grids and scale bars (AP-4).</summary>
public static class NiceSteps
{
    /// <summary>Grid lines are at least this many pixels apart.</summary>
    public const double MinimumGridPixels = 12;

    /// <summary>A scale bar is at most this many pixels long.</summary>
    public const double ScaleBarPixels = 120;

    /// <summary>The smallest round length that is at least <paramref name="minimum"/>.</summary>
    public static double AtLeast(double minimum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimum);
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(minimum)));
        foreach (var factor in new[] { 1.0, 2.0, 5.0, 10.0 })
        {
            if (factor * magnitude >= minimum * (1 - 1e-12)) return factor * magnitude;
        }
        return 10 * magnitude;
    }

    /// <summary>The largest round length that is at most <paramref name="maximum"/>.</summary>
    public static double AtMost(double maximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximum);
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(maximum)));
        foreach (var factor in new[] { 5.0, 2.0, 1.0 })
        {
            if (factor * magnitude <= maximum * (1 + 1e-12)) return factor * magnitude;
        }
        return magnitude;
    }

    /// <summary>The grid spacing in millimetres at <paramref name="scale"/> pixels per millimetre.</summary>
    public static double GridSpacing(double scale) => AtLeast(MinimumGridPixels / scale);

    /// <summary>The length of the scale bar in millimetres at <paramref name="scale"/> pixels per millimetre.</summary>
    public static double ScaleBarLength(double scale) => AtMost(ScaleBarPixels / scale);
}
