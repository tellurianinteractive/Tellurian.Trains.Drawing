namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// The result of an import: the files read, the files that couldn't be, and how many files were
/// skipped because they aren't drawing files of an accepted kind.
/// </summary>
public sealed record DrawingImportResult(IReadOnlyList<ImportedFile> Files, IReadOnlyList<ImportProblem> Problems, int Skipped)
{
    public IEnumerable<ImportedFile> OfKind(DrawingFileKind kind) => Files.Where(f => f.Header.Kind == kind);
}
