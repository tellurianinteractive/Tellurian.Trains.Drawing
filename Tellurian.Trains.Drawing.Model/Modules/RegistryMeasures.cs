namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// Measures as entered in, or imported from, the Module Registry. They are compared with the
/// measures derived from the drawing (MD-10). All are optional; lengths in mm, angle in degrees.
/// </summary>
public sealed record RegistryMeasures
{
    public double? Straight { get; init; }
    public double? Radius { get; init; }
    public double? Angle { get; init; }
    public double? Length { get; init; }
    public double? Width { get; init; }
    public int? NumberOfSections { get; init; }
}
