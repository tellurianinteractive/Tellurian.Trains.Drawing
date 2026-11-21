using System.Text.Json.Nodes;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>
/// The schemas and examples in <c>schemas/</c> are public contracts (EX-7). After a deliberate change, run
/// the tests with <c>TELLURIAN_UPDATE_SCHEMAS=1</c> to rewrite them, and log the change in <c>schemas/CHANGELOG.md</c>.
/// </summary>
[TestClass]
public sealed class DrawingFileSchemaTests
{
    private static string ModuleJson(Module module) => DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, TestCatalogue.Lookup));

    [TestMethod]
    public void PublishedSchemasAreUpToDate()
    {
        foreach (var kind in DrawingFileSchema.Kinds)
        {
            var path = Path.Combine(RepositoryFiles.SchemaDirectory, DrawingFileSchema.FileName(kind));
            var generated = DrawingFileSchema.Create(kind);
            if (RepositoryFiles.IsUpdating) RepositoryFiles.Write(path, generated);
            Assert.IsTrue(File.Exists(path), $"{path} is missing. Run the tests with {RepositoryFiles.UpdateVariable}=1 to create it.");
            Assert.IsTrue(JsonNode.DeepEquals(generated, JsonNode.Parse(File.ReadAllText(path))),
                $"The {kind} schema has changed. Check that the change is backward compatible (EX-7), run the tests with {RepositoryFiles.UpdateVariable}=1 and log it in schemas/CHANGELOG.md.");
        }
    }

    [TestMethod]
    public void SchemaVersionIsInTheFileName() =>
        Assert.AreEqual("module.v2.json", DrawingFileSchema.FileName(DrawingFileKind.Module));

    [TestMethod]
    public void EveryKindHasASchema() =>
        CollectionAssert.AreEquivalent(Enum.GetValues<DrawingFileKind>(), DrawingFileSchema.Kinds.ToArray());

    [TestMethod]
    public void EndProfileFilesMatchTheSchema()
    {
        SchemaValidator.AssertValid(DrawingFileKind.EndProfile, DrawingFileSerializer.Serialize(TestProfiles.FremoSingle()));
        SchemaValidator.AssertValid(DrawingFileKind.EndProfile, DrawingFileSerializer.Serialize(TestProfiles.Asymmetric() with { Source = new() { Title = "T" } }));
    }

    [TestMethod]
    public void ModuleFilesMatchTheSchema()
    {
        foreach (var module in new[] { TestModules.Straight(), TestModules.Curve(), TestModules.Terminus(), TestModules.Station(), TestModules.Junction(), TestModules.Transition(), TestModules.WithEverything() })
            SchemaValidator.AssertValid(DrawingFileKind.Module, ModuleJson(module));
    }

    [TestMethod]
    public void CatalogueFilesMatchTheSchema() =>
        SchemaValidator.AssertValid(DrawingFileKind.Catalogue, DrawingFileSerializer.Serialize(TestCatalogue.Instance));

    [TestMethod]
    public void HallFilesMatchTheSchema()
    {
        SchemaValidator.AssertValid(DrawingFileKind.Hall, DrawingFileSerializer.Serialize(TestHalls.Club()));
        SchemaValidator.AssertValid(DrawingFileKind.Hall, DrawingFileSerializer.Serialize(Halls.Hall.Rectangle("Empty", 10000, 6000)));
    }

    [TestMethod]
    public void HallPlanFilesMatchTheSchema() =>
        SchemaValidator.AssertValid(DrawingFileKind.HallPlan, DrawingFileSerializer.Serialize(TestHalls.Plan(TestCatalogue.Instance)));

    [TestMethod]
    public void SchemaRejectsInvalidFiles()
    {
        var valid = JsonNode.Parse(DrawingFileSerializer.Serialize(TestProfiles.FremoSingle()))!;

        var missingDesignation = valid.DeepClone();
        missingDesignation["content"]!.AsObject().Remove("designation");
        Assert.IsFalse(SchemaValidator.IsValid(DrawingFileKind.EndProfile, missingDesignation.ToJsonString()));

        var wrongKind = valid.DeepClone();
        wrongKind["kind"] = "module";
        Assert.IsFalse(SchemaValidator.IsValid(DrawingFileKind.EndProfile, wrongKind.ToJsonString()));

        var wrongEnum = valid.DeepClone();
        Assert.AreEqual("TwoRail", (string)wrongEnum["content"]!["tracks"]![0]!["railSystem"]!);
        // Enum values are written as in the code. The camelCase of earlier files is read, but isn't what version 2 writes.
        wrongEnum["content"]!["tracks"]![0]!["railSystem"] = "twoRail";
        Assert.IsFalse(SchemaValidator.IsValid(DrawingFileKind.EndProfile, wrongEnum.ToJsonString()));

        var newerVersion = valid.DeepClone();
        newerVersion["schemaVersion"] = DrawingFileFormat.CurrentSchemaVersion(DrawingFileKind.EndProfile) + 1;
        Assert.IsFalse(SchemaValidator.IsValid(DrawingFileKind.EndProfile, newerVersion.ToJsonString()));

        var badStep = JsonNode.Parse(ModuleJson(TestModules.Straight()))!;
        badStep["content"]!["module"]!["drawing"]!["mainPath"]!["steps"]![0]!["type"] = "spline";
        Assert.IsFalse(SchemaValidator.IsValid(DrawingFileKind.Module, badStep.ToJsonString()));
    }

    [TestMethod]
    public void ExamplesMatchTheSchemasAndCanBeRead()
    {
        if (RepositoryFiles.IsUpdating) SchemaExamples.Write(RepositoryFiles.ExamplesDirectory);
        var files = Directory.Exists(RepositoryFiles.ExamplesDirectory) ? Directory.GetFiles(RepositoryFiles.ExamplesDirectory) : [];
        Assert.IsNotEmpty(files, $"There are no examples. Run the tests with {RepositoryFiles.UpdateVariable}=1 to create them.");
        foreach (var file in files)
        {
            var kind = DrawingFileFormat.KindOf(Path.GetFileName(file));
            Assert.IsNotNull(kind, $"{Path.GetFileName(file)} has an unknown extension.");
            StringAssert.EndsWith(file, DrawingFileFormat.Extension(kind.Value), "Examples have the current file names.");
            var json = File.ReadAllText(file);
            SchemaValidator.AssertValid(kind.Value, json);
            Assert.AreEqual(kind.Value, DrawingFileSerializer.ReadHeader(json).Kind);
            switch (kind.Value)
            {
                case DrawingFileKind.EndProfile: DrawingFileSerializer.DeserializeEndProfile(json); break;
                case DrawingFileKind.Module: DrawingFileSerializer.DeserializeModule(json); break;
                case DrawingFileKind.Catalogue: DrawingFileSerializer.DeserializeCatalogue(json); break;
                case DrawingFileKind.Hall: DrawingFileSerializer.DeserializeHall(json); break;
                case DrawingFileKind.HallPlan: DrawingFileSerializer.DeserializeHallPlan(json); break;
                default: Assert.Fail($"{Path.GetFileName(file)}: no reader for {kind}."); break;
            }
        }
    }
}
