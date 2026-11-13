namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// The outline between two consecutive end plates, walking counter-clockwise around the module:
/// from the right corner of <see cref="FromExit"/>'s plate to the left corner of <see cref="ToExit"/>'s
/// (corners as seen standing outside the plate). An end-of-line module's only side starts and ends at its one exit.
/// </summary>
public sealed record ModuleSide
{
    public required string FromExit { get; init; }
    public required string ToExit { get; init; }
    public SideMode Mode { get; init; }

    /// <summary>The geometry, also stored for auto sides so that files can be shown without regenerating.</summary>
    public required PathDrawing Path { get; init; }
}
