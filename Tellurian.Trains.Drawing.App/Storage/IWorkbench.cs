namespace Tellurian.Trains.Drawing.App.Storage;

/// <summary>
/// The workbench for work in progress (spec §6, ADR 0002): drafts, recent files and named values such as
/// settings and the user catalogue. Files are the primary result; the workbench is not a library.
/// </summary>
public interface IWorkbench
{
    /// <summary>The drafts, most recently changed first.</summary>
    Task<IReadOnlyList<Draft>> GetDraftsAsync(CancellationToken cancellationToken = default);

    Task<Draft?> GetDraftAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveDraftAsync(Draft draft, CancellationToken cancellationToken = default);
    Task DeleteDraftAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The recent files, most recently used first.</summary>
    Task<IReadOnlyList<RecentFile>> GetRecentFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds or updates a recent file; the oldest are forgotten beyond <see cref="RecentFiles.MaxCount"/>.</summary>
    Task AddRecentFileAsync(RecentFile file, CancellationToken cancellationToken = default);

    Task RemoveRecentFileAsync(Guid id, CancellationToken cancellationToken = default);

    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task SetValueAsync(string key, string json, CancellationToken cancellationToken = default);

    /// <summary>Asks the browser to keep the workbench when storage runs low; true if granted.</summary>
    Task<bool> RequestPersistenceAsync(CancellationToken cancellationToken = default);
}
