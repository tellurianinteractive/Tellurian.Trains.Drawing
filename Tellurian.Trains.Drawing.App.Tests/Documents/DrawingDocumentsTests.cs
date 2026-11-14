using Microsoft.Extensions.Time.Testing;
using Tellurian.Trains.Drawing.App.Documents;
using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.App.Tests.Fakes;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Tests.Documents;

[TestClass]
public sealed class DrawingDocumentsTests
{
    private readonly InMemoryWorkbench _workbench = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero));

    private DrawingDocuments Documents(FakeFileService files) => new(files, _workbench, _time);

    [TestMethod]
    public async Task OpenedFileBecomesASavedDraftAndARecentFile()
    {
        var (id, json) = TestDrawings.Hall(revision: 3);
        var files = new FakeFileService { FilesToOpen = [new("club.tdhall", json, "h1")] };

        var result = await Documents(files).OpenAsync();

        var draft = result.Drafts.Single();
        Assert.AreEqual(id, draft.Id);
        Assert.AreEqual(DrawingFileKind.Hall, draft.Kind);
        Assert.AreEqual("Club hall", draft.Name);
        Assert.AreEqual("club.tdhall", draft.FileName);
        Assert.AreEqual("h1", draft.HandleId);
        Assert.AreEqual(3, draft.Revision);
        Assert.IsFalse(draft.HasUnsavedChanges);
        Assert.AreEqual(json, _workbench.Drafts[id].Json);
        Assert.AreEqual("club.tdhall", _workbench.Recent.Single().FileName);
    }

    [TestMethod]
    public async Task CancelledOpenDoesNothing()
    {
        var result = await Documents(new FakeFileService { FilesToOpen = null }).OpenAsync();
        Assert.AreSame(OpenResult.Cancelled, result);
        Assert.IsEmpty(_workbench.Drafts);
    }

    [TestMethod]
    public async Task FilesThatCantBeReadAreReported()
    {
        var files = new FakeFileService { FilesToOpen = [new("bad.tdhall", "{ nope"), new("good.tdhall", TestDrawings.Hall().Json)] };
        var result = await Documents(files).OpenAsync();
        Assert.HasCount(1, result.Drafts);
        Assert.AreEqual("bad.tdhall", result.Problems.Single().Name);
    }

    [TestMethod]
    public async Task OpeningAFileKeepsADraftWithUnsavedChanges()
    {
        var (_, json) = TestDrawings.Hall();
        var documents = Documents(new FakeFileService { FilesToOpen = [new("club.tdhall", json)] });
        var draft = (await documents.OpenAsync()).Drafts.Single();
        var changed = await documents.UpdateAsync(draft, DrawingFileSerializer.Serialize(TestDrawings.ReadHall(json) with { Name = "Renamed" }));

        var reopened = (await documents.OpenAsync()).Drafts.Single();

        Assert.IsTrue(reopened.HasUnsavedChanges);
        Assert.AreEqual(changed.Json, reopened.Json);
    }

    [TestMethod]
    public async Task UpdateMarksUnsavedChangesAndTakesTheNewName()
    {
        var (_, json) = TestDrawings.Hall();
        var documents = Documents(new FakeFileService { FilesToOpen = [new("club.tdhall", json)] });
        var draft = (await documents.OpenAsync()).Drafts.Single();
        var hall = TestDrawings.ReadHall(json) with { Name = "Big hall" };
        _time.Advance(TimeSpan.FromMinutes(5));

        var updated = await documents.UpdateAsync(draft, DrawingFileSerializer.Serialize(hall));

        Assert.IsTrue(updated.HasUnsavedChanges);
        Assert.AreEqual("Big hall", updated.Name);
        Assert.AreEqual(_time.GetUtcNow(), updated.Modified);
    }

    [TestMethod]
    public async Task SaveInPlaceClearsUnsavedChanges()
    {
        var (_, json) = TestDrawings.Hall();
        var files = new FakeFileService { FilesToOpen = [new("club.tdhall", json, "h1")] };
        var documents = Documents(files);
        var draft = (await documents.OpenAsync()).Drafts.Single();
        draft = await documents.UpdateAsync(draft, json);

        var saved = await documents.SaveAsync(draft.Id, saveAs: false);

        Assert.IsNotNull(saved);
        Assert.IsFalse(saved.HasUnsavedChanges);
        Assert.AreEqual(("club.tdhall", "h1", false), (files.Saves[0].SuggestedName, files.Saves[0].HandleId, files.Saves[0].SaveAs));
        Assert.AreEqual(1, _workbench.PersistenceRequests);
    }

    [TestMethod]
    public async Task SaveAsGetsANewFile()
    {
        var (_, json) = TestDrawings.Hall();
        var files = new FakeFileService { FilesToOpen = [new("club.tdhall", json, "h1")], SaveAsName = "copy.tdhall" };
        var documents = Documents(files);
        var draft = (await documents.OpenAsync()).Drafts.Single();

        var saved = await documents.SaveAsync(draft.Id, saveAs: true);

        Assert.AreEqual("copy.tdhall", saved?.FileName);
        Assert.AreEqual("new-handle", saved?.HandleId);
        Assert.AreEqual("copy.tdhall", _workbench.Recent.Single().FileName);
    }

    [TestMethod]
    public async Task CancelledSaveKeepsUnsavedChanges()
    {
        var (_, json) = TestDrawings.Hall();
        var files = new FakeFileService { FilesToOpen = [new("club.tdhall", json)], SaveAsName = null };
        var documents = Documents(files);
        var draft = await documents.UpdateAsync((await documents.OpenAsync()).Drafts.Single(), json);

        Assert.IsNull(await documents.SaveAsync(draft.Id, saveAs: false));
        Assert.IsTrue(_workbench.Drafts[draft.Id].HasUnsavedChanges);
    }

    [TestMethod]
    public async Task DownloadSuggestsThePreviousFileName()
    {
        var (_, json) = TestDrawings.Hall();
        var files = new FakeFileService(canSaveInPlace: false) { FilesToOpen = [new("club.tdhall", json)] };
        var documents = Documents(files);
        var draft = (await documents.OpenAsync()).Drafts.Single();

        var saved = await documents.SaveAsync(draft.Id, saveAs: false);

        Assert.AreEqual("club.tdhall", files.Saves[0].SuggestedName);
        Assert.IsNull(saved?.HandleId);
    }

    [TestMethod]
    public async Task NewDrawingIsAnUnsavedDraftWithoutAFile()
    {
        var (id, json) = TestDrawings.Hall("New hall");
        var draft = await Documents(new FakeFileService()).CreateAsync(json);
        Assert.AreEqual((id, "New hall", true, (string?)null), (draft.Id, draft.Name, draft.HasUnsavedChanges, draft.FileName));
        Assert.AreSame(draft, _workbench.Drafts[id]);
        Assert.IsEmpty(_workbench.Recent);
    }

    [TestMethod]
    public async Task RecentFileWithHandleIsReopened()
    {
        var (id, json) = TestDrawings.Hall();
        var files = new FakeFileService();
        files.Reopenable["h1"] = new("club.tdhall", json, "h1");
        var recent = new RecentFile { Id = id, Kind = DrawingFileKind.Hall, Name = "Club hall", FileName = "club.tdhall", HandleId = "h1", Used = _time.GetUtcNow() };

        var result = await Documents(files).ReopenAsync(recent);

        Assert.AreEqual(id, result?.Drafts.Single().Id);
        Assert.IsNull(await Documents(files).ReopenAsync(recent with { HandleId = "gone" }));
        Assert.IsNull(await Documents(files).ReopenAsync(recent with { HandleId = null }));
    }

    [TestMethod]
    [DataRow("Club hall", null, "Club hall.tdhall")]
    [DataRow("A/B: C", null, "A-B- C.tdhall")]
    [DataRow("a<b>c\"d\\e|f?g*h", null, "a-b-c-d-e-f-g-h.tdhall")]
    [DataRow("  ", null, "Hall.tdhall")]
    [DataRow("Club hall", "old.tdhall", "old.tdhall")]
    public void SuggestedFileNames(string name, string? fileName, string expected)
    {
        var draft = new Draft { Id = Guid.NewGuid(), Kind = DrawingFileKind.Hall, Name = name, Json = "{}", FileName = fileName, Modified = default };
        Assert.AreEqual(expected, DrawingDocuments.SuggestedFileName(draft));
    }
}
