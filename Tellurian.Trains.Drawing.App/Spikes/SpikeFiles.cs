using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Tellurian.Trains.Drawing.App.Spikes;

/// <summary>
/// Throw-away helpers for spike C (files first, IndexedDB as workbench).
/// Uses Utf8JsonWriter and JsonDocument only, so it is trim and AOT safe without a serializer context.
/// </summary>
public static class SpikeFiles
{
    public static string SampleModuleJson(int number)
    {
        using var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartObject();
            w.WriteString("format", "tellurian-drawing");
            w.WriteString("kind", "module");
            w.WriteNumber("schemaVersion", 0);
            w.WriteString("appVersion", "spike");
            w.WriteStartObject("content");
            w.WriteString("id", Guid.NewGuid().ToString());
            w.WriteString("name", $"Spike module {number:000}");
            w.WriteString("standard", number % 3 == 0 ? "MMM-2R" : "FREMO/H0-RE");
            w.WriteNumber("straight", 500 + (number % 6) * 250);
            w.WriteNumber("revision", 1);
            w.WriteString("modified", DateTimeOffset.UtcNow);
            w.WriteEndObject();
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    public static byte[] CreateSampleZip(int count)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (var i = 1; i <= count; i++)
            {
                var entry = zip.CreateEntry($"modules/spike-module-{i:000}.tdmodule", CompressionLevel.Optimal);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                writer.Write(SampleModuleJson(i));
            }
        }
        return buffer.ToArray();
    }

    /// <summary>Reads all *.tdmodule and *.json entries of a ZIP archive and parses them.</summary>
    public static ImportResult ReadZip(Stream seekableZip)
    {
        var result = new ImportResult();
        using var zip = new ZipArchive(seekableZip, ZipArchiveMode.Read);
        foreach (var entry in zip.Entries)
        {
            if (!IsDrawingFile(entry.FullName)) continue;
            using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
            result.Add(entry.FullName, reader.ReadToEnd());
        }
        return result;
    }

    public static bool IsDrawingFile(string name) =>
        name.EndsWith(".tdmodule", StringComparison.OrdinalIgnoreCase) ||
        name.EndsWith(".tdendprofile", StringComparison.OrdinalIgnoreCase) ||
        name.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns the kind and name of a drawing file, or an error text.</summary>
    public static (bool Ok, string Text) Describe(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var kind = root.TryGetProperty("kind", out var k) ? k.GetString() : "?";
            var name = root.TryGetProperty("content", out var c) && c.TryGetProperty("name", out var n) ? n.GetString() : "?";
            return (true, $"{kind}: {name}");
        }
        catch (JsonException ex)
        {
            return (false, $"invalid JSON: {ex.Message}");
        }
    }
}

public sealed class ImportResult
{
    public int Files { get; private set; }
    public int Valid { get; private set; }
    public List<string> Errors { get; } = [];
    public string? First { get; private set; }

    public void Add(string name, string json)
    {
        Files++;
        var (ok, text) = SpikeFiles.Describe(json);
        if (ok) { Valid++; First ??= text; }
        else Errors.Add($"{name}: {text}");
    }
}
