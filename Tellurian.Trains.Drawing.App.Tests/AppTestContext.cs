using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Tellurian.Localization.DependencyInjection;
using Tellurian.Trains.Drawing.App.Documents;
using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.App.Resources;
using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.App.Tests.Fakes;

namespace Tellurian.Trains.Drawing.App.Tests;

/// <summary>A bUnit context with the app's services, an in-memory workbench and a fake file service.</summary>
internal sealed class AppTestContext : BunitContext
{
    public AppTestContext(bool canSaveInPlace = true)
    {
        Files = new(canSaveInPlace);
        Services.AddLanguageService(AppLanguages.All);
        Services.AddResxResourceProviders([typeof(AppTexts)]);
        Services.AddSingleton<Texts>();
        Services.AddSingleton<TimeProvider>(Time);
        Services.AddSingleton<IWorkbench>(Workbench);
        Services.AddSingleton<IFileService>(Files);
        Services.AddSingleton<DrawingDocuments>();
        Services.AddSingleton<SafeReload>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public InMemoryWorkbench Workbench { get; } = new();
    public FakeFileService Files { get; }
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero));
}
