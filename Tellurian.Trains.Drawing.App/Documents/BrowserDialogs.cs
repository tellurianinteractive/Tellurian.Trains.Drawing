using Microsoft.JSInterop;

namespace Tellurian.Trains.Drawing.App.Documents;

/// <summary>The browser's own dialogs, for questions that need an answer before something is lost.</summary>
public sealed class BrowserDialogs(IJSRuntime js)
{
    /// <summary>Asks a yes/no question; false if the user declines or the dialog can't be shown.</summary>
    public async Task<bool> ConfirmAsync(string question)
    {
        try
        {
            return await js.InvokeAsync<bool>("confirm", question);
        }
        catch (JSException)
        {
            return false;
        }
    }
}
