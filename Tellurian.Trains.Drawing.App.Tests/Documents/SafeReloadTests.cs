using Bunit;
using Tellurian.Trains.Drawing.App.Documents;
using Tellurian.Trains.Drawing.App.Storage;

namespace Tellurian.Trains.Drawing.App.Tests.Documents;

/// <summary>AP-7: no data loss when the app reloads itself. Registered flushers stand in for open drawings.</summary>
[TestClass]
public sealed class SafeReloadTests
{
    private static (BunitContext Context, SafeReload Reload) Create()
    {
        var context = new BunitContext();
        context.JSInterop.SetupVoid("localStorage.setItem", _ => true).SetVoidResult();
        context.JSInterop.SetupVoid("location.reload").SetVoidResult();
        return (context, new SafeReload(context.JSInterop.JSRuntime));
    }

    [TestMethod]
    public async Task LanguageChangeFlushesTwoOpenDrawingsBeforeReloading()
    {
        var (context, reload) = Create();
        using var _ = context;
        var flushed = new List<string>();
        using var first = reload.Register(_ => { flushed.Add("module"); return Task.CompletedTask; });
        using var second = reload.Register(_ => { flushed.Add("hall plan"); return Task.CompletedTask; });

        var error = await reload.ReloadAsync("sv-SE");

        Assert.IsNull(error, error?.ToString());
        CollectionAssert.AreEqual(new[] { "module", "hall plan" }, flushed);
        var calls = context.JSInterop.Invocations.Select(i => i.Identifier).ToList();
        CollectionAssert.AreEqual(new[] { "localStorage.setItem", "location.reload" }, calls);
        Assert.AreEqual("sv-SE", context.JSInterop.Invocations["localStorage.setItem"].Single().Arguments[1]);
    }

    [TestMethod]
    public async Task BlockedStorageCancelsTheReload()
    {
        var (context, reload) = Create();
        using var _ = context;
        using var registration = reload.Register(_ => throw new StorageException("The database is blocked by another tab."));

        var error = await reload.ReloadAsync("de");

        Assert.IsInstanceOfType<StorageException>(error);
        Assert.IsEmpty(context.JSInterop.Invocations);
    }

    [TestMethod]
    public async Task UpdateRunsAfterTheUnsavedHallPlanIsFlushed()
    {
        var (context, reload) = Create();
        using var _ = context;
        var steps = new List<string>();
        using var registration = reload.Register(_ => { steps.Add("flush hall plan"); return Task.CompletedTask; });

        var error = await reload.RunAsync(_ => { steps.Add("activate update"); return Task.CompletedTask; });

        Assert.IsNull(error);
        CollectionAssert.AreEqual(new[] { "flush hall plan", "activate update" }, steps);
    }

    [TestMethod]
    public async Task ClosedEditorsAreNoLongerFlushed()
    {
        var (context, reload) = Create();
        using var _ = context;
        var flushes = 0;
        reload.Register(_ => { flushes++; return Task.CompletedTask; }).Dispose();

        await reload.ReloadAsync();

        Assert.AreEqual(0, flushes);
    }
}
