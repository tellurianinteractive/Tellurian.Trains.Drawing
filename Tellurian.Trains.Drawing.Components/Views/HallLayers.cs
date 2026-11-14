namespace Tellurian.Trains.Drawing.Components.Views;

/// <summary>The layers of a hall view (EX-8).</summary>
[Flags]
public enum HallLayers
{
    None = 0,
    Walls = 1 << 0,
    Obstacles = 1 << 1,

    /// <summary>Doors and emergency exits with their keep-free zones.</summary>
    Doors = 1 << 2,

    NoGoZones = 1 << 3,
    Labels = 1 << 4,
    All = Walls | Obstacles | Doors | NoGoZones | Labels,
}
