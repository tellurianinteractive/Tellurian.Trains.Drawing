namespace Tellurian.Trains.Drawing.Geometry;

/// <summary>
/// A rigid transform: rotation about the origin followed by a translation.
/// It deliberately has no mirroring, because a physical module can't be mirrored.
/// </summary>
public readonly record struct Transform(Angle Rotation, Vector Translation)
{
    public static Transform Identity => new(Angle.Zero, Vector.Zero);

    /// <summary>The transform that maps <see cref="Pose.Origin"/> to <paramref name="pose"/>.</summary>
    public static Transform FromPose(Pose pose) => new(pose.Heading, pose.Position.AsVector);

    public Point Apply(Point point)
    {
        var rotated = point.AsVector.Rotated(Rotation);
        return new(rotated.X + Translation.X, rotated.Y + Translation.Y);
    }

    public Vector Apply(Vector vector) => vector.Rotated(Rotation);

    public Angle Apply(Angle angle) => angle + Rotation;

    public Pose Apply(Pose pose) => new(Apply(pose.Position), Apply(pose.Heading));

    /// <summary>The transform that first applies this one and then <paramref name="next"/>.</summary>
    public Transform Then(Transform next) =>
        new(Rotation + next.Rotation, next.Apply(Translation) + next.Translation);

    public Transform Inverse()
    {
        var inverseRotation = -Rotation;
        return new(inverseRotation, -Translation.Rotated(inverseRotation));
    }

    public bool IsCloseTo(Transform other, double lengthTolerance = Tolerance.Length, double angleTolerance = Tolerance.Angle) =>
        Rotation.IsCloseTo(other.Rotation, angleTolerance) && Translation.IsCloseTo(other.Translation, lengthTolerance);

    public override string ToString() => $"rotate {Rotation}, translate {Translation}";
}
