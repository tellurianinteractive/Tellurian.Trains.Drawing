using Bunit;
using Microsoft.AspNetCore.Components;
using Tellurian.Trains.Drawing.App.Layout;
using Tellurian.Trains.Drawing.App.Pages;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class InformationPageTests
{
    [TestMethod]
    public void TopMenuHasTheAppNameWithVersionStartAndCatalogue()
    {
        using var context = new AppTestContext();
        var layout = context.Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b => b.AddContent(0, "body"))));

        var name = layout.Find(".app-name");
        Assert.AreEqual("", name.GetAttribute("href"));
        Assert.AreEqual("Module Draw", name.TextContent.Trim());
        Assert.AreEqual(AppVersion.Current, layout.Find(".app-version").TextContent);
        CollectionAssert.AreEqual(new[] { "Start", "Catalogue", "Settings" }, layout.FindAll(".app-nav a").Select(a => a.TextContent).ToArray());
        Assert.AreEqual(Home.Url, layout.Find(".app-nav a").GetAttribute("href"));
    }

    [TestMethod]
    public void InformationPageLeadsToTheStartPage()
    {
        using var context = new AppTestContext();
        var page = context.Render<InformationPage>();

        Assert.AreEqual("Welcome to Module Draw", page.Find("h1").TextContent);
        Assert.IsEmpty(page.FindAll(".version"), "The version is shown in the top bar.");
        Assert.AreEqual(Home.Url, page.Find("#get-started").GetAttribute("href"));
    }

    [TestMethod]
    public void InformationHasGeneralAndBrowserSupportSections()
    {
        using var context = new AppTestContext();
        var page = context.Render<InformationPage>();

        CollectionAssert.AreEqual(new[] { "General", "Browser support" }, page.FindAll("h2").Select(h => h.TextContent).ToArray());
        var browsers = page.Find("#browser-support").TextContent;
        StringAssert.Contains(browsers, "Chromium-based");
        StringAssert.Contains(browsers, "Firefox and Safari");
    }

    [TestMethod]
    public void InformationPageIsTheRootAndTheStartPageMoved()
    {
        static string[] Routes(Type page) => [.. page.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>().Select(r => r.Template)];
        CollectionAssert.AreEquivalent(new[] { "/", "/information" }, Routes(typeof(InformationPage)));
        CollectionAssert.AreEqual(new[] { "/" + Home.Url }, Routes(typeof(Home)));
    }
}
