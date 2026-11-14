namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>How an imported end profile relates to the catalogue.</summary>
public enum EndProfileImportStatus
{
    /// <summary>The catalogue has no profile with the same id or designation and scale.</summary>
    New,

    /// <summary>The catalogue has the same profile.</summary>
    Unchanged,

    /// <summary>The catalogue has the profile, but its drawing or properties differ.</summary>
    Changed,
}
