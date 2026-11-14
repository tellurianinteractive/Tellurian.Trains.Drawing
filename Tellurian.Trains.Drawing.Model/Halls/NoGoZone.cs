namespace Tellurian.Trains.Drawing.Model.Halls;

/// <summary>An area of a hall where nothing may be placed, e.g. in front of a fire hose cabinet (HA-2, VA-6).</summary>
public sealed record NoGoZone
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>A closed path in hall coordinates.</summary>
    public required PathDrawing Area { get; init; }

    public required string Reason { get; init; }

    public override string ToString() => Reason;
}
