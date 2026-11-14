namespace Tellurian.Trains.Drawing.Editing.EndProfiles;

/// <summary>The kinds of selectable items in an end profile drawing.</summary>
public enum EndProfileItemKind
{
    Track,
    ScrewHole,
    Dowel,
    Outline,
    GroundLine,

    /// <summary>A point of the plate's upper edge.</summary>
    PlatePoint,
}
