using System.Text.Json;
using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.Editing.Commands;

namespace Tellurian.Trains.Drawing.App.Documents;

/// <summary>
/// Connects an open editor to the workbench (Phase 4, AP-7): the document is autosaved a moment after each
/// change, every reload the app starts flushes it first, and the editor state (view and selection) is stored
/// so the editor reopens as it was. Dispose it when the editor closes.
/// </summary>
public sealed class EditorSession<TDocument> : IAsyncDisposable where TDocument : class
{
    private readonly string _key;
    private readonly IWorkbench _workbench;
    private readonly IDisposable _reloadRegistration;
    private bool _disposed;

    public EditorSession(string key, EditorDocument<TDocument> document, Func<TDocument, CancellationToken, Task> save,
        IWorkbench workbench, SafeReload reload, TimeProvider time, TimeSpan? delay = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(reload);
        _key = key;
        _workbench = workbench ?? throw new ArgumentNullException(nameof(workbench));
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Autosave = new(document, save, time, delay);
        _reloadRegistration = reload.Register(FlushForReloadAsync);
    }

    public EditorDocument<TDocument> Document { get; }
    public AutosaveScheduler<TDocument> Autosave { get; }

    /// <summary>Gives the current editor state when it is stored; without it, only the document is saved.</summary>
    public Func<EditorState>? CaptureState { get; set; }

    /// <summary>The stored state of the editor with <paramref name="key"/>, if any.</summary>
    public static async Task<EditorState?> LoadStateAsync(IWorkbench workbench, string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbench);
        var json = await workbench.GetValueAsync(EditorState.StorageKey(key), cancellationToken);
        if (json is null) return null;
        try
        {
            return JsonSerializer.Deserialize(json, WorkbenchJsonContext.Default.EditorState);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Saves the document if it has changes, and the editor state; returns false if the document couldn't be saved.</summary>
    public async Task<bool> FlushAsync(CancellationToken cancellationToken = default)
    {
        var saved = await Autosave.FlushAsync(cancellationToken);
        if (CaptureState is { } capture)
            await _workbench.SetValueAsync(EditorState.StorageKey(_key), JsonSerializer.Serialize(capture(), WorkbenchJsonContext.Default.EditorState), cancellationToken);
        return saved;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _reloadRegistration.Dispose();
        try
        {
            await FlushAsync();
        }
        catch (StorageException)
        {
            // The editor is closing; the draft keeps the last version that could be saved.
        }
        await Autosave.DisposeAsync();
    }

    /// <summary>For <see cref="SafeReload"/>: a failed save must cancel the reload, so it throws.</summary>
    private async Task FlushForReloadAsync(CancellationToken cancellationToken)
    {
        if (!await FlushAsync(cancellationToken))
            throw Autosave.LastError ?? new StorageException("The drawing couldn't be saved.");
    }
}
