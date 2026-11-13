namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>
/// Extra requirements on top of a base standard, e.g. MMM-Summer and MMM-Modern on MMM-2R.
/// </summary>
public sealed record StandardAddOn
{
    public required Guid Id { get; init; }
    public required string ShortName { get; init; }
    public required Guid BaseStandardId { get; init; }
    public string? Description { get; init; }
    public DocumentReference? Source { get; init; }

    public override string ToString() => ShortName;
}
