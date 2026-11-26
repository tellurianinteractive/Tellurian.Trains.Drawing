using Bunit;
using Tellurian.Trains.Drawing.App.Layout;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Drawing.Model.Catalogues;

namespace Tellurian.Trains.Drawing.App.Tests.Layout;

[TestClass]
public sealed class BusyButtonTests
{
    [TestMethod]
    public async Task WhileAnActionRunsEveryButtonIsDisabledAndTheRunningOneSpins()
    {
        using var context = new AppTestContext();
        var busy = new BusyState();
        var done = new TaskCompletionSource();
        var runs = 0;
        var first = context.Render<BusyButton>(p => p.Add(x => x.Busy, busy).Add(x => x.Key, "first").Add(x => x.OnClick, async () => { runs++; await done.Task; }).AddChildContent("First"));
        var second = context.Render<BusyButton>(p => p.Add(x => x.Busy, busy).Add(x => x.Key, "second").Add(x => x.OnClick, () => runs++).AddChildContent("Second"));

        var click = first.Find("button").ClickAsync(new());

        first.WaitForAssertion(() => Assert.IsTrue(first.Find("button").HasAttribute("disabled")));
        Assert.IsNotNull(first.Find("button .busy-spinner"));
        Assert.AreEqual("true", first.Find("button").GetAttribute("aria-busy"));
        Assert.IsTrue(second.Find("button").HasAttribute("disabled"));
        Assert.IsEmpty(second.FindAll(".busy-spinner"));

        done.SetResult();
        await click;

        first.WaitForAssertion(() => Assert.IsFalse(first.Find("button").HasAttribute("disabled")));
        Assert.IsFalse(second.Find("button").HasAttribute("disabled"));
        Assert.IsEmpty(first.FindAll(".busy-spinner"));
        Assert.AreEqual(1, runs);
    }

    [TestMethod]
    public async Task ASecondActionDoesntStartWhileOneRuns()
    {
        var busy = new BusyState();
        var done = new TaskCompletionSource();
        var started = new List<string>();

        var first = busy.RunAsync("first", async () => { started.Add("first"); await done.Task; });
        await busy.RunAsync("second", () => { started.Add("second"); return Task.CompletedTask; });
        done.SetResult();
        await first;

        CollectionAssert.AreEqual(new[] { "first" }, started);
        Assert.IsFalse(busy.IsBusy);
    }

    [TestMethod]
    public async Task AFailedActionLeavesThePageNotBusy()
    {
        var busy = new BusyState();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => busy.RunAsync("failing", () => throw new InvalidOperationException()));

        Assert.IsFalse(busy.IsBusy);
    }

    [TestMethod]
    public void TheCataloguePageFetchButtonIsABusyButton()
    {
        using var context = new AppTestContext();
        context.Registry.Json = SampleCatalogue.Json;
        var page = context.Render<CataloguePage>();

        page.Find("#fetch-registry").Click();

        page.WaitForAssertion(() => Assert.IsNotEmpty(page.FindAll(".import-preview")));
        Assert.IsFalse(page.Find("#fetch-registry").HasAttribute("disabled"), "Enabled again when the fetch is done.");
        Assert.IsFalse(page.Find("#import-files").HasAttribute("disabled"));
    }
}
