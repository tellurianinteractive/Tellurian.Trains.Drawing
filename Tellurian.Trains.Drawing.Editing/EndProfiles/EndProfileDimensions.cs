using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Editing.EndProfiles;

/// <summary>
/// The dimensions the end profile editor draws automatically (EP-10): the plate's width, side heights and
/// depth below rail top, and, in detail, each screw hole and upper-edge point measured from the measure origin.
/// </summary>
public static class EndProfileDimensions
{
    /// <param name="gap">The distance between the drawing and its dimension lines, mm; the editor passes some pixels at its zoom.</param>
    /// <param name="details">Also dimension each screw hole and point of the upper edge.</param>
    public static IReadOnlyList<Dimension> Create(EndProfile profile, MeasureFrame frame, double gap, bool details)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(frame);
        var dimensions = new List<Dimension>();
        if (PlateShape.From(profile) is { } plate)
        {
            Point leftBottom = new(plate.Left, plate.Bottom), rightBottom = new(plate.Right, plate.Bottom);
            dimensions.Add(Dimension.Place(leftBottom, rightBottom, new(0, plate.Bottom - gap), DimensionKind.Horizontal));
            dimensions.Add(Dimension.Place(leftBottom, plate.UpperEdge[0], new(plate.Left - gap, 0), DimensionKind.Vertical));
            dimensions.Add(Dimension.Place(rightBottom, plate.UpperEdge[^1], new(plate.Right + gap, 0), DimensionKind.Vertical));
            dimensions.Add(Dimension.Place(new(plate.Left, 0), leftBottom, new(plate.Left - 2.2 * gap, 0), DimensionKind.Vertical));
            if (details) dimensions.AddRange(PointDimensions(plate, frame, gap));
            dimensions.AddRange(RadiusDimensions(plate, gap));
        }
        if (details) dimensions.AddRange(ScrewHoleDimensions(profile, frame));
        return dimensions;
    }

    /// <summary>
    /// For each screw hole: the horizontal distance from the origin's vertical line and the vertical distance from
    /// its horizontal line, each drawn through the hole. Distances of zero aren't drawn.
    /// </summary>
    private static IEnumerable<Dimension> ScrewHoleDimensions(EndProfile profile, MeasureFrame frame)
    {
        foreach (var hole in profile.ScrewHoles)
        {
            var center = hole.Center;
            if (Math.Abs(center.X - frame.Origin.X) > Tolerance.Length)
                yield return new() { Start = new(frame.Origin.X, center.Y), End = center, Kind = DimensionKind.Horizontal, Offset = 0 };
            if (Math.Abs(center.Y - frame.Origin.Y) > Tolerance.Length)
                yield return new() { Start = new(center.X, frame.Origin.Y), End = center, Kind = DimensionKind.Vertical, Offset = 0 };
        }
    }

    /// <summary>
    /// For each arc of the upper edge (arcs and fillets): its radius, as a short line from the middle of the arc
    /// towards its centre, labelled "R" and the radius.
    /// </summary>
    private static IEnumerable<Dimension> RadiusDimensions(PlateShape plate, double gap)
    {
        foreach (var arc in plate.Edge.Segments.OfType<ArcSegment>())
        {
            var middle = arc.PointAt(arc.Length / 2);
            var length = Math.Min(arc.Radius, gap * 1.5);
            var towardsCentre = middle + (arc.Center - middle).Normalized * length;
            yield return new() { Start = towardsCentre, End = middle, Kind = DimensionKind.Aligned, Offset = 0, Text = $"R {Format(arc.Radius)}" };
        }
    }

    private static string Format(double radius) =>
        Math.Round(radius, radius < 100 ? 1 : 0).ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Heights closer than this share one dimension, mm.</summary>
    public const double SameHeight = 0.05;

    /// <summary>
    /// For the points of the upper edge: their heights above the origin's horizontal line, in columns to the right of
    /// the plate, one per distinct height, so points at the same height share a dimension; and their distances from
    /// the origin's vertical line, in rows above the plate. Measured from the corner, the sides' heights are
    /// dimensioned already, so points at those heights share them.
    /// </summary>
    private static IEnumerable<Dimension> PointDimensions(PlateShape plate, MeasureFrame frame, double gap)
    {
        var shared = frame.Kind == MeasureOrigin.PlateLowerLeft ? new[] { plate.UpperEdge[0].Y, plate.UpperEdge[^1].Y } : [];
        var heights = plate.UpperEdge
            .Where(p => Math.Abs(p.Y - frame.Origin.Y) > SameHeight && !shared.Any(y => Math.Abs(p.Y - y) <= SameHeight))
            .GroupBy(p => Math.Round(p.Y / SameHeight / 2) * SameHeight * 2)
            .OrderBy(g => g.Key)
            .ToList();
        var column = 0;
        foreach (var group in heights)
        {
            column++;
            var nearest = group.MaxBy(p => p.X);
            yield return Dimension.Place(new(plate.Right, frame.Origin.Y), nearest, new(plate.Right + gap * (1 + column), 0), DimensionKind.Vertical);
        }

        var top = plate.UpperEdge.Max(p => p.Y);
        var row = 0;
        foreach (var point in plate.UpperEdge.Where(p => Math.Abs(p.X - frame.Origin.X) > Tolerance.Length))
        {
            row++;
            var start = new Point(frame.Origin.X, frame.Kind == MeasureOrigin.PlateLowerLeft ? plate.UpperEdge[0].Y : point.Y);
            yield return Dimension.Place(start, point, new(0, top + gap * row), DimensionKind.Horizontal);
        }
    }
}
