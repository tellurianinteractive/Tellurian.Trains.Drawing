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
        Assert.AreEqual(InformationPage.Url, name.GetAttribute("href"));
        Assert.AreEqual($"Module Draw {AppVersion.Current}", name.TextContent.Trim());
        CollectionAssert.AreEqual(new[] { "Start", "Catalogue" }, layout.FindAll(".app-nav a").Select(a => a.TextContent).ToArray());
    }

    [TestMethod]
    public void InformationPageLeadsToTheStartPage()
    {
        using var context = new AppTestContext();
        var page = context.Render<InformationPage>();

        Assert.AreEqual("Welcome to Module Draw", page.Find("h1").TextContent);
        StringAssert.Contains(page.Find(".version").TextContent, AppVersion.Current);
        Assert.AreEqual("", page.Find("#get-started").GetAttribute("href"));
    }
}
