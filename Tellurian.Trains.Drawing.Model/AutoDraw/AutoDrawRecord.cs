namespace Tellurian.Trains.Drawing.Model.AutoDraw;

/// <summary>
/// Stored on an auto-drawn variant (ModuleDrawing §8.7). While it is there, the variant can be
/// re-generated from changed parameters; editing the variant by hand removes it.
/// </summary>
public sealed record AutoDrawRecord
{
    public required string GeneratorVersion { get; init; }
    public required AutoDrawParameters Parameters { get; init; }
}
