using Microsoft.JSInterop;

namespace Tellurian.Trains.Drawing.App.Layout;

/// <summary>
/// Decides when the information page is shown at start: once per browser tab, so reloads within the tab
/// (a language change, an update) go straight back to the work.
/// </summary>
public sealed class StartupInformation(IJSRuntime js)
{
    public const string SessionKey = "tellurian-drawing-information-shown";

    private bool _shown;

    /// <summary>True the first time it's asked in a browser tab; false after that.</summary>
    public async Task<bool> ShowOnceAsync()
    {
        if (_shown) return false;
        _shown = true;
        try
        {
            if (await js.InvokeAsync<string?>("sessionStorage.getItem", SessionKey) is not null) return false;
            await js.InvokeVoidAsync("sessionStorage.setItem", SessionKey, "1");
            return true;
        }
        catch (JSException)
        {
            // Without session storage the information page is shown once per app start.
            return true;
        }
    }
}
