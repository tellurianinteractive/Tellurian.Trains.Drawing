using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model;

/// <summary>A straight step to <see cref="End"/>.</summary>
public sealed record LineStep(Point2 End) : PathStep
{
    public override Segment ToSegment(Point2 start) => new LineSegment(start, End);
}
