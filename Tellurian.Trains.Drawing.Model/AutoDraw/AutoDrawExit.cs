using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.AutoDraw;

/// <summary>
/// An exit as registered in the Module Registry (<c>ModuleExit</c>). An exit with the end profile
/// <see cref="ModuleAutoDraw.InternalEndProfile"/> is an internal joint between sections, not an exit.
/// </summary>
public sealed record AutoDrawExit
{
    public required string Label { get; init; }
    public ExitDirection Direction { get; init; }

    /// <summary>The end profile designation, e.g. "FREMO-H0-B96".</summary>
    public required string EndProfile { get; init; }

    /// <summary>The end profile id in the catalogue, if known; takes precedence over the designation.</summary>
    public Guid? EndProfileId { get; init; }
}
