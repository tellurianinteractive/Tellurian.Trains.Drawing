using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.Model.Catalogue;

namespace Tellurian.Trains.Drawing.App.Catalogue;

/// <summary>
/// The problems with the user's catalogue entries, shown by the problems indicator in the top bar.
/// Checked when first asked for and again each time the user's catalogue is saved.
/// </summary>
public sealed class CatalogueProblems : IDisposable
{
    private readonly CatalogueService _catalogue;

    public CatalogueProblems(CatalogueService catalogue)
    {
        _catalogue = catalogue;
        _catalogue.Changed += OnCatalogueChanged;
    }

    public IReadOnlyList<CatalogueEntryProblem> Problems { get; private set; } = [];

    /// <summary>The catalogue that was checked, for the names of the entries.</summary>
    public DrawingCatalogue Catalogue { get; private set; } = CatalogueService.BuiltIn;

    public bool IsChecked { get; private set; }

    /// <summary>Raised after each check.</summary>
    public event Action? Changed;

    /// <summary>Checks the user's catalogue; a catalogue that can't be read leaves the previous result.</summary>
    public async Task CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _catalogue.GetUserAsync(cancellationToken);
            var merged = DrawingCatalogue.Merge(CatalogueService.BuiltIn, user);
            Problems = CatalogueValidation.ValidateUserEntries(user, merged);
            Catalogue = merged;
            IsChecked = true;
        }
        catch (StorageException)
        {
            // The pages that use the catalogue report storage problems themselves.
        }
        Changed?.Invoke();
    }

    private void OnCatalogueChanged() => _ = CheckAsync();

    public void Dispose() => _catalogue.Changed -= OnCatalogueChanged;
}
