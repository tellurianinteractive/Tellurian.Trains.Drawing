namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>A file that <see cref="DrawingImport"/> couldn't import, and why.</summary>
public sealed record ImportProblem(string Name, string Message);
