namespace Tellurian.Trains.Drawing.App.Catalogue;

/// <summary>Where a catalogue entry comes from (CA-2).</summary>
public enum CatalogueOrigin
{
    BuiltIn,
    User,

    /// <summary>A user entry with the id of a built-in entry, which it replaces.</summary>
    ReplacesBuiltIn,
}
