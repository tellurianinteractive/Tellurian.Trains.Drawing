using System.Globalization;
using System.Text;
using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model;

namespace Tellurian.Trains.Drawing.Components.Rendering;

/// <summary>
/// SVG path data (<c>d</c>) for drawings in world coordinates, to be drawn inside the canvas's Y-flipped
/// group. In world coordinates a positive sweep is counter-clockwise, which is SVG's sweep flag 1 there.
/// </summary>
public static class SvgPath
{
    private const double HalfTurn = 180;

    /// <summary>Values that round to zero at three decimals; written as 0, never as -0.</summary>
    private const double NegligibleValue = 0.0005;

    public static string From(PathDrawing path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return From(path.ToPath());
    }

    public static string From(SegmentPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var builder = new StringBuilder();
        Move(builder, path.Start);
        foreach (var segment in path.Segments)
        {
            switch (segment)
            {
                case LineSegment line:
                    builder.Append(" L ");
                    Coordinates(builder, line.End);
                    break;
                case ArcSegment arc:
                    Arc(builder, arc);
                    break;
                default:
                    throw new NotSupportedException($"Unknown segment type {segment.GetType().Name}.");
            }
        }
        if (path.IsClosed) builder.Append(" Z");
        return builder.ToString();
    }

    /// <summary>A polyline through <paramref name="points"/>, e.g. a rubber band or a preview.</summary>
    public static string Polyline(IEnumerable<Point> points, bool isClosed = false)
    {
        var builder = new StringBuilder();
        var first = true;
        foreach (var point in points)
        {
            if (first) Move(builder, point);
            else
            {
                builder.Append(" L ");
                Coordinates(builder, point);
            }
            first = false;
        }
        if (isClosed && !first) builder.Append(" Z");
        return builder.ToString();
    }

    /// <summary>A number formatted for SVG: invariant culture and at most three decimals.</summary>
    public static string Number(double value) =>
        Math.Abs(value) < NegligibleValue ? "0" : value.ToString("0.###", CultureInfo.InvariantCulture);

    private static void Arc(StringBuilder builder, ArcSegment arc)
    {
        // SVG can't draw a full circle as one arc, so arcs longer than half a turn are drawn in two halves.
        var sweep = arc.SweepDegrees;
        if (Math.Abs(sweep) > HalfTurn)
        {
            var middle = arc.PointAt(arc.Length / 2);
            ArcTo(builder, arc.Radius, middle, sweep > 0);
            ArcTo(builder, arc.Radius, arc.End, sweep > 0);
        }
        else
        {
            ArcTo(builder, arc.Radius, arc.End, sweep > 0);
        }
    }

    private static void ArcTo(StringBuilder builder, double radius, Point end, bool counterClockwise)
    {
        var r = Number(radius);
        builder.Append(" A ").Append(r).Append(' ').Append(r).Append(" 0 0 ").Append(counterClockwise ? '1' : '0').Append(' ');
        Coordinates(builder, end);
    }

    private static void Move(StringBuilder builder, Point point)
    {
        builder.Append("M ");
        Coordinates(builder, point);
    }

    private static void Coordinates(StringBuilder builder, Point point) =>
        builder.Append(Number(point.X)).Append(' ').Append(Number(point.Y));
}
