using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Tellurian.Localization;
using Tellurian.Localization.DependencyInjection;
using Tellurian.Trains.Drawing.App;
using Tellurian.Trains.Drawing.App.Spikes.Resources;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Localization (spike B). The culture itself is chosen before .NET starts: index.html passes the
// stored culture to Blazor.start({ applicationCulture }), so the right ICU data and satellite assemblies load.
Language[] languages =
[
    new("en", true),
    new("sv", true) { CultureCode = "SE" },
    new("de", true) { CapitalizesNouns = true },
    new("da", true) { CultureCode = "DK" },
    new("nb", true) { CultureCode = "NO" },
];
builder.Services.AddLanguageService(languages);
builder.Services.AddResxResourceProviders([typeof(SpikeTexts)]);
builder.Services.AddHttpMarkdownResourceProvider("Content", neutralLanguage: "en");

await builder.Build().RunAsync();
