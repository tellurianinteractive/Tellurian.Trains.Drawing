using System.IO.Compression;
using System.Text;
using Tellurian.Trains.Drawing.App.Catalogue;
using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.App.Tests.Fakes;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Tests.Catalogue;

[TestClass]
public sealed class CatalogueServiceTests
{
    private readonly InMemoryWorkbench _workbench = new();
    private readonly FakeFileService _files = new();

    private CatalogueService Service() => new(_workbench, _files);

    private static EndProfile BuiltInProfile => BuiltInCatalogue.Instance.EndProfiles[0];

    private static EndProfile NewProfile() => BuiltInProfile.Copy("REGISTRY-1");

    [TestMethod]
    public async Task WithoutUserEntriesTheCatalogueIsTheBuiltInOne()
    {
        var catalogue = await Service().GetAsync();
        Assert.HasCount(BuiltInCatalogue.Instance.Standards.Count, catalogue.Standards);
        Assert.HasCount(BuiltInCatalogue.Instance.EndProfiles.Count, catalogue.EndProfiles);
    }

    [TestMethod]
    public async Task UserEntriesAreStoredAndReplaceBuiltInOnes()
    {
        var builtIn = BuiltInCatalogue.Instance.Standards[0];
        await Service().SaveUserAsync(DrawingCatalogue.Empty with { Standards = [builtIn with { MinimumClearance = 999 }] });

        var catalogue = await Service().GetAsync();

        Assert.AreEqual(999, catalogue.FindStandard(builtIn.Id)!.MinimumClearance);
        Assert.HasCount(BuiltInCatalogue.Instance.Standards.Count, catalogue.Standards);
    }

    [TestMethod]
    public async Task ImportedFilesAreComparedWithTheCatalogue()
    {
        _files.FilesToOpen =
        [
            new OpenedFile("new.tdendprofile", DrawingFileSerializer.Serialize(NewProfile())),
            new OpenedFile("same.tdendprofile", DrawingFileSerializer.Serialize(BuiltInProfile)),
            new OpenedFile("bad.tdendprofile", "{"),
        ];

        var preview = await Service().PickImportAsync(ImportSourceKind.Files);

        Assert.IsNotNull(preview);
        CollectionAssert.AreEqual(new[] { EndProfileImportStatus.New, EndProfileImportStatus.Unchanged }, preview.Items.Select(i => i.Status).ToArray());
        Assert.AreEqual("bad.tdendprofile", preview.Problems.Single().Name);
    }

    [TestMethod]
    public async Task EndProfilesAreImportedFromAZipArchive()
    {
        var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var writer = new StreamWriter(zip.CreateEntry("profiles/new.tdendprofile").Open(), Encoding.UTF8);
            writer.Write(DrawingFileSerializer.Serialize(NewProfile()));
        }
        _files.ArchiveToOpen = buffer.ToArray();

        var preview = await Service().PickImportAsync(ImportSourceKind.Archive);

        Assert.AreEqual("REGISTRY-1", preview?.Items.Single().Imported.Designation);
    }

    [TestMethod]
    public async Task CancelledImportReturnsNull() =>
        Assert.IsNull(await Service().PickImportAsync(ImportSourceKind.Files));

    [TestMethod]
    public async Task AppliedImportIsStoredInTheUserCatalogue()
    {
        _files.FilesToOpen = [new OpenedFile("new.tdendprofile", DrawingFileSerializer.Serialize(NewProfile()))];
        var service = Service();
        var preview = await service.PickImportAsync(ImportSourceKind.Files);

        await service.ApplyImportAsync(preview!.Items);

        var user = await Service().GetUserAsync();
        Assert.AreEqual("REGISTRY-1", user.EndProfiles.Single().Designation);
    }

    [TestMethod]
    public async Task CopyAndDeleteAUserProfile()
    {
        var service = Service();
        var copy = await service.CopyEndProfileAsync(BuiltInProfile.Id, "MY-PROFILE");
        Assert.IsNotNull((await service.GetAsync()).FindEndProfile(copy.Id));

        await service.DeleteEndProfileAsync(copy.Id);

        Assert.IsNull((await service.GetAsync()).FindEndProfile(copy.Id));
        Assert.IsNotNull((await service.GetAsync()).FindEndProfile(BuiltInProfile.Id));
    }

    [TestMethod]
    public async Task ExportSavesTheUserCatalogueAsANewFile()
    {
        var service = Service();
        await service.CopyEndProfileAsync(BuiltInProfile.Id, "MY-PROFILE");
        _files.SaveAsName = "mine.tdcatalogue";

        var saved = await service.ExportAsync();

        Assert.AreEqual("mine.tdcatalogue", saved?.Name);
        var (text, suggested, _, saveAs) = _files.Saves.Single();
        Assert.AreEqual((CatalogueService.ExportFileName, true), (suggested, saveAs));
        Assert.AreEqual("MY-PROFILE", DrawingFileSerializer.DeserializeCatalogue(text).EndProfiles.Single().Designation);
    }

    [TestMethod]
    public void CopyNamesAreUnique()
    {
        var name = string.Join(" ", BuiltInProfile.Designation, "copy");
        var catalogue = BuiltInCatalogue.Instance with { EndProfiles = [.. BuiltInCatalogue.Instance.EndProfiles, BuiltInProfile.Copy(name)] };
        Assert.AreEqual(string.Join(" ", name, "2"), CatalogueService.CopyName(BuiltInProfile.Designation, BuiltInProfile.Scale, catalogue, "copy"));
    }

    [TestMethod]
    [DataRow(true, false, CatalogueOrigin.BuiltIn)]
    [DataRow(false, true, CatalogueOrigin.User)]
    [DataRow(true, true, CatalogueOrigin.ReplacesBuiltIn)]
    public void Origins(bool isBuiltIn, bool isUser, CatalogueOrigin expected) =>
        Assert.AreEqual(expected, CatalogueService.Origin(isBuiltIn, isUser));
}
