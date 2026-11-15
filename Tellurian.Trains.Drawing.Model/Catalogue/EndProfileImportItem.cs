using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>An imported end profile, the existing profile it matches (if any), and what differs.</summary>
public sealed record EndProfileImportItem(EndProfile Imported, EndProfile? Existing, EndProfileImportStatus Status, EndProfileChanges Changes);
