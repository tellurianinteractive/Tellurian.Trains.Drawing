using System.Text.Json.Serialization;

namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>
/// A sequence of modules joined end to end (HP-3). A branch line starts at a free exit of a module in another
/// line (HP-19); a line can end by joining a free exit elsewhere, a rejoining branch or a closed loop (HP-20).
/// </summary>
public sealed record Line
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string? Name { get; init; }

    /// <summary>The modules in order.</summary>
    public IReadOnlyList<LineEntry> Entries { get; init; } = [];

    /// <summary>The free exit of a module in another line where this branch line starts; null for a main line.</summary>
    public ExitReference? StartAt { get; init; }

    /// <summary>The free exit that the last module is joined to; its closure gap is reported (VA-5).</summary>
    public ExitReference? EndAt { get; init; }

    [JsonIgnore]
    public bool IsBranch => StartAt is not null;

    public override string? ToString() => Name;
}
