using System.Text.Json.Serialization;

namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>
/// One module in a line, with the properties of its placement that the planner can change (HP-16).
/// Its pose is computed: the entry exit joins the previous entry's exit exit.
/// </summary>
public sealed record LineEntry
{
    /// <summary>The embedded module; a module is placed at most once per plan (HP-7).</summary>
    public required Guid ModuleId { get; init; }

    /// <summary>The variant used in the plan; null uses the module's default variant.</summary>
    public Guid? VariantId { get; init; }

    /// <summary>The exit that joins the previous entry; null for an end-of-line module that starts the line.</summary>
    public string? EntryExit { get; init; }

    /// <summary>The exit that the line continues from; null for an end-of-line module that ends the line.</summary>
    public string? ExitExit { get; init; }

    /// <summary>Raises (positive) or lowers the whole module, mm; 0 = standard height (HP-13).</summary>
    public double LegAdjustment { get; init; }

    public string? Note { get; init; }

    [JsonIgnore]
    public ExitReference? Entry => EntryExit is null ? null : new(ModuleId, EntryExit);

    [JsonIgnore]
    public ExitReference? Exit => ExitExit is null ? null : new(ModuleId, ExitExit);

    /// <summary>The module turned around, so that its other end joins the previous entry (HP-4).</summary>
    public LineEntry Flipped() => this with { EntryExit = ExitExit, ExitExit = EntryExit };
}
