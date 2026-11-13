namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>How the track is powered. Tracks of different rail systems can't be joined (spec §3.2 rule 1).</summary>
public enum RailSystem
{
    TwoRail,
    ThreeRail,
}
