namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// An area next to the module where operators stand (MD-17). Either attached to a side with a depth
/// (auto), or a polygon drawn by hand (custom, <see cref="Attachment"/> null). The area is always stored.
/// </summary>
public sealed record OperatorSpace
{
    public SideAttachment? Attachment { get; init; }

    /// <summary>The depth from the side for an attached space, mm.</summary>
    public double? Depth { get; init; }

    /// <summary>A closed path in module coordinates, outside the baseboard.</summary>
    public required PathDrawing Area { get; init; }

    public string? Label { get; init; }
}
