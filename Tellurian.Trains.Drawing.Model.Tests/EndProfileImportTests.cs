using Tellurian.Trains.Drawing.Model.Catalogues;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class EndProfileImportTests
{
    private static Catalogue Catalogue => TestCatalogue.Instance;

    private static EndProfileImportItem CompareOne(EndProfile profile) => EndProfileImport.Compare([profile], Catalogue).Single();

    /// <summary>An end profile as the Module Registry gives one it has no drawing of: designation, scale, standards and its id.</summary>
    private static EndProfile Shapeless(EndProfile like) => new()
    {
        Id = Guid.NewGuid(),
        Designation = like.Designation,
        Scale = like.Scale,
        Standards = [.. like.Standards],
        ExternalReferences = Schedules.Model.Layouts.ExternalReferences.WithModuleRegistryId(null, Schedules.Model.Layouts.ExternalReferences.EndProfileId, 42),
    };

    private static EndProfile OfFremo(EndProfile profile) => profile.With(p => p.Standards = [new(TestCatalogue.FremoId, "FREMO/H0-RE")]);

    [TestMethod]
    public void AProfileWithoutAnOutlineHasNoOutline()
    {
        Assert.IsFalse(Shapeless(TestCatalogue.FremoSingle).HasOutline());
        Assert.IsTrue(TestCatalogue.FremoSingle.HasOutline());
    }

    [TestMethod]
    public void AProfileWithoutAShapeWithTheSameIdKeepsTheDrawingAndTakesTheRegistryReference()
    {
        var existing = TestCatalogue.FremoSingle;
        var item = CompareOne(Shapeless(existing).With(p => p.Id = existing.Id));

        Assert.AreEqual(EndProfileImportStatus.Changed, item.Status);
        Assert.AreEqual(EndProfileChanges.Source, item.Changes);
        var applied = EndProfileImport.Apply(Catalogue, [item]).FindEndProfile(existing.Id)!;
        Assert.IsTrue(applied.HasOutline());
        Assert.HasCount(existing.Tracks.Count, applied.Tracks);
        Assert.IsTrue(applied.ExternalReferences!.ContainsKey(Schedules.Model.Layouts.ExternalReferences.ModuleRegistry));
    }

    [TestMethod]
    public void ADrawingWithTheSameDesignationStandardAndScaleIsMergedWithTheRegistrysProfile()
    {
        var existing = TestCatalogue.FremoSingle;
        var registry = Shapeless(existing);
        var item = CompareOne(registry);

        Assert.AreEqual(EndProfileImportStatus.Merged, item.Status);
        Assert.AreSame(existing, item.Existing);
        Assert.AreEqual(registry.Id, item.Imported.Id);
        Assert.IsTrue(item.Imported.HasOutline());
        Assert.AreEqual(registry.Id, EndProfileImport.Merges([item])[existing.Id]);
    }

    [TestMethod]
    public void AMergedProfileTakesTheRegistrysIdInTheWholeCatalogue()
    {
        var existing = TestCatalogue.FremoSingle;
        var registry = Shapeless(existing);
        var catalogue = Catalogue with { Overrides = [new() { FirstEndProfileId = existing.Id, SecondEndProfileId = TestCatalogue.Mmm480.Id, Fit = JointFit.FitsWithClamps }] };

        var applied = EndProfileImport.Apply(catalogue, EndProfileImport.Compare([registry], catalogue));

        Assert.IsNull(applied.FindEndProfile(existing.Id));
        var merged = applied.FindEndProfile(registry.Id)!;
        Assert.IsTrue(merged.HasOutline());
        Assert.HasCount(1, applied.EndProfiles.Where(p => p.Designation == existing.Designation));
        Assert.AreEqual(registry.Id, applied.FindStandard(TestCatalogue.FremoId)!.DefaultSingleTrackEndProfileId);
        Assert.AreEqual(registry.Id, applied.Overrides.Single().FirstEndProfileId);
        Assert.IsTrue(merged.ExternalReferences!.ContainsKey(Schedules.Model.Layouts.ExternalReferences.ModuleRegistry));
    }

    [TestMethod]
    public void AProfileOfAnotherStandardIsNotMerged()
    {
        var existing = OfFremo(TestCatalogue.FremoSingle);
        var other = Shapeless(existing).With(p => p.Standards = [new(Guid.NewGuid(), "SOMETHING-ELSE")]);

        var item = EndProfileImport.Compare([other], Catalogue.With(existing)).Single();

        Assert.AreEqual(EndProfileImportStatus.New, item.Status);
        Assert.IsNull(item.Existing);
    }

    [TestMethod]
    public void TheStandardIsMatchedByNameWhenTheIdsDiffer()
    {
        var existing = OfFremo(TestCatalogue.FremoSingle);
        var registry = Shapeless(existing).With(p => p.Standards = [new(Guid.NewGuid(), "fremo/h0-re")]);

        Assert.AreEqual(EndProfileImportStatus.Merged, EndProfileImport.Compare([registry], Catalogue.With(existing)).Single().Status);
    }

    [TestMethod]
    public void AModuleDrawnBeforeTheMergeIsMovedToTheMergedProfile()
    {
        var existing = TestCatalogue.FremoSingle;
        var registry = Shapeless(existing);
        var catalogue = EndProfileImport.Apply(Catalogue, EndProfileImport.Compare([registry], Catalogue));
        var module = ModuleFileContent.Create(TestModules.Straight(), TestCatalogue.Lookup);
        Assert.IsTrue(module.Module.Drawing.Exits.Any(e => e.EndProfile.Id == existing.Id));

        var (moved, profiles) = module.WithMergedEndProfiles(new Dictionary<Guid, Guid> { [existing.Id] = registry.Id }, catalogue.FindEndProfile);

        Assert.AreEqual(registry.Id, profiles.Single().Id);
        Assert.IsFalse(moved.Module.Drawing.Exits.Any(e => e.EndProfile.Id == existing.Id));
        Assert.IsNotNull(moved.FindEndProfile(registry.Id));
        Assert.IsNull(moved.FindEndProfile(existing.Id));
        Assert.IsTrue(module.Module.Drawing.Exits.Any(e => e.EndProfile.Id == existing.Id), "The original is unchanged.");
    }

    [TestMethod]
    public void AModuleWithoutMergedProfilesIsUnchanged()
    {
        var module = ModuleFileContent.Create(TestModules.Straight(), TestCatalogue.Lookup);

        var (same, profiles) = module.WithMergedEndProfiles(new Dictionary<Guid, Guid> { [Guid.NewGuid()] = Guid.NewGuid() }, TestCatalogue.Lookup);

        Assert.AreSame(module, same);
        Assert.IsEmpty(profiles);
    }

    [TestMethod]
    public void ANewProfileWithoutAShapeIsImportedAsItIs()
    {
        var item = CompareOne(Shapeless(TestCatalogue.FremoSingle).With(p => p.Designation = "REGISTRY-ONLY"));

        Assert.AreEqual(EndProfileImportStatus.New, item.Status);
        Assert.IsFalse(EndProfileImport.Apply(Catalogue, [item]).FindEndProfile(item.Imported.Id)!.HasOutline());
    }

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

        var user = EndProfileImport.Apply(Catalogue.Empty, [item]);

        var stored = user.EndProfiles.Single();
        Assert.AreEqual(TestCatalogue.FremoSingle.Id, stored.Id);
        Assert.IsTrue(Files.ExternalReferences.Same(imported.ExternalReferences, stored.ExternalReferences));
        Assert.IsTrue(Files.ExternalReferences.Same(imported.ExternalReferences, Catalogue.Merge(Catalogue, user).FindEndProfile(TestCatalogue.FremoSingle.Id)!.ExternalReferences));
    }

    [TestMethod]
    public void UnchangedProfilesAreNotAdded()
    {
        var user = EndProfileImport.Apply(Catalogue.Empty, [CompareOne(TestCatalogue.FremoSingle)]);
        Assert.IsEmpty(user.EndProfiles);
    }

    [TestMethod]
    public void NewProfileIsAddedAndReplacesAnEarlierUserCopy()
    {
        var profile = TestProfiles.FremoSingle().With(edited => { edited.Designation = "NEW-1"; });
        var user = EndProfileImport.Apply(Catalogue.Empty, [CompareOne(profile)]);
        var again = EndProfileImport.Apply(user, [CompareOne(profile.With(edited => { edited.Revision = 2; }))]);
        Assert.AreEqual(2, again.EndProfiles.Single().Revision);
    }

    [TestMethod]
    public void ProfilesAreReadFromEndProfileCatalogueAndModuleFiles()
    {
        var files = DrawingImport.FromFiles(
        [
            ImportSource.FromText("p.tdendprofile", DrawingFileSerializer.Serialize(TestCatalogue.Mmm480)),
            ImportSource.FromText("c.tdcatalogue", DrawingFileSerializer.Serialize(Catalogue.Empty with { EndProfiles = [TestCatalogue.FremoDouble] })),
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
