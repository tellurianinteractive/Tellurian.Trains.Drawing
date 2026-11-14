using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.App.Tests.Fakes;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class HomeTests
{
    [TestMethod]
    public void ShowsAppNameAndVersion()
    {
        using var context = new AppTestContext();

        var home = context.Render<Home>();

        Assert.AreEqual("Tellurian Trains Drawing", home.Find("h1").TextContent);
        StringAssert.StartsWith(home.Find(".version").TextContent, "Version 0.1.0");
    }

    [TestMethod]
    public void EmptyWorkbenchSaysSo()
    {
        using var context = new AppTestContext();
        var home = context.Render<Home>();
        Assert.AreEqual("There is no work in progress.", home.Find(".drafts .empty").TextContent);
        Assert.AreEqual("No files opened yet.", home.Find(".recent .empty").TextContent);
    }

    [TestMethod]
    public void ShowsDraftsWithUnsavedChangesAndAReminder()
    {
        using var context = new AppTestContext();
        var id = Guid.NewGuid();
        context.Workbench.Drafts[id] = new() { Id = id, Kind = DrawingFileKind.Module, Name = "Station", Json = "{}", HasUnsavedChanges = true, Modified = context.Time.GetUtcNow() };

        var home = context.Render<Home>();

        var item = home.Find(".drafts li");
        Assert.AreEqual("Station", item.QuerySelector("a")!.TextContent);
        Assert.AreEqual($"drawing/{id}", item.QuerySelector("a")!.GetAttribute("href"));
        Assert.AreEqual("Module", item.QuerySelector(".kind")!.TextContent);
        Assert.AreEqual("Unsaved changes", item.QuerySelector(".unsaved")!.TextContent);
        Assert.IsNotNull(home.Find(".reminder"));
    }

    [TestMethod]
    public void OpeningOneFileGoesToIt()
    {
        using var context = new AppTestContext();
        var (id, json) = TestDrawings.Hall();
        context.Files.FilesToOpen = [new OpenedFile("club.tdhall", json)];

        var home = context.Render<Home>();
        home.Find(".toolbar button").Click();

        Assert.EndsWith($"drawing/{id}", context.Services.GetRequiredService<NavigationManager>().Uri);
        Assert.AreEqual("club.tdhall", context.Workbench.Recent.Single().FileName);
    }

    [TestMethod]
    public void FilesThatCantBeOpenedAreListed()
    {
        using var context = new AppTestContext();
        context.Files.FilesToOpen = [new OpenedFile("bad.tdmodule", "{")];

        var home = context.Render<Home>();
        home.Find(".toolbar button").Click();

        StringAssert.Contains(home.Find(".error li").TextContent, "bad.tdmodule");
    }

    [TestMethod]
    public void RecentFileWithoutReopenAsksToPickItAgain()
    {
        using var context = new AppTestContext(canSaveInPlace: false);
        context.Workbench.Recent.Add(new RecentFile { Id = Guid.NewGuid(), Kind = DrawingFileKind.Hall, Name = "Club hall", FileName = "club.tdhall", Used = context.Time.GetUtcNow() });

        var home = context.Render<Home>();
        home.Find(".recent button").Click();

        StringAssert.Contains(home.Find(".error").TextContent, "club.tdhall");
    }
}
