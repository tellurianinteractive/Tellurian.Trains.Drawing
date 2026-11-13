namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>
/// Mirroring about the centre line (X → −X), used <b>only</b> to compare end profiles: two facing
/// ends are each drawn as seen from outside their own module (spec §3.1). It is deliberately kept
/// apart from <see cref="Transform"/> so that a module placement can never be mirrored.
/// </summary>
public static class Mirror
{
    public static Point AboutCentreLine(Point point) => point.MirroredX();

    public static Segment AboutCentreLine(Segment segment) => segment.MirroredX();

    public static SegmentPath AboutCentreLine(SegmentPath path) => path.MirroredX();

    public static Polyline AboutCentreLine(Polyline polyline) =>
        new(polyline.Points.Select(p => p.MirroredX()), polyline.IsClosed);

    public static IReadOnlyList<Point> AboutCentreLine(IEnumerable<Point> points) =>
        [.. points.Select(p => p.MirroredX())];
}
