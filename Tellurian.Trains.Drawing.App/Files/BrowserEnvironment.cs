namespace Tellurian.Trains.Drawing.App.Files;

/// <summary>What the browser supports, detected once before the app starts rendering.</summary>
public sealed class BrowserEnvironment(FilesInterop files)
{
    public BrowserCapabilities Capabilities { get; private set; } = new();

    public async Task InitializeAsync() => Capabilities = await files.CapabilitiesAsync();
}
