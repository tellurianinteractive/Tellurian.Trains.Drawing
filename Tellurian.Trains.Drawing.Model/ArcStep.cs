using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model;

/// <summary>
/// A circular step around <see cref="Center"/>, turning <see cref="Sweep"/> degrees
/// (positive counter-clockwise). The radius is the distance from the start to the centre.
/// </summary>
public sealed record ArcStep(Point Center, double Sweep) : PathStep
{
    public override Segment ToSegment(Point start) =>
        new ArcSegment(Center, start.DistanceTo(Center), (start - Center).Direction, Sweep);
}
