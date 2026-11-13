namespace Tellurian.Trains.Drawing.Model.AutoDraw;

/// <summary>
/// An assumption or problem in an auto-draw, shown to the user, e.g. "exits inferred: 2 × FREMO-H0-B96".
/// The message is in English; UIs can localise by <see cref="Code"/>.
/// </summary>
public sealed record AutoDrawNote(AutoDrawNoteCode Code, string Message)
{
    public override string ToString() => Message;
}
