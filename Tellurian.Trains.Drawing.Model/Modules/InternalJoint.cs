using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.Catalogue;

namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// The fixed joint between two sections of the same module, along a section line (MD-4a): a polyline across the
/// module with its ends on the outline. Never offered for joining in a hall plan, and it always fits: its end
/// profiles are never compared.
/// </summary>
public sealed record InternalJoint
{
    public required string FirstSection { get; init; }
    public required string SecondSection { get; init; }

    /// <summary>One end of the section line, at the outline.</summary>
    public required Point Start { get; init; }

    /// <summary>The other end of the section line, at the outline.</summary>
    public required Point End { get; init; }

    /// <summary>
    /// The points where the section line bends between <see cref="Start"/> and <see cref="End"/>, in order; null or
    /// empty for a straight line. A section line can have any number of straight parts (MD-4).
    /// </summary>
    public IReadOnlyList<Point>? Bends { get; init; }

    /// <summary>The section line from <see cref="Start"/> through the bends to <see cref="End"/>.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyList<Point> Points => [Start, .. Bends ?? [], End];

    /// <summary>An optional, freely designed internal end profile; not checked against any standard.</summary>
    public CatalogueReference? EndProfile { get; init; }
}
