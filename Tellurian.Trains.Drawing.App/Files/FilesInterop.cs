using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.JSInterop;
using Tellurian.Trains.Drawing.App.Storage;

namespace Tellurian.Trains.Drawing.App.Files;

/// <summary>
/// Calls into <c>js/files.js</c>; results come back as JSON, or null when the user cancels.
/// Failures in the browser are reported as <see cref="FileOperationException"/>.
/// </summary>
public sealed class FilesInterop(IJSRuntime js) : IAsyncDisposable
{
    private const string ModulePath = "./js/files.js";
    private const long MaxArchiveSize = 256 * 1024 * 1024;

    private Task<IJSObjectReference>? _module;

    internal static WorkbenchJsonContext Context => WorkbenchJsonContext.Default;

    public async Task<BrowserCapabilities> CapabilitiesAsync()
    {
        try
        {
            return await InvokeAsync("capabilities", Context.BrowserCapabilities, CancellationToken.None) ?? new();
        }
        catch (FileOperationException)
        {
            return new();
        }
    }

    public async Task<T?> InvokeAsync<T>(string function, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken, params object?[] args)
    {
        try
        {
            var json = await (await Module()).InvokeAsync<string?>(function, cancellationToken, args);
            return json is null ? default : JsonSerializer.Deserialize(json, typeInfo);
        }
        catch (JSException ex)
        {
            throw new FileOperationException(ex.Message, ex);
        }
    }

    /// <summary>Calls a function that returns bytes, or null when the user cancels.</summary>
    public async Task<Stream?> InvokeStreamAsync(string function, CancellationToken cancellationToken)
    {
        try
        {
            var reference = await (await Module()).InvokeAsync<IJSStreamReference?>(function, cancellationToken);
            if (reference is null) return null;
            await using (reference)
            {
                var buffer = new MemoryStream();
                await using var stream = await reference.OpenReadStreamAsync(MaxArchiveSize, cancellationToken);
                await stream.CopyToAsync(buffer, cancellationToken);
                buffer.Position = 0;
                return buffer;
            }
        }
        catch (JSException ex)
        {
            throw new FileOperationException(ex.Message, ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;
        try
        {
            await (await _module).DisposeAsync();
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException)
        {
        }
    }

    private Task<IJSObjectReference> Module()
    {
        try
        {
            return _module ??= js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
        }
        catch (JSException ex)
        {
            throw new FileOperationException(ex.Message, ex);
        }
    }
}
