using Tellurian.Trains.Schedules.Model.Geometry;

namespace Tellurian.Trains.Drawing.Model;

/// <summary>A circle, e.g. a screw hole or a dowel in an end profile.</summary>
public sealed record Circle(Point Center, double Diameter) : ExtensibleRecord
{
    public Circle MirroredX() => this with { Center = Center.MirroredX() };
}
