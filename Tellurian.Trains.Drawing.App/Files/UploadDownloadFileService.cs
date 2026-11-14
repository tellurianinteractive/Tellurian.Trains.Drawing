namespace Tellurian.Trains.Drawing.App.Files;

/// <summary>
/// Files for browsers without the File System Access API (Firefox, Safari): Open uploads, Save downloads
/// a new file, and folders must be picked again to be re-read.
/// </summary>
public sealed class UploadDownloadFileService(FilesInterop files) : IFileService
{
    public bool CanSaveInPlace => false;
    public bool CanReopen => false;

    public async Task<IReadOnlyList<OpenedFile>?> OpenAsync(IReadOnlyList<string> extensions, bool multiple, CancellationToken cancellationToken = default) =>
        await files.InvokeAsync("uploadFiles", FilesInterop.Context.ListOpenedFile, cancellationToken, extensions.ToArray(), multiple);

    public Task<Stream?> OpenArchiveAsync(CancellationToken cancellationToken = default) =>
        files.InvokeStreamAsync("uploadArchive", cancellationToken);

    public Task<OpenedFolder?> OpenFolderAsync(IReadOnlyList<string> extensions, CancellationToken cancellationToken = default) =>
        files.InvokeAsync("uploadFolder", FilesInterop.Context.OpenedFolder, cancellationToken, extensions.ToArray());

    public Task<OpenedFile?> ReopenAsync(string handleId, CancellationToken cancellationToken = default) =>
        Task.FromResult<OpenedFile?>(null);

    public Task<OpenedFolder?> RereadFolderAsync(string handleId, IReadOnlyList<string> extensions, CancellationToken cancellationToken = default) =>
        Task.FromResult<OpenedFolder?>(null);

    /// <summary>Downloads the file; the browser decides where it goes. The suggested name lets the user replace the old file.</summary>
    public Task<SavedFile?> SaveAsync(string text, string suggestedName, string? handleId, bool saveAs, CancellationToken cancellationToken = default) =>
        files.InvokeAsync("download", FilesInterop.Context.SavedFile, cancellationToken, text, suggestedName);
}
