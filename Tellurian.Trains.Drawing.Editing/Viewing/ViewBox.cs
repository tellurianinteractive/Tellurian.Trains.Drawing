using System.Globalization;

namespace Tellurian.Trains.Drawing.Editing.Viewing;

/// <summary>An SVG <c>viewBox</c> in millimetres.</summary>
public readonly record struct ViewBox(double X, double Y, double Width, double Height)
{
    /// <summary>The value of the <c>viewBox</c> attribute, with invariant number formatting.</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{X:0.###} {Y:0.###} {Width:0.###} {Height:0.###}");
}
