using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Catalogue;

/// <summary>
/// The catalogue to work with (CA-1, CA-2): the built-in catalogue with the user's own entries layered
/// on top. The user's part is kept in the workbench as a catalogue file.
/// </summary>
public sealed class CatalogueService(IWorkbench workbench)
{
    public const string UserCatalogueKey = "user-catalogue";

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
}
