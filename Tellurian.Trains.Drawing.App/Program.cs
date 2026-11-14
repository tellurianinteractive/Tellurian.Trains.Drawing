using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Tellurian.Localization;
using Tellurian.Localization.DependencyInjection;
using Tellurian.Trains.Drawing.App;
using Tellurian.Trains.Drawing.App.Documents;
using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.App.Resources;
using Tellurian.Trains.Drawing.App.Storage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Localization (ADR 0003). The culture is chosen before .NET starts: index.html passes the stored
// culture to Blazor.start({ applicationCulture }), so the right ICU data and satellite assemblies load.
builder.Services.AddLanguageService(AppLanguages.All);
builder.Services.AddResxResourceProviders([typeof(AppTexts)]);
builder.Services.AddHttpMarkdownResourceProvider("Content", neutralLanguage: AppLanguages.Neutral);
builder.Services.AddSingleton<Texts>();

// Files and the workbench (ADR 0002). WebAssembly has one scope, so these are singletons.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<FilesInterop>();
builder.Services.AddSingleton<BrowserEnvironment>();
builder.Services.AddSingleton<IFileService>(sp => sp.GetRequiredService<BrowserEnvironment>().Capabilities.FileSystemAccess
    ? new FileSystemAccessFileService(sp.GetRequiredService<FilesInterop>())
    : new UploadDownloadFileService(sp.GetRequiredService<FilesInterop>()));
builder.Services.AddSingleton<IWorkbench, IndexedDbWorkbench>();
builder.Services.AddSingleton<DrawingDocuments>();
builder.Services.AddSingleton<SafeReload>();

var host = builder.Build();
await host.Services.GetRequiredService<BrowserEnvironment>().InitializeAsync();
await host.RunAsync();
