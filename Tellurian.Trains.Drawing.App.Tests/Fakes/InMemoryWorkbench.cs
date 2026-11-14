using Tellurian.Trains.Drawing.App.Storage;

namespace Tellurian.Trains.Drawing.App.Tests.Fakes;

/// <summary>A workbench in memory, with the same ordering and recent-file rules as the IndexedDB one.</summary>
internal sealed class InMemoryWorkbench : IWorkbench
{
    public Dictionary<Guid, Draft> Drafts { get; } = [];
    public List<RecentFile> Recent { get; } = [];
    public Dictionary<string, string> Values { get; } = [];
    public int PersistenceRequests { get; private set; }

    public Task<IReadOnlyList<Draft>> GetDraftsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Draft>>([.. Drafts.Values.OrderByDescending(d => d.Modified)]);

    public Task<Draft?> GetDraftAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Drafts.GetValueOrDefault(id));

    public Task SaveDraftAsync(Draft draft, CancellationToken cancellationToken = default)
    {
        Drafts[draft.Id] = draft;
        return Task.CompletedTask;
    }

    public Task DeleteDraftAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Drafts.Remove(id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RecentFile>> GetRecentFilesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RecentFile>>([.. Recent.OrderByDescending(f => f.Used)]);

    public Task AddRecentFileAsync(RecentFile file, CancellationToken cancellationToken = default)
    {
        var (kept, _) = RecentFiles.Add(Recent, file);
        Recent.Clear();
        Recent.AddRange(kept);
        return Task.CompletedTask;
    }

    public Task RemoveRecentFileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Recent.RemoveAll(f => f.Id == id);
        return Task.CompletedTask;
    }

    public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(Values.GetValueOrDefault(key));

    public Task SetValueAsync(string key, string json, CancellationToken cancellationToken = default)
    {
        Values[key] = json;
        return Task.CompletedTask;
    }

    public Task<bool> RequestPersistenceAsync(CancellationToken cancellationToken = default)
    {
        PersistenceRequests++;
        return Task.FromResult(true);
    }
}
