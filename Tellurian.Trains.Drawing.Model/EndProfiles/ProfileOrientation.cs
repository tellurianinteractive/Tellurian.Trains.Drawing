namespace Tellurian.Trains.Drawing.Model.EndProfiles;

/// <summary>
/// How an end profile is fitted at an exit. Only meaningful for asymmetric profiles:
/// <see cref="Mirrored"/> means the module end is built as the mirror image of the drawing.
/// </summary>
public enum ProfileOrientation
{
    Normal,
    Mirrored,
}
