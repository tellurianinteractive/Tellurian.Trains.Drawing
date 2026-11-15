namespace Tellurian.Trains.Drawing.App.Storage;

/// <summary>A named value in the workbench, e.g. settings or the user catalogue, as JSON.</summary>
public sealed record StoredValue(string Id, string Json);
