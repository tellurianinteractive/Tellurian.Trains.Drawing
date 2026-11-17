namespace Tellurian.Trains.Drawing.App.Files;

/// <summary>
/// The browser the app runs in, for advice that depends on it, e.g. how to make it keep the workbench. Other Chromium
/// browsers, such as Brave, Vivaldi and Opera, have their own settings and get the general advice.
/// </summary>
public enum BrowserKind
{
    Other,
    Chrome,
    Edge,
    Firefox,
    Safari,
}
