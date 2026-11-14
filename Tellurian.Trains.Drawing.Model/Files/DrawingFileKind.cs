namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>The kinds of drawing files (spec §6), stored as <c>kind</c> in the file envelope.</summary>
public enum DrawingFileKind
{
    EndProfile,
    Module,
    Hall,
    HallPlan,
    Catalogue,
}
