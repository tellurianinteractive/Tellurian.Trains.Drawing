using System.IO.Compression;
using System.Text;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// Reads many drawing files at once, from a ZIP archive or a folder (EX-9, UC-2, UC-7). Files are
/// recognised by their extension; a file that can't be read is reported, and the others are still imported.
/// </summary>
public static class DrawingImport
{
    /// <summary>The ZIP file name extension, including the dot.</summary>
    public const string ZipExtension = ".zip";

    /// <summary>
    /// Reads the drawing files in a ZIP archive, in any folder of it. Only files of the
    /// <paramref name="accepted"/> kinds are imported; null accepts every kind.
    /// </summary>
    public static DrawingImportResult FromZip(Stream archive, IReadOnlyCollection<DrawingFileKind>? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(archive);
        try
        {
            using var zip = new ZipArchive(archive, ZipArchiveMode.Read, leaveOpen: true);
            return FromFiles(zip.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).Select(e => new ImportSource(e.FullName, () => Read(e))), accepted);
        }
        catch (InvalidDataException ex)
        {
            return new([], [new(ZipExtension, $"The file isn't a valid ZIP archive: {ex.Message}")], 0);
        }
    }

    /// <summary>Reads the drawing files among <paramref name="files"/>, e.g. the files of a folder.</summary>
    public static DrawingImportResult FromFiles(IEnumerable<ImportSource> files, IReadOnlyCollection<DrawingFileKind>? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(files);
        var imported = new List<ImportedFile>();
        var problems = new List<ImportProblem>();
        var skipped = 0;
        foreach (var file in files)
        {
            var kind = DrawingFileFormat.KindOf(Path.GetExtension(file.Name));
            if (kind is null || (accepted is not null && !accepted.Contains(kind.Value)))
            {
                skipped++;
                continue;
            }
            try
            {
                var json = file.ReadText();
                var header = DrawingFileSerializer.ReadHeader(json);
                if (header.Kind != kind)
                    problems.Add(new(file.Name, $"The file is a {header.Kind} file, but its extension is for {kind} files."));
                else if (!header.IsSupported)
                    problems.Add(new(file.Name, $"The file has {kind} schema version {header.SchemaVersion}, which is newer than this version of the app reads. Update the app to import it."));
                else
                    imported.Add(new(file.Name, header, json));
            }
            catch (DrawingFileException ex)
            {
                problems.Add(new(file.Name, ex.Message));
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or DecoderFallbackException)
            {
                problems.Add(new(file.Name, $"The file couldn't be read: {ex.Message}"));
            }
        }
        return new(imported, problems, skipped);
    }

    private static string Read(ZipArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
