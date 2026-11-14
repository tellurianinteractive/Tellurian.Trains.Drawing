namespace Tellurian.Trains.Drawing.App.Storage;

/// <summary>Rules for the list of recent files.</summary>
public static class RecentFiles
{
    public const int MaxCount = 20;

    /// <summary>
    /// The list after using <paramref name="file"/>: it replaces an entry for the same drawing, comes first,
    /// and the oldest entries beyond <see cref="MaxCount"/> are dropped.
    /// </summary>
    public static (IReadOnlyList<RecentFile> Kept, IReadOnlyList<RecentFile> Dropped) Add(IEnumerable<RecentFile> current, RecentFile file)
    {
        var ordered = current.Where(f => f.Id != file.Id).Prepend(file).OrderByDescending(f => f.Used).ToList();
        return (ordered.Take(MaxCount).ToList(), ordered.Skip(MaxCount).ToList());
    }
}
