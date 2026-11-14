namespace Tellurian.Trains.Drawing.App.Files;

/// <summary>
/// Opening and saving files (ADR 0002). Two implementations, chosen by what the browser supports:
/// <see cref="FileSystemAccessFileService"/> saves in place; <see cref="UploadDownloadFileService"/>
/// uploads and downloads. Pickers the user cancels return null. Components never touch files directly.
/// </summary>
public interface IFileService
{
    /// <summary>True if Save writes the file in place; otherwise it downloads a new file.</summary>
    bool CanSaveInPlace { get; }

    /// <summary>True if recent files and folders can be re-opened without picking them again.</summary>
    bool CanReopen { get; }

    Task<IReadOnlyList<OpenedFile>?> OpenAsync(IReadOnlyList<string> extensions, bool multiple, CancellationToken cancellationToken = default);

    /// <summary>Picks a ZIP archive and returns its content.</summary>
    Task<Stream?> OpenArchiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Picks a folder and reads its files with the given extensions.</summary>
    Task<OpenedFolder?> OpenFolderAsync(IReadOnlyList<string> extensions, CancellationToken cancellationToken = default);

    /// <summary>Re-opens a recent file from its stored handle; null if it isn't available.</summary>
    Task<OpenedFile?> ReopenAsync(string handleId, CancellationToken cancellationToken = default);

    /// <summary>Re-reads a folder from its stored handle; null if it isn't available.</summary>
    Task<OpenedFolder?> RereadFolderAsync(string handleId, IReadOnlyList<string> extensions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves <paramref name="text"/>: in place to <paramref name="handleId"/> when possible, otherwise
    /// as a new file named <paramref name="suggestedName"/>. <paramref name="saveAs"/> always asks for a new file.
    /// </summary>
    Task<SavedFile?> SaveAsync(string text, string suggestedName, string? handleId, bool saveAs, CancellationToken cancellationToken = default);
}
