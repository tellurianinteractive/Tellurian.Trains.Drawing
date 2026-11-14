namespace Tellurian.Trains.Drawing.App.Files;

/// <summary>Where a file was saved: its name and, where the browser allows, a stored handle for saving in place.</summary>
public sealed record SavedFile(string Name, string? HandleId = null);
