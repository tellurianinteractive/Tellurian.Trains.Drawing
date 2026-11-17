namespace Tellurian.Trains.Drawing.Editing.Input;

/// <summary>
/// The unit lengths are shown and typed in when no unit is written: millimetres for end profiles and modules, metres
/// for halls. Lengths are always stored in millimetres.
/// </summary>
public enum LengthUnit
{
    Millimetre,
    Metre,
}

/// <summary>Conversions of <see cref="LengthUnit"/>.</summary>
public static class LengthUnits
{
    /// <summary>The millimetres in one <paramref name="unit"/>.</summary>
    public static double Millimetres(this LengthUnit unit) => unit == LengthUnit.Metre ? 1000 : 1;

    /// <summary>The unit's symbol, e.g. "m".</summary>
    public static string Symbol(this LengthUnit unit) => unit == LengthUnit.Metre ? "m" : "mm";
}
