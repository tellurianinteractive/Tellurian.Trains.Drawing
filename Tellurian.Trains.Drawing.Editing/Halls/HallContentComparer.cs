using System.Text.Json;

namespace Tellurian.Trains.Drawing.Editing.Halls;

/// <summary>
/// Compares halls by what they contain, as written to a file: the shared model's <see cref="Hall"/> is a mutable class
/// that every edit copies, so two equal drawings are two objects, and a command that changes nothing adds no undo step.
/// </summary>
public sealed class HallContentComparer : IEqualityComparer<Hall>
{
    /// <summary>The comparer.</summary>
    public static HallContentComparer Comparer { get; } = new();

    /// <inheritdoc/>
    public bool Equals(Hall? x, Hall? y) =>
        ReferenceEquals(x, y) || (x is not null && y is not null && Json(x) == Json(y));

    /// <inheritdoc/>
    public int GetHashCode(Hall obj) => Json(obj).GetHashCode(StringComparison.Ordinal);

    private static string Json(Hall hall) => JsonSerializer.Serialize(hall, DrawingJsonContext.Default.Hall);
}
