using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.Catalogue;

namespace Tellurian.Trains.Drawing.Model.EndProfiles;

/// <summary>Parameters for <see cref="EndProfileTemplate.Create"/>. Heights are relative to rail top (Y = 0).</summary>
public sealed record EndProfileTemplateParameters
{
    public Guid? Id { get; init; }
    public required string Designation { get; init; }
    public required string Scale { get; init; }
    public IReadOnlyList<CatalogueReference> Standards { get; init; } = [];
    public required double PlateWidth { get; init; }

    /// <summary>Y of the top edge of the plate; normally negative (below rail top).</summary>
    public required double PlateTop { get; init; }

    /// <summary>Y of the bottom edge of the plate.</summary>
    public required double PlateBottom { get; init; }

    public IReadOnlyList<double> TrackOffsets { get; init; } = [0];
    public required double Gauge { get; init; }
    public RailSystem RailSystem { get; init; }
    public IReadOnlyList<Point2> ScrewHoles { get; init; } = [];
    public double ScrewHoleDiameter { get; init; } = 8;
    public string? ScrewHolePattern { get; init; }
    public DocumentReference? Source { get; init; }
}
