using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.EndProfiles;

/// <summary>
/// Creates an end profile from a template (spec EP-2): a rectangular end plate with tracks,
/// screw holes and a flat ground line along the top of the plate.
/// </summary>
public static class EndProfileTemplate
{
    public static EndProfile Create(EndProfileTemplateParameters parameters)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parameters.PlateWidth);
        if (parameters.PlateBottom >= parameters.PlateTop)
            throw new ArgumentException("The plate bottom must be below the plate top.", nameof(parameters));

        var half = parameters.PlateWidth / 2;
        var top = parameters.PlateTop;
        var bottom = parameters.PlateBottom;
        return new()
        {
            Id = parameters.Id ?? Guid.NewGuid(),
            Designation = parameters.Designation,
            Scale = parameters.Scale,
            StandardIds = parameters.StandardIds,
            Outline = PathDrawing.Polyline([new(-half, bottom), new(half, bottom), new(half, top), new(-half, top)], isClosed: true),
            GroundLine = PathDrawing.Polyline([new(-half, top), new(half, top)]),
            Tracks = [.. parameters.TrackOffsets.Select(offset => new EndProfileTrack { Offset = offset, Gauge = parameters.Gauge, RailSystem = parameters.RailSystem })],
            ScrewHoles = [.. parameters.ScrewHoles.Select(p => new Circle(p, parameters.ScrewHoleDiameter))],
            ScrewHolePattern = parameters.ScrewHolePattern,
            Source = parameters.Source,
        };
    }

    /// <summary>Offsets of <paramref name="count"/> tracks centred on the centre line, <paramref name="centreDistance"/> apart.</summary>
    public static IReadOnlyList<double> CentredOffsets(int count, double centreDistance)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return [.. Enumerable.Range(0, count).Select(i => (i - (count - 1) / 2.0) * centreDistance)];
    }
}
