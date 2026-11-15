namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>An area of the plan where visitors have access (HP-17).</summary>
public sealed record PublicArea
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>A closed path in hall coordinates.</summary>
    public required PathDrawing Area { get; init; }

    public string? Label { get; init; }
    public string? Note { get; init; }
}
