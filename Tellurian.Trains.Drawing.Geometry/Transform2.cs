namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>
/// A rigid transform: rotation about the origin followed by a translation.
/// It deliberately has no mirroring, because a physical module can't be mirrored.
/// </summary>
public readonly record struct Transform2(Angle Rotation, Vector2 Translation)
{
    public static Transform2 Identity => new(Angle.Zero, Vector2.Zero);

    /// <summary>The transform that maps <see cref="Pose2.Origin"/> to <paramref name="pose"/>.</summary>
    public static Transform2 FromPose(Pose2 pose) => new(pose.Heading, pose.Position.AsVector);

    public Point2 Apply(Point2 point)
    {
        var rotated = point.AsVector.Rotated(Rotation);
        return new(rotated.X + Translation.X, rotated.Y + Translation.Y);
    }

    public Vector2 Apply(Vector2 vector) => vector.Rotated(Rotation);

    public Angle Apply(Angle angle) => angle + Rotation;

    public Pose2 Apply(Pose2 pose) => new(Apply(pose.Position), Apply(pose.Heading));

    /// <summary>The transform that first applies this one and then <paramref name="next"/>.</summary>
    public Transform2 Then(Transform2 next) =>
        new(Rotation + next.Rotation, next.Apply(Translation) + next.Translation);

    public Transform2 Inverse()
    {
        var inverseRotation = -Rotation;
        return new(inverseRotation, -Translation.Rotated(inverseRotation));
    }

    public bool IsCloseTo(Transform2 other, double lengthTolerance = Tolerance.Length, double angleTolerance = Tolerance.Angle) =>
        Rotation.IsCloseTo(other.Rotation, angleTolerance) && Translation.IsCloseTo(other.Translation, lengthTolerance);

    public override string ToString() => $"rotate {Rotation}, translate {Translation}";
}
