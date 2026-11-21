namespace Tellurian.Trains.Drawing.Model.AutoDraw;

/// <summary>
/// Stored on an auto-drawn drawing (ModuleDrawing §8.7). While it is there, the drawing can be
/// re-generated from changed parameters; editing the drawing by hand removes it.
/// </summary>
public sealed record AutoDrawRecord : ExtensibleRecord
{
    public required string GeneratorVersion { get; init; }
    public required AutoDrawParameters Parameters { get; init; }
}
