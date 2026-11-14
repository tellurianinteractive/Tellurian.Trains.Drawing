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
    /// For each point of the upper edge: its height from the origin's horizontal line, drawn through the point, and
    /// its distance from the origin's vertical line, in rows above the plate so they don't overlap.
    /// </summary>
    private static IEnumerable<Dimension> PointDimensions(PlateShape plate, MeasureFrame frame, double gap)
    {
        var top = plate.UpperEdge.Max(p => p.Y);
        var row = 0;
        for (var i = 0; i < plate.UpperEdge.Count; i++)
        {
            var point = plate.UpperEdge[i];
            var isSide = i == 0 || i == plate.UpperEdge.Count - 1;
            // The sides' heights are dimensioned already when measuring from the bottom.
            if (!(isSide && frame.Kind == MeasureOrigin.PlateLowerLeft) && Math.Abs(point.Y - frame.Origin.Y) > Tolerance.Length)
                yield return new() { Start = new(point.X, frame.Origin.Y), End = point, Kind = DimensionKind.Vertical, Offset = 0 };
            if (Math.Abs(point.X - frame.Origin.X) > Tolerance.Length)
            {
                row++;
                var start = new Point(frame.Origin.X, frame.Kind == MeasureOrigin.PlateLowerLeft ? plate.UpperEdge[0].Y : point.Y);
                yield return Dimension.Place(start, point, new(0, top + gap * row), DimensionKind.Horizontal);
            }
        }
    }
}
