namespace Tellurian.Trains.Drawing.App.Files;

/// <summary>
/// The drawing files of a folder the user picked. With a stored handle the folder can be re-read
/// without picking it again (UC-7).
/// </summary>
public sealed record OpenedFolder(string Name, IReadOnlyList<OpenedFile> Files, string? HandleId = null);
