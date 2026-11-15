using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model;

/// <summary>
/// A dimension on a drawing (EP-10): the distance between two points, drawn as a dimension line
/// <see cref="Offset"/> millimetres to the left of the direction from <see cref="Start"/> to <see cref="End"/>
/// (for aligned dimensions), or above or beside the points (for horizontal and vertical ones).
/// </summary>
public sealed record Dimension
{
    public required Point Start { get; init; }
    public required Point End { get; init; }
    public DimensionKind Kind { get; init; }

    /// <summary>The distance from the measured points to the dimension line, mm; negative is to the other side.</summary>
    public double Offset { get; init; }

    /// <summary>A text to show instead of the measured value, e.g. "c/c".</summary>
    public string? Text { get; init; }

    /// <summary>The measured length, mm.</summary>
    [JsonIgnore]
    public double Length => Kind switch
    {
        DimensionKind.Horizontal => Math.Abs(End.X - Start.X),
        DimensionKind.Vertical => Math.Abs(End.Y - Start.Y),
        _ => Start.DistanceTo(End),
    };

    /// <summary>The two ends of the dimension line, where the extension lines meet it.</summary>
    public (Point Start, Point End) Line()
    {
        switch (Kind)
        {
            case DimensionKind.Horizontal:
                var y = Math.Max(Start.Y, End.Y) + Offset;
                return (new(Start.X, y), new(End.X, y));
            case DimensionKind.Vertical:
                var x = Math.Max(Start.X, End.X) + Offset;
                return (new(x, Start.Y), new(x, End.Y));
            default:
                var direction = End - Start;
                var normal = direction.Length < Tolerance.Length ? Vector.Zero : direction.Normalized.LeftNormal * Offset;
                return (Start + normal, End + normal);
        }
    }

    /// <summary>
    /// The dimension that a pointer at <paramref name="placement"/> places: its kind follows where the pointer is
    /// (beside the points: vertical, above or below them: horizontal), unless <paramref name="kind"/> is given.
    /// </summary>
    public static Dimension Place(Point start, Point end, Point placement, DimensionKind? kind = null)
    {
        var chosen = kind ?? Infer(start, end, placement);
        var offset = chosen switch
        {
            DimensionKind.Horizontal => placement.Y - Math.Max(start.Y, end.Y),
            DimensionKind.Vertical => placement.X - Math.Max(start.X, end.X),
            _ => (end - start).Length < Tolerance.Length ? 0 : (placement - start).Dot((end - start).Normalized.LeftNormal),
        };
        return new() { Start = start, End = end, Kind = chosen, Offset = offset };
    }

    private static DimensionKind Infer(Point start, Point end, Point placement)
    {
        var box = BoundingBox.FromPoints([start, end]);
        var beside = placement.X < box.Min.X || placement.X > box.Max.X;
        var aboveOrBelow = placement.Y < box.Min.Y || placement.Y > box.Max.Y;
        return (beside, aboveOrBelow) switch
        {
            (true, false) => DimensionKind.Vertical,
            (false, true) => DimensionKind.Horizontal,
            _ => DimensionKind.Aligned,
        };
    }
}
