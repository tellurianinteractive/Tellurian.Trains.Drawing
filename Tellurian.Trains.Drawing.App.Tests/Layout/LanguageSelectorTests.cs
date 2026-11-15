using Bunit;
using Tellurian.Trains.Drawing.App.Documents;
using Tellurian.Trains.Drawing.App.Layout;

namespace Tellurian.Trains.Drawing.App.Tests.Layout;

[TestClass]
public sealed class LanguageSelectorTests
{
    [TestMethod]
    public void EachLanguageHasAFlagAndTheCurrentOneIsMarked()
    {
        using var context = new AppTestContext();
        var selector = context.Render<LanguageSelector>();

        var buttons = selector.FindAll(".language-selector button");
        CollectionAssert.AreEqual(new[] { "English", "Svenska", "Deutsch", "Dansk", "Norsk Bokmål" }, buttons.Select(b => b.GetAttribute("title")).ToArray());
        CollectionAssert.AreEqual(
            new[] { "images/flags/gb.png", "images/flags/se.png", "images/flags/de.png", "images/flags/dk.png", "images/flags/no.png" },
            buttons.Select(b => b.QuerySelector("img")!.GetAttribute("src")).ToArray());
        var active = Assert.ContainsSingle(selector.FindAll(".language-selector button.active"));
        Assert.AreEqual("English", active.GetAttribute("title"));
        Assert.AreEqual("true", active.GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void ChoosingAnotherLanguageStoresItAndReloads()
    {
        using var context = new AppTestContext();
        var selector = context.Render<LanguageSelector>();

        selector.Find("button[title='Svenska']").Click();

        var stored = context.JSInterop.Invocations.Single(i => i.Identifier == "localStorage.setItem");
        CollectionAssert.AreEqual(new object?[] { SafeReload.CultureStorageKey, "sv-SE" }, stored.Arguments.ToArray());
        Assert.ContainsSingle(context.JSInterop.Invocations.Where(i => i.Identifier == "location.reload"));
    }

    [TestMethod]
    public void ChoosingTheCurrentLanguageDoesNothing()
    {
        using var context = new AppTestContext();
        var selector = context.Render<LanguageSelector>();

        selector.Find("button[title='English']").Click();

        Assert.IsFalse(context.JSInterop.Invocations.Any(i => i.Identifier == "location.reload"));
    }
}
