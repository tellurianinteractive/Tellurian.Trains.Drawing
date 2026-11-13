namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>A side, or part of one, that the owner intends to face the public (MD-18).</summary>
public sealed record PublicSide
{
    public required SideAttachment Attachment { get; init; }
}
