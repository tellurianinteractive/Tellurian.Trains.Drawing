using Tellurian.Trains.Drawing.Model.Catalogue;

namespace Tellurian.Trains.Drawing.Model.EndProfiles;

/// <summary>
/// A track crossing an end profile. Its rail top is at Y = 0 by definition (RÖK);
/// <see cref="Offset"/> is the lateral position of the track centre, positive to the right.
/// </summary>
public sealed record EndProfileTrack
{
    public required double Offset { get; init; }
    public required double Gauge { get; init; }
    public RailSystem RailSystem { get; init; }
    public string? Label { get; init; }

    public EndProfileTrack MirroredX() => this with { Offset = -Offset };
}
