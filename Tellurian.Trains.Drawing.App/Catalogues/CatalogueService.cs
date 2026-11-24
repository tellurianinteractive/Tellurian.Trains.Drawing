using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.App.Settings;
using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.Model.Catalogues;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Catalogues;

/// <summary>
/// The user's catalogue (CA-2): the standards, add-ons, end profiles and overrides the user has imported or made. The
/// app has no catalogue of its own (survey step 7); a new user imports a catalogue file (CA-3), and later one from the
/// Module Registry (CA-5). It is kept in the workbench as a catalogue file, and can be exported.
/// </summary>
public sealed class CatalogueService(IWorkbench workbench, IFileService files, SettingsService settings)
{
    public const string UserCatalogueKey = "user-catalogue";
    public const string ExportFileName = "my-catalogue.catalogue.json";

    private Catalogue? _user;

    /// <summary>Raised after the user's part of the catalogue has been saved.</summary>
    public event Action? Changed;

    /// <summary>The catalogue to work with: the user's.</summary>
    public Task<Catalogue> GetAsync(CancellationToken cancellationToken = default) => GetUserAsync(cancellationToken);

    /// <summary>
    /// The entries to choose from: <see cref="GetAsync"/> limited to the country (CA-6) and the scale (AP-9) in the
    /// settings. Drawings are still read with the whole catalogue.
    /// </summary>
    public async Task<Catalogue> GetForChoicesAsync(CancellationToken cancellationToken = default) =>
        (await GetAsync(cancellationToken)).ForChoices(await settings.CountryCodeAsync(cancellationToken), await settings.ScaleAsync(cancellationToken));

    public async Task<Catalogue> GetUserAsync(CancellationToken cancellationToken = default)
    {
        if (_user is not null) return _user;
        var json = await workbench.GetValueAsync(UserCatalogueKey, cancellationToken);
        _user = json is null ? Catalogue.Empty : DrawingFileSerializer.DeserializeCatalogue(json);
        return _user;
    }

    public async Task SaveUserAsync(Catalogue user, CancellationToken cancellationToken = default)
    {
        await workbench.SetValueAsync(UserCatalogueKey, DrawingFileSerializer.Serialize(user), cancellationToken);
        _user = user;
        Changed?.Invoke();
    }

    /// <summary>
    /// Lets the user pick files, a ZIP archive or a folder, and compares the end profiles in them with the catalogue
    /// (UC-2); a catalogue file's other entries come along (CA-3).
    /// </summary>
    public async Task<CatalogueImportPreview?> PickImportAsync(ImportSourceKind source, CancellationToken cancellationToken = default)
    {
        DrawingImportResult? result;
        switch (source)
        {
            case ImportSourceKind.Files:
                var picked = await files.OpenAsync(FileTypes.Of([.. EndProfileImport.Kinds]), multiple: true, cancellationToken);
                result = picked is null ? null : DrawingImport.FromFiles(picked.Select(f => ImportSource.FromText(f.Name, f.Text)), EndProfileImport.Kinds);
                break;
            case ImportSourceKind.Archive:
                await using (var archive = await files.OpenArchiveAsync(cancellationToken))
                {
                    result = archive is null ? null : DrawingImport.FromZip(archive, EndProfileImport.Kinds);
                }
                break;
            default:
                var folder = await files.OpenFolderAsync(FileTypes.Of([.. EndProfileImport.Kinds]), cancellationToken);
                result = folder is null ? null : DrawingImport.FromFiles(folder.Files.Select(f => ImportSource.FromText(f.Name, f.Text)), EndProfileImport.Kinds);
                break;
        }
        if (result is null) return null;
        var (profiles, problems) = EndProfileImport.Read(result.Files);
        return new(EndProfileImport.Compare(profiles, await GetAsync(cancellationToken)), [.. result.Problems, .. problems], result.Skipped, EndProfileImport.Entries(result.Files));
    }

    /// <summary>
    /// Takes the imported catalogue entries and the chosen end profiles into the user's catalogue; an entry with the id
    /// of one of the user's replaces it.
    /// </summary>
    public async Task ApplyImportAsync(IEnumerable<EndProfileImportItem> chosen, Catalogue? entries = null, CancellationToken cancellationToken = default) =>
        await SaveUserAsync(EndProfileImport.Apply(await GetUserAsync(cancellationToken), entries ?? Catalogue.Empty, chosen), cancellationToken);

