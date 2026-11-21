using System.Text.Json;

namespace Tellurian.Trains.Drawing.Editing.EndProfiles;

/// <summary>
/// Compares end profiles by what they contain, as written to a file: the shared model's <see cref="EndProfile"/> is a
/// mutable class that every edit copies, so two equal drawings are two objects.
/// </summary>
public sealed class EndProfileContentComparer : IEqualityComparer<EndProfile>
{
    /// <summary>The comparer.</summary>
    public static EndProfileContentComparer Comparer { get; } = new();

    /// <inheritdoc/>
    public bool Equals(EndProfile? x, EndProfile? y) =>
        ReferenceEquals(x, y) || (x is not null && y is not null && Json(x) == Json(y));

    /// <inheritdoc/>
    public int GetHashCode(EndProfile obj) => Json(obj).GetHashCode(StringComparison.Ordinal);

    private static string Json(EndProfile profile) => JsonSerializer.Serialize(profile, DrawingJsonContext.Default.EndProfile);
}
