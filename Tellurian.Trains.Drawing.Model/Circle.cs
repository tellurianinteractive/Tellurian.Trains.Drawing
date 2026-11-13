using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model;

/// <summary>A circle, e.g. a screw hole or a dowel in an end profile.</summary>
public sealed record Circle(Point2 Center, double Diameter)
{
    public Circle MirroredX() => this with { Center = Center.MirroredX() };
}
