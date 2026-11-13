namespace Tellurian.Trains.Drawing.Model;

/// <summary>A reference to a source document, such as a FREMO or MMM end profile PDF.</summary>
public sealed record DocumentReference
{
    public string? Title { get; init; }
    public string? Url { get; init; }
    public string? Revision { get; init; }
}
