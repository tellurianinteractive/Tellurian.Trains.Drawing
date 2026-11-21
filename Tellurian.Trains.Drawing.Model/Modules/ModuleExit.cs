using Tellurian.Trains.Schedules.Model.Geometry;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// An outer module end that can connect to another module (Registry <c>ModuleExit</c>).
/// Its label is unique within the drawing and is text for the reader: on the module it is seldom marked. A hall plan
/// refers to an exit by its side and number (<see cref="ExitPlace"/>).
/// </summary>
public sealed record ModuleExit : ExtensibleRecord
{
    public required string Label { get; init; }
    public ExitDirection Direction { get; init; }

    /// <summary>
    /// The end profile. Each exit has its own, of the module's scale; it may belong to another standard than
    /// the module's, e.g. FREMO at one end and MMM at the other (MD-5).
    /// </summary>
    public required CatalogueReference EndProfile { get; init; }

    /// <summary>
    /// Whether the end plate is built as drawn or mirrored; a plate can usually be fitted either side out.
    /// Only meaningful for asymmetric end profiles.
    /// </summary>
    public ProfileOrientation Orientation { get; init; }

    /// <summary>Rail top relative to the standard's rail-top height, mm; 0 except on ramp modules.</summary>
    public double RailTopOffset { get; init; }

    /// <summary>The centre of the end plate on its joint face, heading out of the module.</summary>
    public required Pose Pose { get; init; }

    /// <summary>True while a provisionally placed junction exit hasn't been positioned by the user (ModuleDrawing §8.5).</summary>
    public bool IsPositionProvisional { get; init; }

    public override string ToString() => Label;
}
