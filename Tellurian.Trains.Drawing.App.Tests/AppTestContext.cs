using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Tellurian.Localization;
using Tellurian.Localization.DependencyInjection;
using Tellurian.Localization.Implementatioms;
using Tellurian.Trains.Drawing.App.Catalogue;
using Tellurian.Trains.Drawing.App.Documents;
using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.App.Tests.Fakes;
using Tellurian.Trains.Drawing.App.Translations;
using Tellurian.Trains.Drawing.App.Translations.Resources;

namespace Tellurian.Trains.Drawing.App.Tests;

/// <summary>A bUnit context with the app's services, an in-memory workbench and a fake file service.</summary>
internal sealed class AppTestContext : BunitContext
{
    // Pages that load Markdown documents render again when the document is read; the first read in a test run
    // can take more than bUnit's default second when all test projects run in parallel. A wait that passes returns at once.
    static AppTestContext() => DefaultWaitTimeout = TimeSpan.FromSeconds(10);

    public AppTestContext(bool canSaveInPlace = true)
    {
        Files = new(canSaveInPlace);
        Services.AddLanguageService(AppLanguages.All);
        Services.AddResxResourceProviders([typeof(AppTexts)]);
        Services.AddSingleton<Texts>();
        // The app fetches the Markdown documents over HTTP; the tests read the same files from the source folder.
        Services.AddKeyedSingleton<IResourceProvider>(ContentDocuments.MarkdownProvider, new MarkdownResourceProvider(RepositoryPaths.Content, AppLanguages.Neutral));
        Services.AddScoped<ContentDocuments>();
        Services.AddSingleton<TimeProvider>(Time);
        Services.AddSingleton<IWorkbench>(Workbench);
        Services.AddSingleton<IFileService>(Files);
        Services.AddSingleton<DrawingDocuments>();
        Services.AddSingleton<global::Tellurian.Trains.Drawing.App.Settings.SettingsService>();
        Services.AddSingleton<CatalogueService>();
        Services.AddSingleton<SafeReload>();
        // An instance, which the container doesn't dispose: the interop it uses can only be disposed asynchronously.
        Services.AddSingleton(new BrowserEnvironment(new FilesInterop(JSInterop.JSRuntime)));
        Services.AddSingleton<BrowserDialogs>();
        Services.AddSingleton<CatalogueProblems>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>Chooses the country with the two-letter <paramref name="code"/> in the settings, e.g. SE for the MMM standards.</summary>
    public void UseCountry(string code) =>
        Services.GetRequiredService<global::Tellurian.Trains.Drawing.App.Settings.SettingsService>().SaveAsync(new() { CountryId = global::Tellurian.Trains.Drawing.App.Settings.Countries.All.Single(c => c.Code == code).Id }).Wait();

    public InMemoryWorkbench Workbench { get; } = new();
    public FakeFileService Files { get; }
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero));
}
