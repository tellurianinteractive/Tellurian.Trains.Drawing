using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>A part of an end plate where the end profile's upper edge is above or below rail top, in end view X.</summary>
public sealed record ProfileInterval(double From, double To, bool IsAboveRailTop);

/// <summary>A shaded area at an exit in plan view (ModuleDrawing §9.1).</summary>
public sealed record ProfileShade(IReadOnlyList<Point> Corners, bool IsAboveRailTop);

/// <summary>
/// Shows the kind of end profile in plan view, as in DWG and SKP module drawings (ModuleDrawing §9.1).
/// The profile's upper edge is folded into the module about the end plate: a point at height <c>y</c> above or
/// below rail top lands <c>|y|</c> inwards from the plate. The area between the plate and the folded edge is dark
/// where the edge is above rail top and light where it is below, so the shading has the form of the profile.
/// </summary>
public static class ProfileShading
{
    /// <summary>
    /// The parts of the profile's upper edge above and below rail top (Y = 0), from left to right in end view.
    /// The upper edge is the ground line, or the top of the plate outline when there is no ground line.
    /// </summary>
    public static IReadOnlyList<ProfileInterval> Intervals(EndProfile profile)
    {
        var intervals = new List<ProfileInterval>();
        foreach (var part in Parts(profile))
            intervals.Add(new(part[0].X, part[^1].X, part.Any(p => p.Y > Tolerance.Length)));
        return intervals;
    }

    /// <summary>How far the shading reaches into the module: the largest height of the upper edge from rail top, mm.</summary>
    public static double Depth(EndProfile profile) =>
        Parts(profile).SelectMany(p => p).Select(p => Math.Abs(p.Y)).DefaultIfEmpty(0).Max();

    /// <summary>The shaded areas at <paramref name="exit"/>, with the profile as built there (its orientation).</summary>
    public static IReadOnlyList<ProfileShade> Shades(ModuleExit exit, EndProfile profile)
    {
        ArgumentNullException.ThrowIfNull(exit);
        ArgumentNullException.ThrowIfNull(profile);
        var inwards = -exit.Pose.Forward;
        return [.. Parts(profile.Oriented(exit.Orientation)).Select(part =>
        {
            var folded = part.Select(p => ModuleGeometry.ToPlan(exit, p.X) + inwards * Math.Abs(p.Y));
            var plate = new[] { ModuleGeometry.ToPlan(exit, part[^1].X), ModuleGeometry.ToPlan(exit, part[0].X) };
            return new ProfileShade([.. folded, .. plate], part.Any(p => p.Y > Tolerance.Length));
        })];
    }

    /// <summary>
    /// The upper edge across the plate as polylines in end view, split where it crosses rail top, so each part is
    /// entirely above or below it. Vertical steps in the edge are kept.
    /// </summary>
    private static List<List<Point>> Parts(EndProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var edge = (profile.GroundLine ?? profile.Outline)?.Tessellate();
        if (edge is null || edge.Points.Count < 2) return [];
        var span = profile.Outline?.ToPath().Bounds is { IsEmpty: false } plate ? plate : edge.Bounds;
        var edges = Enumerable.Range(0, edge.EdgeCount).Select(edge.Edge).ToList();
        var breaks = edges
            .SelectMany(e => CrossingAtRailTop(e) is { } x ? new[] { e.Start.X, e.End.X, x } : [e.Start.X, e.End.X])
            .Append(span.Min.X).Append(span.Max.X)
            .Where(x => x >= span.Min.X - Tolerance.Length && x <= span.Max.X + Tolerance.Length)
            .Order().ToList();

        // The edge as a function of X; at a vertical step both heights are kept.
        var points = new List<Point>();
        for (var i = 0; i < breaks.Count; i++)
        {
            var x = breaks[i];
            if (i > 0 && x - breaks[i - 1] < Tolerance.Length) continue;
            var fromLeft = i > 0 ? TopAt(edges, x, fromLeft: true) : null;
            var fromRight = i < breaks.Count - 1 ? TopAt(edges, x, fromLeft: false) : null;
            if (fromLeft is { } l) points.Add(new(x, l));
            if (fromRight is { } r && (fromLeft is null || Math.Abs(r - fromLeft.Value) > Tolerance.Length)) points.Add(new(x, r));
        }

        // Split at rail top: a point at Y = 0 ends one part and starts the next.
        var parts = new List<List<Point>>();
        var current = new List<Point>();
        foreach (var point in points)
        {
            var side = Side(point.Y);
            if (current.Count > 0 && side != 0 && current.Select(p => Side(p.Y)).FirstOrDefault(s => s != 0) is var currentSide && currentSide != 0 && currentSide != side)
            {
                // Crossing without a point at rail top, e.g. a vertical step through it.
                var last = current[^1];
                var crossing = new Point(Math.Abs(last.X - point.X) < Tolerance.Length ? point.X : last.X + (point.X - last.X) * (-last.Y / (point.Y - last.Y)), 0);
                current.Add(crossing);
                Add(parts, current);
                current = [crossing];
            }
            current.Add(point);
            if (side == 0 && current.Count > 1)
            {
                Add(parts, current);
                current = [point];
            }
        }
        Add(parts, current);
        return parts;
    }

    private static int Side(double y) => y > Tolerance.Length ? 1 : y < -Tolerance.Length ? -1 : 0;

    /// <summary>Keeps a part that spans some width and leaves rail top somewhere.</summary>
    private static void Add(List<List<Point>> parts, List<Point> part)
    {
        if (part.Count > 1 && part[^1].X - part[0].X > Tolerance.Length && part.Any(p => Side(p.Y) != 0)) parts.Add(part);
    }

    private static double? CrossingAtRailTop((Point Start, Point End) e)
    {
        var (a, b) = e;
        if (a.Y * b.Y >= 0 || Math.Abs(b.Y - a.Y) < Tolerance.Length) return null;
        return a.X + (b.X - a.X) * (-a.Y / (b.Y - a.Y));
    }

    /// <summary>
    /// The highest point of the edge at <paramref name="x"/>, approached from the left or the right,
    /// or null if the edge doesn't reach it from that side.
    /// </summary>
    private static double? TopAt(List<(Point Start, Point End)> edges, double x, bool fromLeft)
    {
        double? top = null;
        foreach (var (a, b) in edges)
        {
            var (left, right) = a.X <= b.X ? (a, b) : (b, a);
            if (right.X - left.X < Tolerance.Length) continue;
            var inside = fromLeft
                ? x > left.X + Tolerance.Length && x <= right.X + Tolerance.Length
                : x >= left.X - Tolerance.Length && x < right.X - Tolerance.Length;
            if (!inside) continue;
            var t = Math.Clamp((x - left.X) / (right.X - left.X), 0, 1);
            var y = left.Y + (right.Y - left.Y) * t;
            if (top is null || y > top) top = y;
        }
        return top;
    }
}
