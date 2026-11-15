namespace Tellurian.Trains.Drawing.App.Settings;

/// <summary>The user's settings, kept in this browser's workbench. Never written to drawing or catalogue files.</summary>
public sealed record UserSettings
{
    public static UserSettings Empty { get; } = new();

    /// <summary>The name recorded in the revision log of the drawings the user saves.</summary>
    public string? UserName { get; init; }

    /// <summary>The Module Registry id of the country the user works in; standards for other countries aren't offered.</summary>
    public int? CountryId { get; init; }

    /// <summary>
    /// The short name of the scale the user works in, e.g. H0; standards and end profiles of other scales aren't
    /// offered (AP-9), and Module Registry API calls ask for this scale. Null for all scales.
    /// </summary>
    public string? Scale { get; init; }

    /// <summary>The user's key for the Module Registry API.</summary>
    public string? ApiKey { get; init; }
}
