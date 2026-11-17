namespace Tellurian.Trains.Drawing.App.Files;

/// <summary>What the browser supports, detected once before the app starts rendering.</summary>
public sealed class BrowserEnvironment(FilesInterop files)
{
    /// <summary>What the browser supports; detected by <see cref="InitializeAsync"/>, or set by tests.</summary>
    public BrowserCapabilities Capabilities { get; set; } = new();

    public async Task InitializeAsync() => Capabilities = await files.CapabilitiesAsync();
}
