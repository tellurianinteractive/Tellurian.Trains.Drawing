using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Halls;

/// <summary>
/// A door or emergency exit in a wall, with a zone in front of it that must be kept free (HA-2, VA-6).
/// </summary>
public sealed record Door
{
    public const double DefaultKeepFreeDepth = 1000;

    private static readonly Angle QuarterTurn = new(90);

    public Guid Id { get; init; } = Guid.NewGuid();
    public DoorKind Kind { get; init; }

    /// <summary>The centre of the opening on the wall, heading into the hall.</summary>
    public required Pose Position { get; init; }

    /// <summary>The width of the opening, mm.</summary>
    public required double Width { get; init; }

    /// <summary>How far into the hall the zone in front of the door must be kept free, mm.</summary>
    public double KeepFreeDepth { get; init; } = DefaultKeepFreeDepth;

    public string? Label { get; init; }

    /// <summary>The keep-free zone: the width of the opening, <see cref="KeepFreeDepth"/> into the hall.</summary>
    public PathDrawing KeepFreeZone()
    {
        // The rectangle's X axis runs along the wall, to the right of the heading, and its Y axis into the hall.
        var corner = new Pose(Position.Position + Position.Left * (Width / 2), Position.Heading - QuarterTurn);
        return PathDrawing.Rectangle(Width, KeepFreeDepth, Transform.FromPose(corner));
    }

    public override string? ToString() => Label ?? Kind.ToString();
}
