namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>A physically separate baseboard part of a module variant.</summary>
public sealed record Section
{
    /// <summary>Unique within the variant, e.g. "1", "2".</summary>
    public required string Label { get; init; }

    /// <summary>A closed path in module coordinates.</summary>
    public required PathDrawing Outline { get; init; }
}
