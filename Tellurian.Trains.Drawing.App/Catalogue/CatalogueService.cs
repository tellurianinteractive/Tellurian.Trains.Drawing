using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Catalogue;

/// <summary>
/// The catalogue to work with (CA-1, CA-2): the built-in catalogue with the user's own entries layered
/// on top. The user's part is kept in the workbench as a catalogue file, and can be exported and imported (CA-3).
/// </summary>
public sealed class CatalogueService(IWorkbench workbench, IFileService files)
{
    public const string UserCatalogueKey = "user-catalogue";
    public const string ExportFileName = "my-catalogue.tdcatalogue";

    private DrawingCatalogue? _user;

    public static DrawingCatalogue BuiltIn => BuiltInCatalogue.Instance;

    /// <summary>The built-in and user entries merged; a user entry replaces a built-in one with the same id.</summary>
    public async Task<DrawingCatalogue> GetAsync(CancellationToken cancellationToken = default) =>
        DrawingCatalogue.Merge(BuiltIn, await GetUserAsync(cancellationToken));

    public async Task<DrawingCatalogue> GetUserAsync(CancellationToken cancellationToken = default)
    {
        if (_user is not null) return _user;
        var json = await workbench.GetValueAsync(UserCatalogueKey, cancellationToken);
        _user = json is null ? DrawingCatalogue.Empty : DrawingFileSerializer.DeserializeCatalogue(json);
        return _user;
    }

    public async Task SaveUserAsync(DrawingCatalogue user, CancellationToken cancellationToken = default)
    {
        await workbench.SetValueAsync(UserCatalogueKey, DrawingFileSerializer.Serialize(user), cancellationToken);
        _user = user;
    }

    /// <summary>Where an entry comes from: built in, the user's own, or the user's replacement of a built-in entry.</summary>
    public static CatalogueOrigin Origin(bool isBuiltIn, bool isUser) =>
        (isBuiltIn, isUser) switch
        {
            (true, true) => CatalogueOrigin.ReplacesBuiltIn,
            (false, true) => CatalogueOrigin.User,
            _ => CatalogueOrigin.BuiltIn,
        };

    /// <summary>Lets the user pick files, a ZIP archive or a folder, and compares the end profiles in them with the catalogue (UC-2).</summary>
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
        return new(EndProfileImport.Compare(profiles, await GetAsync(cancellationToken)), [.. result.Problems, .. problems], result.Skipped);
    }

    /// <summary>Adds the chosen imported end profiles to the user's catalogue.</summary>
    public async Task ApplyImportAsync(IEnumerable<EndProfileImportItem> chosen, CancellationToken cancellationToken = default) =>
        await SaveUserAsync(EndProfileImport.Apply(await GetUserAsync(cancellationToken), chosen), cancellationToken);

    /// <summary>Copies an end profile as a new user profile (CA-2) and returns the copy.</summary>
    public async Task<Model.EndProfiles.EndProfile> CopyEndProfileAsync(Guid id, string designation, CancellationToken cancellationToken = default)
    {
        var original = (await GetAsync(cancellationToken)).FindEndProfile(id) ?? throw new KeyNotFoundException($"There is no end profile {id}.");
        var copy = original.Copy(designation);
        var user = await GetUserAsync(cancellationToken);
        await SaveUserAsync(user with { EndProfiles = [.. user.EndProfiles, copy] }, cancellationToken);
        return copy;
    }

    /// <summary>Removes a user end profile; a replaced built-in profile becomes visible again.</summary>
    public async Task DeleteEndProfileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(cancellationToken);
        await SaveUserAsync(user with { EndProfiles = [.. user.EndProfiles.Where(p => p.Id != id)] }, cancellationToken);
    }

    /// <summary>Saves a standard as a user entry, replacing a built-in one with the same id; returns the problems if it isn't valid.</summary>
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

    /// <summary>Removes a user standard; a replaced built-in standard becomes visible again.</summary>
    public async Task DeleteStandardAsync(Guid id, CancellationToken cancellationToken = default) =>
        await SaveUserAsync((await GetUserAsync(cancellationToken)).WithoutStandard(id), cancellationToken);

    public async Task DeleteAddOnAsync(Guid id, CancellationToken cancellationToken = default) =>
        await SaveUserAsync((await GetUserAsync(cancellationToken)).WithoutAddOn(id), cancellationToken);

    public async Task DeleteOverrideAsync(EndProfileCompatibilityOverride entry, CancellationToken cancellationToken = default) =>
        await SaveUserAsync((await GetUserAsync(cancellationToken)).WithoutOverride(entry), cancellationToken);

    /// <summary>Saves the user's part of the catalogue as a file (CA-3); null if the user cancelled.</summary>
    public async Task<SavedFile?> ExportAsync(CancellationToken cancellationToken = default) =>
        await files.SaveAsync(DrawingFileSerializer.Serialize(await GetUserAsync(cancellationToken)), ExportFileName, handleId: null, saveAs: true, cancellationToken);

    /// <summary>A name for a copy that isn't used by another end profile of the same scale.</summary>
    public static string CopyName(string designation, string scale, DrawingCatalogue catalogue, string suffix)
    {
        var name = $"{designation} {suffix}";
        for (var n = 2; catalogue.FindEndProfile(name, scale) is not null; n++) name = $"{designation} {suffix} {n}";
        return name;
    }
}
