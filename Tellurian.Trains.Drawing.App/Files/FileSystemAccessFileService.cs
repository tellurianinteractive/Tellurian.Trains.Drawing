namespace Tellurian.Trains.Drawing.App.Files;

/// <summary>Files through the File System Access API (Chromium browsers): save in place, re-open from handles.</summary>
public sealed class FileSystemAccessFileService(FilesInterop files) : IFileService
{
    public bool CanSaveInPlace => true;
    public bool CanReopen => true;

    public async Task<IReadOnlyList<OpenedFile>?> OpenAsync(IReadOnlyList<string> extensions, bool multiple, CancellationToken cancellationToken = default) =>
        await files.InvokeAsync("openFiles", FilesInterop.Context.ListOpenedFile, cancellationToken, extensions.ToArray(), multiple);

    public Task<Stream?> OpenArchiveAsync(CancellationToken cancellationToken = default) =>
        files.InvokeStreamAsync("openArchive", cancellationToken);

    public Task<OpenedFolder?> OpenFolderAsync(IReadOnlyList<string> extensions, CancellationToken cancellationToken = default) =>
        files.InvokeAsync("pickFolder", FilesInterop.Context.OpenedFolder, cancellationToken, extensions.ToArray());

    public Task<OpenedFile?> ReopenAsync(string handleId, CancellationToken cancellationToken = default) =>
        files.InvokeAsync("reopen", FilesInterop.Context.OpenedFile, cancellationToken, handleId);

    public Task<OpenedFolder?> RereadFolderAsync(string handleId, IReadOnlyList<string> extensions, CancellationToken cancellationToken = default) =>
        files.InvokeAsync("rereadFolder", FilesInterop.Context.OpenedFolder, cancellationToken, handleId, extensions.ToArray());

    public Task<SavedFile?> SaveAsync(string text, string suggestedName, string? handleId, bool saveAs, CancellationToken cancellationToken = default) =>
        files.InvokeAsync("save", FilesInterop.Context.SavedFile, cancellationToken, text, suggestedName, Path.GetExtension(suggestedName), handleId, saveAs);
}
