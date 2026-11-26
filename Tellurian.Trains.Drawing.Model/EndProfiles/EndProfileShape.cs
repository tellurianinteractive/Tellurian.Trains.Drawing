namespace Tellurian.Trains.Drawing.Model.EndProfiles;

/// <summary>
/// Whether an end profile is drawn. The Module Registry's catalogue has end profiles without a shape, only designation,
/// scale and standards, for those it has no drawing of yet; they can be kept in the catalogue and drawn later, but a
/// module exit can't use one, since without an outline the plate has no width.
/// </summary>
public static class EndProfileShape
{
    /// <summary>Whether <paramref name="profile"/> has an end-plate outline.</summary>
    public static bool HasOutline(this EndProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return profile.Outline is not null || profile.Plate is not null;
    }

    /// <summary>The end profiles that a module exit can use: those with an outline.</summary>
    public static IEnumerable<EndProfile> WithOutline(this IEnumerable<EndProfile> profiles) => profiles.Where(p => p.HasOutline());
}
