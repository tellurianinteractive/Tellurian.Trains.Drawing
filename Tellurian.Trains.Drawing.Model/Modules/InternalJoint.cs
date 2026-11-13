using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.Catalogue;

namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// The fixed joint between two sections of the same module, along a section line (MD-4a).
/// Never offered for joining in a hall plan.
/// </summary>
public sealed record InternalJoint
{
    public required string FirstSection { get; init; }
    public required string SecondSection { get; init; }

    /// <summary>One end of the section line, at the outline.</summary>
    public required Point Start { get; init; }

    /// <summary>The other end of the section line, at the outline.</summary>
    public required Point End { get; init; }

    /// <summary>An optional, freely designed internal end profile; not checked against any standard.</summary>
    public CatalogueReference? EndProfile { get; init; }
}
