using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>
/// One module in a line, with the properties of its placement that the planner can change (HP-16). A line is a list
/// of modules in order, each turned or not. Not turned, the module's left exit joins the previous module and its
/// right exit the next; turned 180°, the other way round. Nothing inside the module drawing is referred to, so a
/// module can be redrawn or replaced without the plan breaking. Its pose is computed.
/// An end-of-line module has one exit, which joins its only neighbour.
/// </summary>
public sealed record LineEntry : ExtensibleRecord
{
    /// <summary>The embedded module; a module is placed at most once per plan (HP-7).</summary>
    public required Guid ModuleId { get; init; }

    /// <summary>The module is turned 180°: its right exit joins the previous module (HP-4).</summary>
    public bool IsTurned { get; init; }

    /// <summary>
    /// For a module with three or more exits: the exit the line continues from. Null means the first exit on the side
    /// facing the next module, which is all a module with two exits needs.
    /// </summary>
    public ExitPlace? ContinuesFrom { get; init; }

    /// <summary>Raises (positive) or lowers the whole module, mm; 0 = standard height (HP-13).</summary>
    public double LegAdjustment { get; init; }

    public string? Note { get; init; }

    /// <summary>The exit that joins the previous module in the line.</summary>
    [JsonIgnore]
    public ExitPlace Entry => IsTurned ? ExitPlace.Right : ExitPlace.Left;

    /// <summary>The exit that the line continues from, to the next module.</summary>
    [JsonIgnore]
    public ExitPlace Exit => ContinuesFrom ?? (IsTurned ? ExitPlace.Left : ExitPlace.Right);

    /// <summary>The module turned around, so that its other end joins the previous module (HP-4).</summary>
    public LineEntry Flipped() => this with { IsTurned = !IsTurned };
}
