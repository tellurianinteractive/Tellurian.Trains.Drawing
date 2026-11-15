namespace Tellurian.Trains.Drawing.Editing.Input;

/// <summary>The kind of value a numeric field holds, which decides the units it accepts.</summary>
public enum Quantity
{
    /// <summary>A plain number, e.g. a count.</summary>
    Number,

    /// <summary>A length in millimetres; "m" and "cm" are converted.</summary>
    Length,

    /// <summary>An angle in degrees.</summary>
    Angle,
}
