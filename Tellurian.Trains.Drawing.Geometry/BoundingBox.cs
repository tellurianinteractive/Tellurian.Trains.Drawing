namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>An axis-aligned bounding box. <see cref="Empty"/> contains nothing.</summary>
public readonly record struct BoundingBox(Point Min, Point Max)
{
    public static BoundingBox Empty => new(
        new(double.PositiveInfinity, double.PositiveInfinity),
        new(double.NegativeInfinity, double.NegativeInfinity));

    public bool IsEmpty => Min.X > Max.X || Min.Y > Max.Y;
    public double Width => IsEmpty ? 0 : Max.X - Min.X;
    public double Height => IsEmpty ? 0 : Max.Y - Min.Y;
    public Point Center => new((Min.X + Max.X) / 2, (Min.Y + Max.Y) / 2);

    public static BoundingBox FromPoints(IEnumerable<Point> points) =>
        points.Aggregate(Empty, (box, point) => box.Including(point));

    public BoundingBox Including(Point point) => new(
        new(Math.Min(Min.X, point.X), Math.Min(Min.Y, point.Y)),
        new(Math.Max(Max.X, point.X), Math.Max(Max.Y, point.Y)));

    public BoundingBox Union(BoundingBox other) =>
        IsEmpty ? other :
        other.IsEmpty ? this :
        new(new(Math.Min(Min.X, other.Min.X), Math.Min(Min.Y, other.Min.Y)),
            new(Math.Max(Max.X, other.Max.X), Math.Max(Max.Y, other.Max.Y)));

    /// <summary>The box grown by <paramref name="margin"/> on every side.</summary>
    public BoundingBox Inflated(double margin) =>
        IsEmpty ? this : new(new(Min.X - margin, Min.Y - margin), new(Max.X + margin, Max.Y + margin));

    public bool Contains(Point point) =>
        point.X >= Min.X && point.X <= Max.X && point.Y >= Min.Y && point.Y <= Max.Y;

    public bool Intersects(BoundingBox other) =>
        !IsEmpty && !other.IsEmpty &&
        Min.X <= other.Max.X && other.Min.X <= Max.X &&
        Min.Y <= other.Max.Y && other.Min.Y <= Max.Y;
}
