using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.Model.AutoDraw;
using Tellurian.Trains.Drawing.Model.Tracks;

namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// The drawing of a module, in the module's local coordinates: origin at the first exit's end-plate centre,
/// X pointing into the module from it (ModuleDrawing §2). A module has one drawing: a module that can be set up
/// in several ways is several modules, each with a file of its own. Only the Module Registry knows that they belong together.
/// </summary>
public sealed record ModuleDrawing : ExtensibleRecord
{
    public IReadOnlyList<ModuleExit> Exits { get; init; } = [];

    /// <summary>The centre line from the first exit to the second; null if there is none.</summary>
    public PathDrawing? MainPath { get; init; }

    /// <summary>The sides between end plates, in counter-clockwise order.</summary>
    public IReadOnlyList<ModuleSide> Sides { get; init; } = [];

    public IReadOnlyList<Section> Sections { get; init; } = [];
    public IReadOnlyList<InternalJoint> InternalJoints { get; init; } = [];
    /// <summary>
    /// The track elements of the drawing: the geometry of the track plan (spec MD-6). Each refers by
    /// <see cref="TrackElement.StationTrackId"/> to a track of the module's list (the <c>Tracks</c> of the
    /// <see cref="Module"/>, spec MD-8), which has the properties that aren't geometry.
    /// </summary>
    public IReadOnlyList<TrackElement> TrackElements { get; init; } = [];

    /// <summary>
    /// The ids of the module's tracks whose length the user has entered. The length of every other track follows the
    /// drawing (<see cref="StationTrackMeasurement"/>).
    /// </summary>
    public IReadOnlyList<int> TracksWithEnteredLength { get; init; } = [];

    /// <summary>
    /// The ids of the module's tracks for which the user has chosen whether they are stubs. For every other track it
    /// follows the drawing (<see cref="StationTrackMeasurement"/>).
    /// </summary>
    public IReadOnlyList<int> TracksWithEnteredStub { get; init; } = [];
    public IReadOnlyList<OperatorSpace> OperatorSpaces { get; init; } = [];
    public IReadOnlyList<PublicSide> PublicSides { get; init; } = [];
    public IReadOnlyList<DuckUnderArea> DuckUnderAreas { get; init; } = [];
    public IReadOnlyList<ModuleLabel> Labels { get; init; } = [];

    /// <summary>Measures entered in or imported from the Registry, for comparison (MD-10).</summary>
    public RegistryMeasures? RegistryMeasures { get; init; }

    /// <summary>Set while the drawing is as auto-draw generated it (MD-20); null once edited by hand.</summary>
    public AutoDrawRecord? AutoDraw { get; init; }

    [JsonIgnore]
    public ModuleKind Kind => Exits.Count switch
    {
        0 => ModuleKind.Invalid,
        1 => ModuleKind.EndOfLine,
        2 => ModuleKind.PassThrough,
        _ => ModuleKind.Junction,
    };

    public ModuleExit? Exit(string label) => Exits.FirstOrDefault(e => e.Label == label);
}
