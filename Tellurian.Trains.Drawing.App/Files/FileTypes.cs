using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Files;

/// <summary>Extensions offered by the open and folder pickers.</summary>
public static class FileTypes
{
    /// <summary>Every kind of drawing file.</summary>
    public static IReadOnlyList<string> Drawings { get; } = [.. Enum.GetValues<DrawingFileKind>().Select(DrawingFileFormat.Extension)];

    public static IReadOnlyList<string> Of(params DrawingFileKind[] kinds) => [.. kinds.Select(DrawingFileFormat.Extension)];
}
