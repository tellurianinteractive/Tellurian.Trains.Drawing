namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>A physically separate baseboard part of a module drawing.</summary>
public sealed record Section : ExtensibleRecord
{
    /// <summary>Unique within the drawing, e.g. "1", "2".</summary>
    public required string Label { get; init; }

    /// <summary>A closed path in module coordinates.</summary>
    public required PathDrawing Outline { get; init; }
}
