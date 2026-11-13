using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>
/// Overrides the rule result (spec §3.2) for a specific pair of end profiles, for known exceptions.
/// The pair is unordered and applies to all orientations.
/// </summary>
public sealed record EndProfileCompatibilityOverride
{
    public required Guid FirstEndProfileId { get; init; }
    public required Guid SecondEndProfileId { get; init; }
    public required JointFit Fit { get; init; }
    public string? Note { get; init; }

    public bool Applies(Guid oneEndProfileId, Guid otherEndProfileId) =>
        (FirstEndProfileId == oneEndProfileId && SecondEndProfileId == otherEndProfileId) ||
        (FirstEndProfileId == otherEndProfileId && SecondEndProfileId == oneEndProfileId);
}
