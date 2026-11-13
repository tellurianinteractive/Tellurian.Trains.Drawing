namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// The part of a side that a zone (operator space, public side) is attached to: from
/// <see cref="From"/> to <see cref="To"/> mm along the side, measured from its start.
/// A null <see cref="To"/> means the end of the side.
/// </summary>
public sealed record SideAttachment
{
    /// <summary>Index into <see cref="ModuleVariant.Sides"/>.</summary>
    public required int SideIndex { get; init; }

    public double From { get; init; }
    public double? To { get; init; }
}
