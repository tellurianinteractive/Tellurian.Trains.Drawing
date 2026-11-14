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

        Assert.AreEqual("Module Draw", home.Find("h1").TextContent);
        // The version from Directory.Build.props, so a release doesn't have to change the test.
        var version = typeof(Home).Assembly.GetName().Version!;
        StringAssert.StartsWith(home.Find(".version").TextContent, $"Version {version.Major}.{version.Minor}.{version.Build}");
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

    [TestMethod]
    public void DraftRowsHaveSaveOnlyWithUnsavedChangesEditAndDiscard()
    {
        using var context = new AppTestContext();
        Guid unsaved = Guid.NewGuid(), saved = Guid.NewGuid();
        context.Workbench.Drafts[unsaved] = new() { Id = unsaved, Kind = DrawingFileKind.Module, Name = "A", Json = "{}", HasUnsavedChanges = true, Modified = context.Time.GetUtcNow() };
        context.Workbench.Drafts[saved] = new() { Id = saved, Kind = DrawingFileKind.Module, Name = "B", Json = "{}", HasUnsavedChanges = false, Modified = context.Time.GetUtcNow() };

        var home = context.Render<Home>();

        var rows = home.FindAll(".drafts li").ToDictionary(r => r.QuerySelector("a")!.TextContent);
        Assert.IsNotNull(rows["A"].QuerySelector("button.save.primary"));
        Assert.IsNull(rows["B"].QuerySelector("button.save"), "Nothing to save.");
        foreach (var row in rows.Values)
        {
            Assert.AreEqual("Edit", row.QuerySelector("a.edit.primary")!.TextContent);
            Assert.IsNotNull(row.QuerySelector("button.discard.danger"));
        }
        Assert.AreEqual($"drawing/{saved}", rows["B"].QuerySelector("a.edit")!.GetAttribute("href"));
    }

    [TestMethod]
    public void SaveInTheListSavesTheDraftToAFile()
    {
        using var context = new AppTestContext();
        var (id, json) = TestDrawings.Hall();
        context.Workbench.Drafts[id] = new() { Id = id, Kind = DrawingFileKind.Hall, Name = "Club hall", Json = json, HasUnsavedChanges = true, Modified = context.Time.GetUtcNow() };

        var home = context.Render<Home>();
        home.Find(".drafts button.save").Click();

        Assert.HasCount(1, context.Files.Saves);
        Assert.IsFalse(context.Workbench.Drafts[id].HasUnsavedChanges);
        Assert.IsEmpty(home.FindAll(".drafts button.save"), "Saved: no Save button any more.");
    }

    [TestMethod]
    public void DiscardAsksFirstWhenThereAreUnsavedChanges()
    {
        using var context = new AppTestContext();
        var id = Guid.NewGuid();
        context.Workbench.Drafts[id] = new() { Id = id, Kind = DrawingFileKind.Module, Name = "Station", Json = "{}", HasUnsavedChanges = true, Modified = context.Time.GetUtcNow() };
        var confirm = context.JSInterop.Setup<bool>("confirm", _ => true);
        confirm.SetResult(false);

        var home = context.Render<Home>();
        home.Find(".drafts button.discard").Click();
        Assert.HasCount(1, context.Workbench.Drafts, "Declined: the draft is kept.");

        confirm.SetResult(true);
        home.Find(".drafts button.discard").Click();
        Assert.IsEmpty(context.Workbench.Drafts);
        Assert.IsNotNull(home.Find(".drafts .empty"));
    }

    [TestMethod]
    public void InformationPageComesFirstWhenTheAppStarts()
    {
        using var context = new AppTestContext();
        context.JSInterop.Setup<string?>("sessionStorage.getItem", _ => true).SetResult(null);
        var navigation = context.Services.GetRequiredService<NavigationManager>();

        context.Render<Home>();
        Assert.EndsWith("information", navigation.Uri);

        navigation.NavigateTo("");
        context.Render<Home>();
        Assert.EndsWith("/", navigation.Uri, "Only once.");
    }
}
