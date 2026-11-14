using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Storage;

/// <summary>A file the user opened or saved recently, shown on the start page (UC-5).</summary>
public sealed record RecentFile
{
    /// <summary>The id of the drawing in the file.</summary>
    public required Guid Id { get; init; }

    public required DrawingFileKind Kind { get; init; }
    public required string Name { get; init; }
    public required string FileName { get; init; }

    /// <summary>The stored file handle; without one, the file must be picked again to re-open it.</summary>
    public string? HandleId { get; init; }

    /// <summary>When the file was last opened or saved, UTC.</summary>
    public required DateTimeOffset Used { get; init; }
}
