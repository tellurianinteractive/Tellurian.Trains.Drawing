using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// An outer module end that can connect to another module (Registry <c>ModuleExit</c>).
/// Its label is unique within the variant and is how hall plans refer to it.
/// </summary>
public sealed record ModuleExit
{
    public required string Label { get; init; }
    public ExitDirection Direction { get; init; }

    /// <summary>The end profile; it must belong to the module's standard (MD-5).</summary>
    public required CatalogueReference EndProfile { get; init; }

    /// <summary>Only meaningful for asymmetric end profiles.</summary>
    public ProfileOrientation Orientation { get; init; }

    /// <summary>Rail top relative to the standard's rail-top height, mm; 0 except on ramp modules.</summary>
    public double RailTopOffset { get; init; }

    /// <summary>The centre of the end plate on its joint face, heading out of the module.</summary>
    public required Pose2 Pose { get; init; }

    /// <summary>True while a provisionally placed junction exit hasn't been positioned by the user (ModuleDrawing §8.5).</summary>
    public bool IsPositionProvisional { get; init; }

    public override string ToString() => Label;
}
