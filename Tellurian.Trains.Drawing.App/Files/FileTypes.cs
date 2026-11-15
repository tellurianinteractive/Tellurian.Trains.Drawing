using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Files;

/// <summary>
/// Extensions offered by the open and folder pickers: <c>.json</c>, as the pickers don't accept extensions with two
/// dots like <c>.module.json</c>, plus the legacy extensions of files saved before version 0.5. The kind is read
/// from each file's content.
/// </summary>
public static class FileTypes
{
    /// <summary>Every kind of drawing file.</summary>
    public static IReadOnlyList<string> Drawings { get; } = Of(Enum.GetValues<DrawingFileKind>());

    public static IReadOnlyList<string> Of(params DrawingFileKind[] kinds) =>
        [DrawingFileFormat.JsonExtension, .. kinds.Select(DrawingFileFormat.LegacyExtension)];
}
