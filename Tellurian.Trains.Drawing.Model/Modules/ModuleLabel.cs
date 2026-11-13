using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>A text label or simple marker in a module drawing. The pose's heading is the text direction.</summary>
public sealed record ModuleLabel
{
    public LabelKind Kind { get; init; }
    public string? Text { get; init; }
    public required Pose2 Pose { get; init; }

    /// <summary>For markers with an extent, such as a platform edge, mm.</summary>
    public double? Length { get; init; }
}
