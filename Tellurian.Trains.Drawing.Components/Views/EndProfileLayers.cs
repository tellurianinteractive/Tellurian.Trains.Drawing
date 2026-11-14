namespace Tellurian.Trains.Drawing.Components.Views;

/// <summary>The layers of an end profile view (EX-8).</summary>
[Flags]
public enum EndProfileLayers
{
    None = 0,
    Outline = 1 << 0,
    GroundLine = 1 << 1,
    Tracks = 1 << 2,
    ScrewHoles = 1 << 3,
    Dowels = 1 << 4,

    /// <summary>The rail-top reference line (RÖK), always shown in the editor (EP-2).</summary>
    RailTop = 1 << 5,

    Labels = 1 << 6,
    All = Outline | GroundLine | Tracks | ScrewHoles | Dowels | RailTop | Labels,
}
