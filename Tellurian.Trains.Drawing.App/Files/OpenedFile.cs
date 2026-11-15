namespace Tellurian.Trains.Drawing.App.Files;

/// <summary>A file the user picked, with its text and, where the browser allows, a stored handle.</summary>
public sealed record OpenedFile(string Name, string Text, string? HandleId = null);
