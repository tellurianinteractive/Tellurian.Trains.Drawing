using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class DrawingRevisionLogTests
{
    private static readonly DateOnly Monday = new(2026, 11, 16);
    private static readonly DateOnly Friday = new(2026, 11, 20);

    [TestMethod]
    public void EachPersonHasOneEntryWithTheLastDateNewestFirst()
    {
        IReadOnlyList<DrawingRevision> log = [];
        log = DrawingRevisionLog.Record(log, "Anna", Monday);
        log = DrawingRevisionLog.Record(log, "Bo", Monday.AddDays(1));
        log = DrawingRevisionLog.Record(log, " anna ", Friday);

        CollectionAssert.AreEqual(new[]
        {
            new DrawingRevision { Name = "anna", Date = Friday },
            new DrawingRevision { Name = "Bo", Date = Monday.AddDays(1) },
        }, log.ToArray());
    }

    [TestMethod]
    public void WithoutANameNothingIsRecorded()
    {
        IReadOnlyList<DrawingRevision> log = [new() { Name = "Anna", Date = Monday }];
        Assert.AreSame(log, DrawingRevisionLog.Record(log, "  ", Friday));
    }

    [TestMethod]
    public void SavingAnEndProfileFileRecordsTheSave()
    {
        var json = DrawingFileSerializer.Serialize(TestCatalogue.FremoSingle);

        var recorded = DrawingRevisionLog.Record(DrawingRevisionLog.Record(json, "Anna", Monday), "Bo", Friday);

        var profile = DrawingFileSerializer.DeserializeEndProfile(recorded);
        CollectionAssert.AreEqual(new[] { "Bo", "Anna" }, profile.RevisionLog.Select(r => r.Name).ToArray());
        Assert.AreEqual(Friday, profile.RevisionLog[0].Date);
        Assert.AreEqual(TestCatalogue.FremoSingle.Designation, profile.Designation);
    }

    [TestMethod]
    public void SavingAModuleFileRecordsTheSaveInTheModuleNotInItsEndProfiles()
    {
        var module = TestModules.Straight();
        var json = DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, TestCatalogue.Lookup));

        var content = DrawingFileSerializer.DeserializeModule(DrawingRevisionLog.Record(json, "Anna", Monday));

        Assert.AreEqual("Anna", Assert.ContainsSingle(content.Module.RevisionLog).Name);
        Assert.IsTrue(content.EndProfiles.All(p => p.RevisionLog.Count == 0));
    }

    [TestMethod]
    public void AChangedDrawingKeepsTheRecordedLog()
    {
        var saved = DrawingRevisionLog.Record(DrawingFileSerializer.Serialize(TestCatalogue.FremoSingle), "Anna", Monday);
        var edited = DrawingFileSerializer.Serialize(TestCatalogue.FremoSingle.With(edited => { edited.Designation = "Edited"; }));

        var kept = DrawingRevisionLog.KeepLog(edited, saved);

        var profile = DrawingFileSerializer.DeserializeEndProfile(kept);
        Assert.AreEqual("Edited", profile.Designation);
        Assert.AreEqual("Anna", Assert.ContainsSingle(DrawingRevisionLog.Read(kept)).Name);
        Assert.AreEqual("Anna", Assert.ContainsSingle(profile.RevisionLog).Name);
    }

    [TestMethod]
    public void CatalogueFilesHaveNoLog()
    {
        var json = DrawingFileSerializer.Serialize(TestCatalogue.Instance);
        Assert.AreEqual(json, DrawingRevisionLog.Record(json, "Anna", Monday));
    }
}
