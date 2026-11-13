using System.Globalization;

namespace Tellurian.Trains.Drawing.App.Spikes;

/// <summary>
/// Throw-away module shapes for performance spike A (docs/ImplementationPlan.md, Phase 0).
/// The mix follows the Module Registry statistics (docs/ModuleDrawing.md §12):
/// about 56 % straight, 36 % curve, 7 % straight + curve, about 20 % double track.
/// Plate widths are provisional: FREMO-H0-B96 and FREMO-H0-2E96 500 mm, MMM480 480 mm.
/// SVG coordinates are y-down; that doesn't matter for measuring performance.
/// </summary>
public sealed class SpikeModule
{
    public required int Id { get; init; }
    public required string Outline { get; init; }
    public required string Tracks { get; init; }
    public required string TrackClass { get; init; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Rotation { get; set; }
    public string Transform => string.Create(CultureInfo.InvariantCulture, $"translate({X:F1} {Y:F1}) rotate({Rotation:F1})");
}

public static class SpikeModules
{
    public const double HallWidth = 40_000;
    public const double HallDepth = 25_000;
    private const double TrackCentres = 50; // c/c between double tracks, provisional.

    private static readonly double[] StraightLengths = [500, 750, 1000, 1200, 1200, 1500, 2000];
    private static readonly double[] Radii = [1500, 2000, 2000, 2500, 3000];
    private static readonly double[] Angles = [15, 22.5, 30, 30, 45, 90];

    public static List<SpikeModule> Create(int count, int seed = 4711)
    {
        var random = new Random(seed);
        var modules = new List<SpikeModule>(count);
        for (var id = 0; id < count; id++)
        {
            var isFremo = random.NextDouble() < 0.6;
            var width = isFremo ? 500.0 : 480.0;
            var doubleTrack = random.NextDouble() < 0.2;
            var kind = random.NextDouble();
            var isStation = random.NextDouble() < 0.05;
            double straight = 0, radius = 0, angle = 0;
            if (isStation) { straight = 3000 + random.Next(0, 4) * 1000; width = 600; }
            else if (kind < 0.56) straight = Pick(random, StraightLengths);
            else if (kind < 0.93) { radius = Pick(random, Radii); angle = Pick(random, Angles); }
            else { straight = Pick(random, StraightLengths); radius = Pick(random, Radii); angle = Pick(random, Angles); }

            var offsets = doubleTrack ? new[] { -TrackCentres / 2, TrackCentres / 2 } : new[] { 0.0 };
            modules.Add(new SpikeModule
            {
                Id = id,
                Outline = OutlinePath(straight, radius, angle, width),
                Tracks = string.Join(' ', offsets.Select(o => TrackPath(straight, radius, angle, o))),
                TrackClass = isFremo ? "track fremo" : "track mmm",
                X = 1000 + random.NextDouble() * (HallWidth - 2000),
                Y = 1000 + random.NextDouble() * (HallDepth - 2000),
                Rotation = random.Next(0, 24) * 15,
            });
        }
        return modules;
    }

    private static double Pick(Random random, double[] values) => values[random.Next(values.Length)];

    // Straight part first along +X, then a curve turning towards -Y (left on screen).
    private static string OutlinePath(double straight, double radius, double angle, double width)
    {
        var h = width / 2;
        if (angle <= 0) return F($"M 0 {-h} H {straight} V {h} H 0 Z");
        var (ro, ri) = (radius + h, radius - h);
        var large = angle > 180 ? 1 : 0;
        var (ox, oy) = ArcPoint(straight, radius, ro, angle);
        var (ix, iy) = ArcPoint(straight, radius, ri, angle);
        // Outer edge (y = +h side) runs along the straight part, then around the outer arc; back along the inner arc.
        return F($"M 0 {h} H {straight} A {ro} {ro} 0 {large} 0 {ox} {oy} L {ix} {iy} A {ri} {ri} 0 {large} 1 {straight} {-h} H 0 Z");
    }

    private static string TrackPath(double straight, double radius, double angle, double offset)
    {
        if (angle <= 0) return F($"M 0 {offset} H {straight}");
        var r = radius + offset;
        var (x, y) = ArcPoint(straight, radius, r, angle);
        return F($"M 0 {offset} H {straight} A {r} {r} 0 {(angle > 180 ? 1 : 0)} 0 {x} {y}");
    }

    // Point at radius r and angle (degrees) on a curve whose centre is at (straight, -radius).
    private static (double X, double Y) ArcPoint(double straight, double radius, double r, double angle)
    {
        var t = angle * Math.PI / 180;
        return (straight + r * Math.Sin(t), -radius + r * Math.Cos(t));
    }

    private static string F(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
}
