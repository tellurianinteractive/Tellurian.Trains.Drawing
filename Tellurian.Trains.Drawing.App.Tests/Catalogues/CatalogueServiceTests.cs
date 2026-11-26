using System.IO.Compression;
using System.Text;
using Tellurian.Trains.Drawing.App.Catalogues;
using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.App.Tests.Fakes;
using Tellurian.Trains.Drawing.Model.Catalogues;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Tests.Catalogues;

[TestClass]
public sealed class CatalogueServiceTests
{
    private readonly InMemoryWorkbench _workbench = new();
    private readonly FakeFileService _files = new();

    private readonly FakeRegistryHandler _registry = new();

    private CatalogueService Service() => new(_workbench, _files, new global::Tellurian.Trains.Drawing.App.Settings.SettingsService(_workbench), new ModuleRegistryClient(new HttpClient(_registry)));

    private static EndProfile SampleProfile => SampleCatalogue.Instance.EndProfiles[0];

    private static EndProfile NewProfile() => SampleProfile.Copy("REGISTRY-1");

    /// <summary>The user's catalogue is the sample catalogue, as if imported.</summary>
    private void WithSampleCatalogue() => _workbench.Values[CatalogueService.UserCatalogueKey] = SampleCatalogue.Json;

    [TestMethod]
    public async Task ANewUsersCatalogueIsEmpty()
    {
        var catalogue = await Service().GetAsync();

        Assert.IsEmpty(catalogue.Standards);
        Assert.IsEmpty(catalogue.EndProfiles);
    }

    [TestMethod]
    public async Task TheCatalogueIsTheUsersOwn()
    {
        var standard = SampleCatalogue.Instance.Standards[0];
        await Service().SaveUserAsync(Catalogue.Empty with { Standards = [standard.With(edited => { edited.MinimumClearance = 999; })] });

        var catalogue = await Service().GetAsync();

        Assert.AreEqual(999, catalogue.FindStandard(standard.Id!.Value)!.MinimumClearance);
        Assert.HasCount(1, catalogue.Standards);
    }

    [TestMethod]
    public async Task ACatalogueFileBringsItsStandardsAddOnsAndOverrides()
    {
        _files.FilesToOpen = [new OpenedFile("sample.catalogue.json", SampleCatalogue.Json)];
        var service = Service();

        var preview = await service.PickImportAsync(ImportSourceKind.Files);
        Assert.IsNotNull(preview);
        Assert.IsTrue(preview.HasEntries);
        Assert.HasCount(SampleCatalogue.Instance.Standards.Count, preview.Entries.Standards);
        await service.ApplyImportAsync(preview.Items, preview.Entries);

        var user = await Service().GetUserAsync();
        Assert.HasCount(SampleCatalogue.Instance.Standards.Count, user.Standards);
        Assert.HasCount(SampleCatalogue.Instance.AddOns.Count, user.AddOns);
        Assert.HasCount(SampleCatalogue.Instance.Overrides.Count, user.Overrides);
        Assert.HasCount(SampleCatalogue.Instance.EndProfiles.Count, user.EndProfiles);
    }

    [TestMethod]
    public async Task AnImportedStandardReplacesTheOneWithItsId()
    {
        WithSampleCatalogue();
        var standard = SampleCatalogue.Instance.Standards[0];
        var changed = SampleCatalogue.Instance with { Standards = [standard.With(edited => { edited.MinimumClearance = 999; })], EndProfiles = [], AddOns = [], Overrides = [] };
        _files.FilesToOpen = [new OpenedFile("changed.catalogue.json", DrawingFileSerializer.Serialize(changed))];
        var service = Service();

        var preview = await service.PickImportAsync(ImportSourceKind.Files);
        await service.ApplyImportAsync([], preview!.Entries);

        var user = await Service().GetUserAsync();
        Assert.AreEqual(999, user.FindStandard(standard.Id!.Value)!.MinimumClearance);
        Assert.HasCount(SampleCatalogue.Instance.Standards.Count, user.Standards);
    }

    [TestMethod]
    public async Task EndProfilesAreAddedToTheCatalogue()
    {
        WithSampleCatalogue();
        var service = Service();

        await service.AddEndProfilesAsync([NewProfile()]);

        Assert.IsNotNull((await Service().GetUserAsync()).FindEndProfile("REGISTRY-1", SampleProfile.Scale));
    }

