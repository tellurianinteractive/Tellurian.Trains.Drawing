using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Editing.Tools;

/// <summary>A measurement between two points (MD-12): distance, its X and Y parts, and the direction.</summary>
public sealed record Measurement(Point Start, Point End)
{
    public double Distance => Start.DistanceTo(End);
    public double DeltaX => End.X - Start.X;
    public double DeltaY => End.Y - Start.Y;

    /// <summary>The direction from <see cref="Start"/> to <see cref="End"/>, 0–360°, counter-clockwise from X.</summary>
    public Angle Direction => (End - Start).Direction.Normalized;
}
