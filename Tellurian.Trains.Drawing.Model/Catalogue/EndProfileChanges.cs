namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>What differs between two drawings of the same end profile.</summary>
[Flags]
public enum EndProfileChanges
{
    None = 0,
    Designation = 1 << 0,
    Scale = 1 << 1,
    Standards = 1 << 2,
    Tracks = 1 << 3,
    ScrewHoles = 1 << 4,
    Dowels = 1 << 5,
    Outline = 1 << 6,
    GroundLine = 1 << 7,

    /// <summary>The source document or the Registry id.</summary>
    Source = 1 << 8,
}