    [TestMethod]
    public async Task ImportedFilesAreComparedWithTheCatalogue()
    {
        WithSampleCatalogue();
        _files.FilesToOpen =
        [
            new OpenedFile("new.tdendprofile", DrawingFileSerializer.Serialize(NewProfile())),
            new OpenedFile("same.tdendprofile", DrawingFileSerializer.Serialize(SampleProfile)),
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
        WithSampleCatalogue();
        var service = Service();
        var copy = await service.CopyEndProfileAsync(SampleProfile.Id, "MY-PROFILE");
        Assert.IsNotNull((await service.GetAsync()).FindEndProfile(copy.Id));

        await service.DeleteEndProfileAsync(copy.Id);

        Assert.IsNull((await service.GetAsync()).FindEndProfile(copy.Id));
        Assert.IsNotNull((await service.GetAsync()).FindEndProfile(SampleProfile.Id));
    }

    [TestMethod]
    public async Task ExportSavesTheUserCatalogueAsANewFile()
    {
        WithSampleCatalogue();
        var service = Service();
        await service.CopyEndProfileAsync(SampleProfile.Id, "MY-PROFILE");
        _files.SaveAsName = "mine.tdcatalogue";

        var saved = await service.ExportAsync();

        Assert.AreEqual("mine.tdcatalogue", saved?.Name);
        var (text, suggested, _, saveAs) = _files.Saves.Single();
        Assert.AreEqual((CatalogueService.ExportFileName, true), (suggested, saveAs));
        Assert.IsTrue(DrawingFileSerializer.DeserializeCatalogue(text).EndProfiles.Any(p => p.Designation == "MY-PROFILE"));
    }

    [TestMethod]
    public void CopyNamesAreUnique()
    {
        var name = string.Join(" ", SampleProfile.Designation, "copy");
        var catalogue = SampleCatalogue.Instance with { EndProfiles = [.. SampleCatalogue.Instance.EndProfiles, SampleProfile.Copy(name)] };
        Assert.AreEqual(string.Join(" ", name, "2"), CatalogueService.CopyName(SampleProfile.Designation, SampleProfile.Scale, catalogue, "copy"));
    }

    [TestMethod]
    public async Task TheModuleRegistrysCatalogueIsFetchedForTheScaleAndCountryInTheSettings()
    {
        await new global::Tellurian.Trains.Drawing.App.Settings.SettingsService(_workbench).SaveAsync(new() { Scale = "H0", CountryId = global::Tellurian.Trains.Drawing.App.Settings.Countries.All.Single(c => c.Code == "SE").Id });
        _registry.Json = SampleCatalogue.Json;

        var preview = await Service().FetchFromModuleRegistryAsync();

        Assert.AreEqual(new Uri(ModuleRegistryClient.CatalogueUrl + "?scale=H0&country=SE"), _registry.Requested.Single());
        Assert.IsTrue(preview.HasEntries);
        Assert.HasCount(SampleCatalogue.Instance.EndProfiles.Count, preview.Items);
        Assert.IsEmpty(preview.Problems);
    }

    [TestMethod]
    public async Task WithoutScaleAndCountryTheWholeCatalogueIsFetched()
    {
        _registry.Json = SampleCatalogue.Json;

        await Service().FetchFromModuleRegistryAsync();

        Assert.AreEqual(new Uri(ModuleRegistryClient.CatalogueUrl), _registry.Requested.Single());
    }

    [TestMethod]
    [DataRow(false), DataRow(true)]
    public async Task ARegistryThatCantBeReachedIsReported(bool unreachable)
    {
        _registry.Unreachable = unreachable;
        _registry.Status = System.Net.HttpStatusCode.ServiceUnavailable;

        await Assert.ThrowsExactlyAsync<ModuleRegistryException>(() => Service().FetchFromModuleRegistryAsync());
    }

    /// <summary>A drawn profile that is the Registry's end profile 42, in the user's catalogue at <paramref name="revision"/>.</summary>
    private async Task<EndProfile> RegistryProfileAsync(int revision = 3)
    {
        var profile = SampleProfile.Copy("REGISTRY-42").With(p =>
        {
            p.Revision = revision;
            p.ExternalReferences = Schedules.Model.Layouts.ExternalReferences.WithModuleRegistryId(null, Schedules.Model.Layouts.ExternalReferences.EndProfileId, 42);
        });
        await Service().SaveEndProfileAsync(profile);
        return profile;
    }

    private async Task WithApiKeyAsync() =>
        await new global::Tellurian.Trains.Drawing.App.Settings.SettingsService(_workbench).SaveAsync(new() { ApiKey = "secret-key" });

    [TestMethod]
    public async Task ADrawingIsUploadedToTheRegistryAsANewRevision()
    {
        var profile = await RegistryProfileAsync(revision: 3);
        await WithApiKeyAsync();
        _registry.Json = """{"endProfileId":42,"widthMillimeter":500,"heightMillimeter":200}""";

        var (result, uploaded) = await Service().UploadToModuleRegistryAsync(profile);

        Assert.AreEqual(EndProfileUploadStatus.Saved, result.Status);
        var request = _registry.Requests.Single();
        Assert.AreEqual((HttpMethod.Post, ModuleRegistryClient.EndProfileDrawingAddress(42), "secret-key"), (request.Method, request.Address, request.ApiKey));
        Assert.AreEqual(4, DrawingFileSerializer.DeserializeEndProfile(request.Body!).Revision);
        Assert.AreEqual(4, uploaded!.Revision);
        Assert.AreEqual(4, (await Service().GetUserAsync()).FindEndProfile(profile.Id)!.Revision);
    }

    [TestMethod]
    public async Task TheRevisionIsHigherThanTheCataloguesCopy()
    {
        var profile = await RegistryProfileAsync(revision: 7);
        await WithApiKeyAsync();

        await Service().UploadToModuleRegistryAsync(profile.With(p => p.Revision = 2));

        Assert.AreEqual(8, DrawingFileSerializer.DeserializeEndProfile(_registry.Requests.Single().Body!).Revision);
    }

    [TestMethod]
    public async Task WithoutAnApiKeyNothingIsSent()
    {
        var profile = await RegistryProfileAsync();

        var (result, uploaded) = await Service().UploadToModuleRegistryAsync(profile);

        Assert.AreEqual(EndProfileUploadStatus.NoApiKey, result.Status);
        Assert.IsNull(uploaded);
        Assert.IsEmpty(_registry.Requests);
    }

    [TestMethod]
    public async Task AProfileThatIsntTheRegistrysIsNotSent()
    {
        await WithApiKeyAsync();

        var (result, _) = await Service().UploadToModuleRegistryAsync(NewProfile());

        Assert.AreEqual(EndProfileUploadStatus.NotInRegistry, result.Status);
        Assert.IsEmpty(_registry.Requests);
    }

    [TestMethod]
    [DataRow(System.Net.HttpStatusCode.Conflict, EndProfileUploadStatus.Conflict)]
    [DataRow(System.Net.HttpStatusCode.Forbidden, EndProfileUploadStatus.NotAllowed)]
    [DataRow(System.Net.HttpStatusCode.Unauthorized, EndProfileUploadStatus.NotAllowed)]
    [DataRow(System.Net.HttpStatusCode.BadRequest, EndProfileUploadStatus.Refused)]
    public async Task ARefusedUploadTellsWhyAndLeavesTheCatalogue(System.Net.HttpStatusCode answer, EndProfileUploadStatus expected)
    {
        var profile = await RegistryProfileAsync(revision: 3);
        await WithApiKeyAsync();
        _registry.Status = answer;
        _registry.Json = """{"title":"The drawing is older than the stored one.","status":409,"key":"EndProfileDrawingIsOlder"}""";

        var (result, uploaded) = await Service().UploadToModuleRegistryAsync(profile);

        Assert.AreEqual(expected, result.Status);
        Assert.AreEqual("The drawing is older than the stored one.", result.Message);
        Assert.IsNull(uploaded);
        Assert.AreEqual(3, (await Service().GetUserAsync()).FindEndProfile(profile.Id)!.Revision);
    }
}
