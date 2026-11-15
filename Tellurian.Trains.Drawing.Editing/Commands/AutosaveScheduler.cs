namespace Tellurian.Trains.Drawing.Editing.Commands;

/// <summary>
/// Saves an edited document a short while after the last change (Phase 4, AP-7): every change restarts the
/// delay, so a burst of edits is saved once. <see cref="FlushAsync"/> saves at once, e.g. before a reload.
/// Saves never overlap, and a document changed while it was being saved stays modified and is saved again.
/// </summary>
public sealed class AutosaveScheduler<TDocument> : IAsyncDisposable where TDocument : class
{
    public static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(1);

    private readonly EditorDocument<TDocument> _document;
    private readonly Func<TDocument, CancellationToken, Task> _save;
    private readonly ITimer _timer;
    private readonly SemaphoreSlim _saving = new(1, 1);
    private bool _disposed;

    public AutosaveScheduler(EditorDocument<TDocument> document, Func<TDocument, CancellationToken, Task> save, TimeProvider time, TimeSpan? delay = null)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        ArgumentNullException.ThrowIfNull(time);
        Delay = delay ?? DefaultDelay;
        _timer = time.CreateTimer(_ => _ = SaveIfModifiedAsync(CancellationToken.None), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _document.Changed += OnChanged;
    }

    public TimeSpan Delay { get; }

    /// <summary>True while a change waits for the delay to pass.</summary>
    public bool IsPending { get; private set; }

    /// <summary>The error of the last save, or null if it succeeded.</summary>
    public Exception? LastError { get; private set; }

    /// <summary>Raised after a successful save.</summary>
    public event EventHandler? Saved;

    /// <summary>Raised when a save fails; the document stays modified and is saved again after the next change or flush.</summary>
    public event EventHandler<Exception>? SaveFailed;

    /// <summary>Saves now if the document has changes that aren't saved; returns false if the save failed.</summary>
    public async Task<bool> FlushAsync(CancellationToken cancellationToken = default)
    {
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        await SaveIfModifiedAsync(cancellationToken);
        return LastError is null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _document.Changed -= OnChanged;
        await _timer.DisposeAsync();
        _saving.Dispose();
    }

    private void OnChanged(object? sender, EventArgs e)
    {
        if (_disposed) return;
        IsPending = _document.IsModified;
        _timer.Change(IsPending ? Delay : Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    private async Task SaveIfModifiedAsync(CancellationToken cancellationToken)
    {
        if (_disposed) return;
        await _saving.WaitAsync(cancellationToken);
        try
        {
            IsPending = false;
            if (!_document.IsModified) return;
            var version = _document.Current;
            await _save(version, cancellationToken);
            _document.MarkSaved(version);
            LastError = null;
            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LastError = ex;
            SaveFailed?.Invoke(this, ex);
        }
        finally
        {
            if (!_disposed) _saving.Release();
        }
    }
}
