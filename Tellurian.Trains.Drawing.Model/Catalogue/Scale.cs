namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>A model scale, e.g. H0 1:87. The short name is its key.</summary>
public sealed record Scale
{
    public required string ShortName { get; init; }
    public required int Denominator { get; init; }

    public override string ToString() => $"{ShortName} 1:{Denominator}";
}
