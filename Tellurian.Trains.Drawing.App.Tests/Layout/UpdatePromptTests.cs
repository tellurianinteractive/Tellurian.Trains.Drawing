using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Documents;
using Tellurian.Trains.Drawing.App.Layout;
using Tellurian.Trains.Drawing.App.Storage;

namespace Tellurian.Trains.Drawing.App.Tests.Layout;

[TestClass]
public sealed class UpdatePromptTests
{
    private const string ModulePath = "./Layout/UpdatePrompt.razor.js";

    private static (AppTestContext Context, BunitJSModuleInterop Module) Create(bool isWaiting)
    {
        var context = new AppTestContext();
        context.JSInterop.Mode = JSRuntimeMode.Strict;
        var module = context.JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("subscribe", _ => true).SetResult(isWaiting);
        module.Setup<bool>("apply").SetResult(true);
        module.SetupVoid("unsubscribe").SetVoidResult();
        return (context, module);
    }

    [TestMethod]
    public void NothingIsShownWithoutANewVersion()
    {
        var (context, _) = Create(isWaiting: false);
        using var _ = context;
        var prompt = context.Render<UpdatePrompt>();
        Assert.IsEmpty(prompt.FindAll("#update-available"));
    }

    [TestMethod]
    public void WaitingVersionIsAppliedAfterFlushing()
    {
        var (context, module) = Create(isWaiting: true);
        using var _ = context;
        var flushed = false;
        context.Services.GetRequiredService<SafeReload>().Register(_ => { flushed = true; return Task.CompletedTask; });
        var prompt = context.Render<UpdatePrompt>();

        prompt.WaitForElement("#update-now").Click();

        Assert.IsTrue(flushed);
        Assert.HasCount(1, module.Invocations["apply"]);
    }

    [TestMethod]
    public void FailedFlushPostponesTheUpdate()
    {
        var (context, module) = Create(isWaiting: true);
        using var _ = context;
        context.Services.GetRequiredService<SafeReload>().Register(_ => throw new StorageException("Quota exceeded"));
        var prompt = context.Render<UpdatePrompt>();

        prompt.WaitForElement("#update-now").Click();

        StringAssert.Contains(prompt.Find("#update-available .error").TextContent, "Quota exceeded");
        Assert.IsEmpty(module.Invocations["apply"]);
    }
}
