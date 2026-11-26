namespace Tellurian.Trains.Drawing.App.Layout;

/// <summary>
/// The action a page is busy with, such as reading files or calling the Module Registry. While it runs, the page's
/// <see cref="BusyButton"/>s are disabled, so no second action starts, and the one that started it shows a spinner.
/// Each page has its own.
/// </summary>
public sealed class BusyState
{
    /// <summary>The key of the running action; null when the page isn't busy.</summary>
    public string? Running { get; private set; }

    public bool IsBusy => Running is not null;

    /// <summary>Raised when an action starts and when it ends, so the busy buttons show it.</summary>
    public event Action? Changed;

    /// <summary>Whether the action <paramref name="key"/> is the running one.</summary>
    public bool Is(string key) => Running == key;

    /// <summary>
    /// Runs <paramref name="action"/> as the action <paramref name="key"/>, unless the page is busy with another.
    /// The page is not busy afterwards, also when the action fails.
    /// </summary>
    public async Task RunAsync(string key, Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsBusy) return;
        Running = key;
        Changed?.Invoke();
        try
        {
            // Let the browser show the spinner before work that doesn't yield, e.g. reading a large file.
            await Task.Yield();
            await action();
        }
        finally
        {
            Running = null;
            Changed?.Invoke();
        }
    }
}
