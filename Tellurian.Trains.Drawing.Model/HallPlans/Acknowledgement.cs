namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>
/// A planner's decision that a specific warning is OK (HP-15). It is valid only while the involved
/// placements are unchanged, which the fingerprint of their state (pose, variant, leg adjustment and
/// module revision) records.
/// </summary>
public sealed record Acknowledgement
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The code of the acknowledged finding type (VA-11, VA-12, VA-13, VA-14).</summary>
    public required string FindingCode { get; init; }

    /// <summary>The modules of the involved placements.</summary>
    public required IReadOnlyList<Guid> ModuleIds { get; init; }

    /// <summary>The state of the involved placements when acknowledged.</summary>
    public required string Fingerprint { get; init; }

    public string? Note { get; init; }
    public required DateTimeOffset Time { get; init; }
}
