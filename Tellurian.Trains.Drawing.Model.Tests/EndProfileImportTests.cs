using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class EndProfileImportTests
{
    private static DrawingCatalogue Catalogue => TestCatalogue.Instance;

    private static EndProfileImportItem CompareOne(EndProfile profile) => EndProfileImport.Compare([profile], Catalogue).Single();

    [TestMethod]
    public void UnknownProfileIsNew()
    {
        var item = CompareOne(TestProfiles.FremoSingle().With(edited => { edited.Designation = "NEW-1"; }));
        Assert.AreEqual(EndProfileImportStatus.New, item.Status);
        Assert.IsNull(item.Existing);
    }

    [TestMethod]
    public void SameProfileIsUnchanged() =>
        Assert.AreEqual(EndProfileImportStatus.Unchanged, CompareOne(TestCatalogue.FremoSingle).Status);

    [TestMethod]
    public void SameProfileReadFromAFileIsUnchanged()
    {
        var read = DrawingFileSerializer.DeserializeEndProfile(DrawingFileSerializer.Serialize(TestCatalogue.FremoDouble));
        Assert.AreEqual(EndProfileImportStatus.Unchanged, CompareOne(read).Status);
    }

    [TestMethod]
    public void ChangedDrawingIsListed()
    {
        var existing = TestCatalogue.FremoSingle;
        var changed = existing.With(edited => { edited.ScrewHoles = [.. existing.ScrewHoles.Skip(1)]; edited.Tracks = [existing.Tracks[0].With(edited => { edited.Offset = 5; })]; edited.Source = new() { Title = "Registry" }; });
        var item = CompareOne(changed);
        Assert.AreEqual(EndProfileImportStatus.Changed, item.Status);
        Assert.AreEqual(EndProfileChanges.ScrewHoles | EndProfileChanges.Tracks | EndProfileChanges.Source, item.Changes);
    }

    [TestMethod]
    public void ProfileIsMatchedByDesignationWhenTheIdDiffers()
    {
        var item = CompareOne(TestCatalogue.FremoSingle.With(edited => { edited.Id = Guid.NewGuid(); edited.Designation = "fremo-h0-b96 "; }));
        Assert.AreSame(TestCatalogue.FremoSingle, item.Existing);
        Assert.AreEqual(EndProfileImportStatus.Unchanged, item.Status);
    }

    [TestMethod]
    public void ReplacingKeepsTheExistingId()
    {
        var imported = TestCatalogue.FremoSingle.With(edited => { edited.Id = Guid.NewGuid(); edited.ExternalReferences = TestReferences.Registry("""{ "endProfileId": 42 }"""); });
        var item = CompareOne(imported);

        var user = EndProfileImport.Apply(DrawingCatalogue.Empty, [item]);

        var stored = user.EndProfiles.Single();
        Assert.AreEqual(TestCatalogue.FremoSingle.Id, stored.Id);
        Assert.IsTrue(Files.ExternalReferences.Same(imported.ExternalReferences, stored.ExternalReferences));
        Assert.IsTrue(Files.ExternalReferences.Same(imported.ExternalReferences, DrawingCatalogue.Merge(Catalogue, user).FindEndProfile(TestCatalogue.FremoSingle.Id)!.ExternalReferences));
    }

    [TestMethod]
    public void UnchangedProfilesAreNotAdded()
    {
        var user = EndProfileImport.Apply(DrawingCatalogue.Empty, [CompareOne(TestCatalogue.FremoSingle)]);
        Assert.IsEmpty(user.EndProfiles);
    }

    [TestMethod]
    public void NewProfileIsAddedAndReplacesAnEarlierUserCopy()
    {
        var profile = TestProfiles.FremoSingle().With(edited => { edited.Designation = "NEW-1"; });
        var user = EndProfileImport.Apply(DrawingCatalogue.Empty, [CompareOne(profile)]);
        var again = EndProfileImport.Apply(user, [CompareOne(profile.With(edited => { edited.Revision = 2; }))]);
        Assert.AreEqual(2, again.EndProfiles.Single().Revision);
    }

    [TestMethod]
    public void ProfilesAreReadFromEndProfileCatalogueAndModuleFiles()
    {
        var files = DrawingImport.FromFiles(
        [
            ImportSource.FromText("p.tdendprofile", DrawingFileSerializer.Serialize(TestCatalogue.Mmm480)),
            ImportSource.FromText("c.tdcatalogue", DrawingFileSerializer.Serialize(DrawingCatalogue.Empty with { EndProfiles = [TestCatalogue.FremoDouble] })),
            ImportSource.FromText("m.tdmodule", DrawingFileSerializer.Serialize(ModuleFileContent.Create(TestModules.Straight(), TestCatalogue.Lookup))),
        ], EndProfileImport.Kinds);

        var (profiles, problems) = EndProfileImport.Read(files.Files);

        Assert.IsEmpty(problems);
        CollectionAssert.AreEquivalent(new[] { "MMM480", "FREMO-H0-2E96", "FREMO-H0-B96" }, profiles.Select(p => p.Designation).ToArray());
    }

    [TestMethod]
    public void CopyIsANewUserProfile()
    {
        var original = TestCatalogue.FremoSingle.With(edited => { edited.ExternalReferences = TestReferences.Registry("""{ "endProfileId": 7 }"""); edited.Revision = 3; edited.Source = new() { Title = "FREMO" }; });
        var copy = original.Copy("MY-B96");
        Assert.AreNotEqual(original.Id, copy.Id);
        Assert.AreEqual(("MY-B96", 0, "FREMO"), (copy.Designation, copy.Revision, copy.Source?.Title));
        Assert.IsNull(copy.ExternalReferences, "A copy must not point to what the original is registered as.");
        Assert.AreEqual(EndProfileChanges.Designation | EndProfileChanges.Source, EndProfileImport.Changes(original, copy));
    }
}
