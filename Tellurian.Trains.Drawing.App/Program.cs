using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Tellurian.Localization;
using Tellurian.Localization.DependencyInjection;
using Tellurian.Trains.Drawing.App;
using Tellurian.Trains.Drawing.App.Catalogue;
using Tellurian.Trains.Drawing.App.Documents;
using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.App.Translations;
using Tellurian.Trains.Drawing.App.Translations.Resources;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Localization (ADR 0003). The culture is chosen before .NET starts: index.html passes the stored
// culture to Blazor.start({ applicationCulture }), so the right ICU data and satellite assemblies load.
builder.Services.AddLanguageService(AppLanguages.All);
builder.Services.AddResxResourceProviders([typeof(AppTexts)]);
builder.Services.AddHttpMarkdownResourceProvider(ContentDocuments.BasePath, neutralLanguage: AppLanguages.Neutral);
builder.Services.AddSingleton<Texts>();
// Scoped like the HTTP Markdown provider it reads from (WebAssembly has one scope, so it still caches for the session).
builder.Services.AddScoped<ContentDocuments>();

// Files and the workbench (ADR 0002). WebAssembly has one scope, so these are singletons.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<FilesInterop>();
builder.Services.AddSingleton<BrowserEnvironment>();
builder.Services.AddSingleton<IFileService>(sp => sp.GetRequiredService<BrowserEnvironment>().Capabilities.FileSystemAccess
    ? new FileSystemAccessFileService(sp.GetRequiredService<FilesInterop>())
    : new UploadDownloadFileService(sp.GetRequiredService<FilesInterop>()));
builder.Services.AddSingleton<IWorkbench, IndexedDbWorkbench>();
builder.Services.AddSingleton<DrawingDocuments>();
builder.Services.AddSingleton<Tellurian.Trains.Drawing.App.Settings.SettingsService>();
builder.Services.AddSingleton<CatalogueService>();
builder.Services.AddSingleton<SafeReload>();
builder.Services.AddSingleton<BrowserDialogs>();
builder.Services.AddSingleton<CatalogueProblems>();

var host = builder.Build();
await host.Services.GetRequiredService<BrowserEnvironment>().InitializeAsync();
await host.RunAsync();
