namespace Tellurian.Trains.Drawing.Model.AutoDraw;

/// <summary>What an auto-draw note is about (ModuleDrawing §8.6). UIs can localise notes by code.</summary>
public enum AutoDrawNoteCode
{
    UnknownStandard,
    StandardMapped,
    ExitsInferred,
    ExitDirectionsUndefined,
    InternalExitsBecameJoints,
    EndProfileNotFound,
    MissingDefaultEndProfile,
    EndProfileWithoutOutline,
    EndOfLineFlagMismatch,
    NoGeometry,
    DefaultRadiusUsed,
    WidthBelowPlateIgnored,
    WideningShortened,
    ThroughTracksMismatch,
    TrackNotGenerated,
    LoopModule,
    CheckAngleOrTurntable,
    JunctionNeedsCompletion,
    SectionsNotGenerated,
    InvalidGeometry,
}
