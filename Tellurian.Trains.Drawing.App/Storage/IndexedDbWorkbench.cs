using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.JSInterop;

namespace Tellurian.Trains.Drawing.App.Storage;

/// <summary>
/// The workbench in the browser's IndexedDB, through <c>js/workbench.js</c>.
/// Failures in the browser are reported as <see cref="StorageException"/>.
/// </summary>
public sealed class IndexedDbWorkbench(IJSRuntime js) : IWorkbench, IAsyncDisposable
{
    private const string ModulePath = "./js/workbench.js";
    private const string Drafts = "drafts";
    private const string Recent = "recent";
    private const string Values = "values";

    private Task<IJSObjectReference>? _module;

    private static WorkbenchJsonContext Context => WorkbenchJsonContext.Default;

    public async Task<IReadOnlyList<Draft>> GetDraftsAsync(CancellationToken cancellationToken = default) =>
        (await GetAllAsync(Drafts, Context.ListDraft, cancellationToken)).OrderByDescending(d => d.Modified).ToList();

    public Task<Draft?> GetDraftAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync(Drafts, id.ToString(), Context.Draft, cancellationToken);

    public Task SaveDraftAsync(Draft draft, CancellationToken cancellationToken = default) =>
        CallAsync("put", cancellationToken, Drafts, JsonSerializer.Serialize(draft, Context.Draft));

    public Task DeleteDraftAsync(Guid id, CancellationToken cancellationToken = default) =>
        CallAsync("remove", cancellationToken, Drafts, id.ToString());

    public async Task<IReadOnlyList<RecentFile>> GetRecentFilesAsync(CancellationToken cancellationToken = default) =>
        (await GetAllAsync(Recent, Context.ListRecentFile, cancellationToken)).OrderByDescending(f => f.Used).ToList();

    public async Task AddRecentFileAsync(RecentFile file, CancellationToken cancellationToken = default)
    {
        var (_, dropped) = RecentFiles.Add(await GetRecentFilesAsync(cancellationToken), file);
        await CallAsync("put", cancellationToken, Recent, JsonSerializer.Serialize(file, Context.RecentFile));
        foreach (var old in dropped) await RemoveRecentFileAsync(old.Id, cancellationToken);
    }

    public async Task RemoveRecentFileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var file = await GetAsync(Recent, id.ToString(), Context.RecentFile, cancellationToken);
        await CallAsync("remove", cancellationToken, Recent, id.ToString());
        if (file?.HandleId is { } handleId) await CallAsync("removeHandle", cancellationToken, handleId);
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default) =>
        (await GetAsync(Values, key, Context.StoredValue, cancellationToken))?.Json;

    public Task SetValueAsync(string key, string json, CancellationToken cancellationToken = default) =>
        CallAsync("put", cancellationToken, Values, JsonSerializer.Serialize(new StoredValue(key, json), Context.StoredValue));

    public async Task<bool> RequestPersistenceAsync(CancellationToken cancellationToken = default) =>
        await CallAsync<bool>("isPersisted", cancellationToken) || await CallAsync<bool>("requestPersistence", cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;
        try
        {
            await (await _module).DisposeAsync();
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException)
        {
        }
    }

    private async Task<List<T>> GetAllAsync<T>(string store, JsonTypeInfo<List<T>> typeInfo, CancellationToken cancellationToken) =>
        JsonSerializer.Deserialize(await CallAsync<string>("getAll", cancellationToken, store), typeInfo) ?? [];

    private async Task<T?> GetAsync<T>(string store, string id, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken) where T : class =>
        await CallAsync<string?>("get", cancellationToken, store, id) is { } json ? JsonSerializer.Deserialize(json, typeInfo) : null;

    private async Task<T> CallAsync<T>(string function, CancellationToken cancellationToken, params object?[] args)
    {
        try
        {
            _module ??= js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
            return await (await _module).InvokeAsync<T>(function, cancellationToken, args);
        }
        catch (JSException ex)
        {
            throw new StorageException(ex.Message, ex);
        }
    }

    private Task CallAsync(string function, CancellationToken cancellationToken, params object?[] args) =>
        CallAsync<object?>(function, cancellationToken, args);
}
