namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>The result of <see cref="ShapeComparison.MaximumDeviation"/>: the deviation and where it occurs.</summary>
public readonly record struct ShapeDeviation(double Maximum, Point At)
{
    public bool IsWithin(double tolerance) => Maximum <= tolerance;
}
