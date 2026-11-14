using Microsoft.JSInterop;

namespace Tellurian.Trains.Drawing.App.Documents;

/// <summary>
/// Every reload the app starts itself (language change, update) goes through here (AP-7): open editors
/// flush their unsaved work to drafts first, and if that fails the reload is cancelled.
/// </summary>
public sealed class SafeReload(IJSRuntime js)
{
    public const string CultureStorageKey = "tellurian-drawing-culture";

    private readonly List<Func<CancellationToken, Task>> _flushers = [];

    /// <summary>Registers work to flush before a reload; dispose the result when the editor closes.</summary>
    public IDisposable Register(Func<CancellationToken, Task> flush)
    {
        _flushers.Add(flush);
        return new Registration(() => _flushers.Remove(flush));
    }

    /// <summary>
    /// Flushes all open work, then reloads, first storing <paramref name="culture"/> if given.
    /// Returns the error if flushing or reloading failed; then nothing was reloaded.
    /// </summary>
    public async Task<Exception?> ReloadAsync(string? culture = null, CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var flush in _flushers.ToList()) await flush(cancellationToken);
            if (culture is not null) await js.InvokeVoidAsync("localStorage.setItem", cancellationToken, CultureStorageKey, culture);
            await js.InvokeVoidAsync("location.reload", cancellationToken);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ex;
        }
    }

    private sealed class Registration(Action unregister) : IDisposable
    {
        public void Dispose() => unregister();
    }
}
