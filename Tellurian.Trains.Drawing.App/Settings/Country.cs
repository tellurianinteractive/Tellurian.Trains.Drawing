namespace Tellurian.Trains.Drawing.App.Settings;

/// <summary>A country from the Module Registry: its id there, its English name and its two-letter domain suffix.</summary>
public sealed record Country
{
    public required int Id { get; init; }
    public required string EnglishName { get; init; }

    /// <summary>The ISO 3166 two-letter code in lower case, as the Registry stores it, e.g. <c>se</c>.</summary>
    public required string DomainSuffix { get; init; }

    /// <summary>
    /// The ISO 3166 two-letter code in capitals, as module standards use it, e.g. <c>SE</c>. The domain suffix
    /// is the ISO code except for the United Kingdom, whose domain is <c>uk</c> but whose ISO code is <c>GB</c>.
    /// </summary>
    public string Code => DomainSuffix.Trim().ToUpperInvariant() switch
    {
        "UK" => "GB",
        var code => code,
    };
}
