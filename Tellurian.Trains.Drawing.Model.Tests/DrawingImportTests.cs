using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class DrawingImportTests
{
    private static string ModuleJson() => DrawingFileSerializer.Serialize(ModuleFileContent.Create(TestModules.Straight(), TestCatalogue.Lookup));

    private static string ProfileJson() => DrawingFileSerializer.Serialize(TestProfiles.FremoSingle());

    private static MemoryStream Zip(params (string Name, string Text)[] entries)
    {
        var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, text) in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open(), Encoding.UTF8);
                writer.Write(text);
            }
        }
        buffer.Position = 0;
        return buffer;
    }

    [TestMethod]
    public void ReadsDrawingFilesInAnyFolderOfAZip()
    {
        using var zip = Zip(("modules/a.tdmodule", ModuleJson()), ("b.TDMODULE", ModuleJson()), ("profiles/p.tdendprofile", ProfileJson()), ("readme.txt", "Hello"));
        var result = DrawingImport.FromZip(zip);
        Assert.HasCount(3, result.Files);
        Assert.HasCount(2, result.OfKind(DrawingFileKind.Module));
        Assert.AreEqual(1, result.Skipped);
        Assert.IsEmpty(result.Problems);
        Assert.AreEqual("modules/a.tdmodule", result.Files[0].Name);
    }

    [TestMethod]
    public void OnlyAcceptedKindsAreImported()
    {
        using var zip = Zip(("a.tdmodule", ModuleJson()), ("p.tdendprofile", ProfileJson()));
        var result = DrawingImport.FromZip(zip, [DrawingFileKind.EndProfile]);
        Assert.HasCount(1, result.Files);
        Assert.AreEqual(DrawingFileKind.EndProfile, result.Files[0].Header.Kind);
        Assert.AreEqual(1, result.Skipped);
    }

    [TestMethod]
    public void BadFilesAreReportedAndTheOthersImported()
    {
        var newer = JsonNode.Parse(ModuleJson())!;
        newer["schemaVersion"] = 99;
        var result = DrawingImport.FromFiles(
        [
            ImportSource.FromText("good.tdmodule", ModuleJson()),
            ImportSource.FromText("broken.tdmodule", "{ not json"),
            ImportSource.FromText("other.tdmodule", "{ \"name\": \"x\" }"),
            ImportSource.FromText("newer.tdmodule", newer.ToJsonString()),
            ImportSource.FromText("profile.tdmodule", ProfileJson()),
            new("unreadable.tdmodule", () => throw new IOException("Locked")),
        ]);
        Assert.HasCount(1, result.Files);
        CollectionAssert.AreEqual(
            new[] { "broken.tdmodule", "other.tdmodule", "newer.tdmodule", "profile.tdmodule", "unreadable.tdmodule" },
            result.Problems.Select(p => p.Name).ToArray());
        StringAssert.Contains(result.Problems[2].Message, "Update the app");
    }

    [TestMethod]
    public void InvalidZipIsReported()
    {
        using var notZip = new MemoryStream(Encoding.UTF8.GetBytes("not a zip"));
        var result = DrawingImport.FromZip(notZip);
        Assert.IsEmpty(result.Files);
        Assert.HasCount(1, result.Problems);
    }
}
