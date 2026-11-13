namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>
/// A position with a heading. Used for exits (heading out of the module), track connection points
/// and the placement of modules.
/// </summary>
public readonly record struct Pose2(Point2 Position, Angle Heading)
{
    public static Pose2 Origin => new(Point2.Origin, Angle.Zero);

    /// <summary>Unit vector in the heading direction.</summary>
    public Vector2 Forward => Vector2.FromAngle(Heading);

    /// <summary>Unit vector 90° to the left of the heading.</summary>
    public Vector2 Left => Forward.LeftNormal;

    /// <summary>The same position, facing the opposite way.</summary>
    public Pose2 Reversed() => this with { Heading = Heading + Angle.Half };

    /// <summary>The pose moved <paramref name="distance"/> along its heading.</summary>
    public Pose2 Advanced(double distance) => this with { Position = Position + Forward * distance };

    public bool IsCloseTo(Pose2 other, double lengthTolerance = Tolerance.Length, double angleTolerance = Tolerance.Angle) =>
        Position.IsCloseTo(other.Position, lengthTolerance) && Heading.IsCloseTo(other.Heading, angleTolerance);

    public override string ToString() => $"{Position} {Heading}";
}
