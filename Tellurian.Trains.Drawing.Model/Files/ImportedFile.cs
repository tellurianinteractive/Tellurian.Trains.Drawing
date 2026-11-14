namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>A drawing file read by <see cref="DrawingImport"/>; its envelope is checked, its content not yet.</summary>
public sealed record ImportedFile(string Name, DrawingFileHeader Header, string Json);
