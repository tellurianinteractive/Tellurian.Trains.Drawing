namespace Tellurian.Trains.Drawing.Editing.EndProfiles;

/// <summary>Where measures in the end profile editor are taken from.</summary>
public enum MeasureOrigin
{
    /// <summary>Rail top and the centre line: how drawings are stored (spec §3.1).</summary>
    RailTopCentre,

    /// <summary>The plate's lower left corner, as many FREMO drawings are dimensioned.</summary>
    PlateLowerLeft,
}
