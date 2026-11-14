using Tellurian.Trains.Drawing.App.Files;

namespace Tellurian.Trains.Drawing.App.Tests.Fakes;

/// <summary>A file service that returns prepared files and records what was saved.</summary>
internal sealed class FakeFileService(bool canSaveInPlace = true) : IFileService
{
    public bool CanSaveInPlace => canSaveInPlace;
    public bool CanReopen => canSaveInPlace;

    /// <summary>The files the next open returns; null simulates a cancelled picker.</summary>
    public IReadOnlyList<OpenedFile>? FilesToOpen { get; set; }

    public Dictionary<string, OpenedFile> Reopenable { get; } = [];

    /// <summary>The name the next save picks; null simulates a cancelled picker.</summary>
    public string? SaveAsName { get; set; } = "saved.tdmodule";

    public List<(string Text, string SuggestedName, string? HandleId, bool SaveAs)> Saves { get; } = [];

    public Task<IReadOnlyList<OpenedFile>?> OpenAsync(IReadOnlyList<string> extensions, bool multiple, CancellationToken cancellationToken = default) =>
        Task.FromResult(FilesToOpen);

    public Task<Stream?> OpenArchiveAsync(CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);

    public Task<OpenedFolder?> OpenFolderAsync(IReadOnlyList<string> extensions, CancellationToken cancellationToken = default) =>
        Task.FromResult<OpenedFolder?>(null);

    public Task<OpenedFile?> ReopenAsync(string handleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Reopenable.GetValueOrDefault(handleId));

    public Task<OpenedFolder?> RereadFolderAsync(string handleId, IReadOnlyList<string> extensions, CancellationToken cancellationToken = default) =>
        Task.FromResult<OpenedFolder?>(null);

    public Task<SavedFile?> SaveAsync(string text, string suggestedName, string? handleId, bool saveAs, CancellationToken cancellationToken = default)
    {
        Saves.Add((text, suggestedName, handleId, saveAs));
        if (!canSaveInPlace) return Task.FromResult<SavedFile?>(new(suggestedName));
        if (!saveAs && handleId is not null) return Task.FromResult<SavedFile?>(new(suggestedName, handleId));
        return Task.FromResult(SaveAsName is null ? null : new SavedFile(SaveAsName, "new-handle"));
    }
}