    /// <summary>Adds end profiles to the user's catalogue, e.g. those a module embeds and the catalogue lacks; one with a known id replaces it.</summary>
    public async Task AddEndProfilesAsync(IEnumerable<EndProfile> profiles, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(cancellationToken);
        await SaveUserAsync(profiles.Aggregate(user, (catalogue, profile) => catalogue.With(profile)), cancellationToken);
    }

    /// <summary>Copies an end profile as a new user profile (CA-2) and returns the copy.</summary>
    public async Task<EndProfile> CopyEndProfileAsync(Guid id, string designation, CancellationToken cancellationToken = default)
    {
        var original = (await GetAsync(cancellationToken)).FindEndProfile(id) ?? throw new KeyNotFoundException($"There is no end profile {id}.");
        var copy = original.Copy(designation);
        var user = await GetUserAsync(cancellationToken);
        await SaveUserAsync(user with { EndProfiles = [.. user.EndProfiles, copy] }, cancellationToken);
        return copy;
    }

    /// <summary>Saves an end profile in the user's catalogue, replacing one with the same id (EP-1, CA-2).</summary>
    public async Task SaveEndProfileAsync(EndProfile profile, CancellationToken cancellationToken = default) =>
        await SaveUserAsync((await GetUserAsync(cancellationToken)).With(profile), cancellationToken);

    /// <summary>Removes an end profile from the user's catalogue.</summary>
    public async Task DeleteEndProfileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(cancellationToken);
        await SaveUserAsync(user with { EndProfiles = [.. user.EndProfiles.Where(p => p.Id != id)] }, cancellationToken);
    }

    /// <summary>Saves a standard in the user's catalogue, replacing one with the same id; returns the problems if it isn't valid.</summary>
    public async Task<IReadOnlyList<CatalogueProblem>> SaveStandardAsync(ModuleStandard standard, CancellationToken cancellationToken = default)
    {
        var problems = CatalogueValidation.Validate(standard, await GetAsync(cancellationToken));
        if (problems.Count == 0) await SaveUserAsync((await GetUserAsync(cancellationToken)).With(standard), cancellationToken);
        return problems;
    }

    public async Task<IReadOnlyList<CatalogueProblem>> SaveAddOnAsync(StandardAddOn addOn, CancellationToken cancellationToken = default)
    {
        var problems = CatalogueValidation.Validate(addOn, await GetAsync(cancellationToken));
        if (problems.Count == 0) await SaveUserAsync((await GetUserAsync(cancellationToken)).With(addOn), cancellationToken);
        return problems;
    }

    /// <summary>Saves an override; <paramref name="replacing"/> is the override being edited, if any.</summary>
    public async Task<IReadOnlyList<CatalogueProblem>> SaveOverrideAsync(EndProfileCompatibilityOverride entry, EndProfileCompatibilityOverride? replacing, CancellationToken cancellationToken = default)
    {
        var problems = CatalogueValidation.Validate(entry, await GetAsync(cancellationToken), replacing);
        if (problems.Count == 0) await SaveUserAsync((await GetUserAsync(cancellationToken)).With(entry, replacing), cancellationToken);
        return problems;
    }

    /// <summary>Removes a standard from the user's catalogue.</summary>
    public async Task DeleteStandardAsync(Guid id, CancellationToken cancellationToken = default) =>
        await SaveUserAsync((await GetUserAsync(cancellationToken)).WithoutStandard(id), cancellationToken);

    public async Task DeleteAddOnAsync(Guid id, CancellationToken cancellationToken = default) =>
        await SaveUserAsync((await GetUserAsync(cancellationToken)).WithoutAddOn(id), cancellationToken);

    public async Task DeleteOverrideAsync(EndProfileCompatibilityOverride entry, CancellationToken cancellationToken = default) =>
        await SaveUserAsync((await GetUserAsync(cancellationToken)).WithoutOverride(entry), cancellationToken);

    /// <summary>Saves the user's catalogue as a file (CA-3); null if the user cancelled.</summary>
    public async Task<SavedFile?> ExportAsync(CancellationToken cancellationToken = default) =>
        await files.SaveAsync(DrawingFileSerializer.Serialize(await GetUserAsync(cancellationToken)), ExportFileName, handleId: null, saveAs: true, cancellationToken);

    /// <summary>A name for a copy that isn't used by another end profile of the same scale.</summary>
    public static string CopyName(string designation, string scale, Catalogue catalogue, string suffix)
    {
        var name = $"{designation} {suffix}";
        for (var n = 2; catalogue.FindEndProfile(name, scale) is not null; n++) name = $"{designation} {suffix} {n}";
        return name;
    }
}
