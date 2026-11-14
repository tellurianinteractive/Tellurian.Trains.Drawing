namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>
/// The measurements that tell the people setting up where to put the anchor module (HP-21): the distances
/// from a reference point on the anchor to two walls or obstacles, and the angle to a wall. The distances
/// and the angle are computed from the anchor pose; typing them moves the anchor instead.
/// </summary>
public sealed record AnchorDimensions
{
    public required AnchorReferencePoint ReferencePoint { get; init; }
    public required HallElementReference First { get; init; }
    public required HallElementReference Second { get; init; }

    /// <summary>The index of the wall that the anchor's angle is given to.</summary>
    public int? AngleWall { get; init; }
}
