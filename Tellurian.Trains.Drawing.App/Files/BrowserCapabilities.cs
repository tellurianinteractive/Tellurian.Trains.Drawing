namespace Tellurian.Trains.Drawing.App.Files;

/// <summary>What the browser supports for files (ADR 0002), detected once at start-up.</summary>
public sealed record BrowserCapabilities
{
    /// <summary>The File System Access API: save in place and re-open recent files.</summary>
    public bool FileSystemAccess { get; init; }

    public bool DirectoryPicker { get; init; }

    /// <summary>Files can be opened from the operating system (manifest file handlers).</summary>
    public bool LaunchQueue { get; init; }

    /// <summary>The browser's name as the script recognised it, e.g. <c>Firefox</c>; see <see cref="Kind"/>.</summary>
    public string? Browser { get; init; }

    /// <summary>The browser, for advice that depends on it; <see cref="BrowserKind.Other"/> when not recognised.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public BrowserKind Kind => Enum.TryParse<BrowserKind>(Browser, ignoreCase: true, out var kind) ? kind : BrowserKind.Other;
}
