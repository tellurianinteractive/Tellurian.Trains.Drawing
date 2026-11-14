using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Tellurian.Trains.Drawing.Components.Tests;

/// <summary>
/// Renders a component as static HTML, the way static server rendering or prerendering does (NF-10, EX-8):
/// no interactivity, so OnAfterRender never runs, and any JS call fails the test.
/// </summary>
internal static class StaticRendering
{
    /// <summary>The HTML, decoded so that tests can look for texts as they are shown.</summary>
    public static async Task<string> RenderAsync<TComponent>(IDictionary<string, object?> parameters) where TComponent : IComponent
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime, NoJSRuntime>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
            System.Net.WebUtility.HtmlDecode((await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters))).ToHtmlString()));
    }

    private sealed class NoJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new InvalidOperationException($"Static rendering must not call JS ({identifier}).");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }
}
