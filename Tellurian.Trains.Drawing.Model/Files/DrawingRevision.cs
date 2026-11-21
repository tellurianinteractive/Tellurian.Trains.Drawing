namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>An entry in a drawing's revision log: who changed the drawing, and the last date they saved it.</summary>
public sealed record DrawingRevision : ExtensibleRecord
{
    /// <summary>The user name from the settings of the person who saved the drawing.</summary>
    public required string Name { get; init; }

    /// <summary>The last date this person saved the drawing.</summary>
    public required DateOnly Date { get; init; }
}
