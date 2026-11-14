using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Modules;
using Module = Tellurian.Trains.Drawing.Model.Modules.Module;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class DrawingFileTests
{
    private static ModuleFileContent ModuleFile(Module module) => ModuleFileContent.Create(module, TestCatalogue.Lookup);

    private static JsonObject Envelope(string json) => JsonNode.Parse(json)!.AsObject();

    // Exit criterion of Phase 2: saved to a file and loaded back identically.
    [TestMethod]
    public void EndProfileRoundTripsIdentically()
    {
        var profile = TestProfiles.Asymmetric() with
        {
            Standards = [new(TestCatalogue.FremoId, "FREMO/H0-RE")],
            Dowels = [new(new(-200, -90), 10)],
            Source = new() { Title = "FREMO H0-RE", Url = "https://example.org/b96.pdf", Revision = "2009" },
            RegistryId = 12,
            Revision = 3,
        };
        var json = DrawingFileSerializer.Serialize(profile);
        var read = DrawingFileSerializer.DeserializeEndProfile(json);
        Assert.AreEqual(json, DrawingFileSerializer.Serialize(read));
        Assert.AreEqual(profile.Id, read.Id);
        Assert.AreEqual(JointFit.Fits, JointFitEvaluator.Evaluate(read, ProfileOrientation.Mirrored, profile, ProfileOrientation.Normal).Fit);
    }

    [TestMethod]
    public void ModuleRoundTripsIdentically()
    {
        var json = DrawingFileSerializer.Serialize(ModuleFile(TestModules.WithEverything()));
        var read = DrawingFileSerializer.DeserializeModule(json);
        Assert.AreEqual(json, DrawingFileSerializer.Serialize(read));
    }

    [TestMethod]
    public void AutoDrawnModulesRoundTripIdentically()
    {
        foreach (var module in new[] { TestModules.Straight(), TestModules.Curve(), TestModules.Terminus(), TestModules.Station(), TestModules.Junction(), TestModules.Transition() })
        {
            var json = DrawingFileSerializer.Serialize(ModuleFile(module));
            Assert.AreEqual(json, DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeModule(json)), module.Name);
        }
    }

    // NF-8: exit poses are reproducible after save and load.
    [TestMethod]
    public void ReadModuleHasTheSameGeometry()
    {
        var module = TestModules.Curve();
        var read = DrawingFileSerializer.DeserializeModule(DrawingFileSerializer.Serialize(ModuleFile(module)));
        var exits = module.DefaultVariant.Exits.Zip(read.Module.DefaultVariant.Exits);
        Assert.IsTrue(exits.All(e => e.First.Pose.IsCloseTo(e.Second.Pose, Tolerance.Length, Tolerance.Angle)));
        var outline = ModuleGeometry.Outline(read.Module.DefaultVariant, read.FindEndProfile)!;
        Assert.AreEqual(ModuleGeometry.Outline(module.DefaultVariant, TestCatalogue.Lookup)!.Length, outline.Length, Precision);
    }

    [TestMethod]
    public void TrackElementsKeepTheirTypes()
    {
        var module = TestModules.WithEverything();
        var read = DrawingFileSerializer.DeserializeModule(DrawingFileSerializer.Serialize(ModuleFile(module)));
        CollectionAssert.AreEqual(
            module.DefaultVariant.Tracks.Select(t => t.GetType()).ToArray(),
            read.Module.DefaultVariant.Tracks.Select(t => t.GetType()).ToArray());
    }

    [TestMethod]
    public void CatalogueRoundTripsIdentically()
    {
        var catalogue = TestCatalogue.Instance with
        {
            Overrides = [new() { FirstEndProfileId = TestCatalogue.FremoSingle.Id, SecondEndProfileId = TestCatalogue.Mmm480.Id, Fit = JointFit.FitsWithClamps, Note = "Known." }],
        };
        var json = DrawingFileSerializer.Serialize(catalogue);
        Assert.AreEqual(json, DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeCatalogue(json)));
    }

    [TestMethod]
    public void ModuleFileEmbedsItsEndProfilesOnce()
    {
        var content = ModuleFile(TestModules.Transition());
        CollectionAssert.AreEquivalent(new[] { TestCatalogue.FremoDouble.Id, TestCatalogue.FremoDoubleWide.Id }, content.EndProfiles.Select(p => p.Id).ToArray());
        Assert.HasCount(1, ModuleFile(TestModules.Straight()).EndProfiles);
    }

    [TestMethod]
    public void ModuleFileIncludesDerivedMeasuresPerVariant()
    {
        var module = TestModules.WithEverything();
        var content = ModuleFile(module);
        CollectionAssert.AreEqual(module.Variants.Select(v => v.Id).ToArray(), content.Measures.Select(m => m.VariantId).ToArray());
        Assert.AreEqual(1200, content.Measures[0].Measures.Length!.Value, Precision);
        var json = Envelope(DrawingFileSerializer.Serialize(content));
        Assert.AreEqual(500, (double)json["content"]!["measures"]![0]!["measures"]!["width"]!, Precision);
    }

    [TestMethod]
    public void ModuleFileNeedsAllEndProfiles() =>
        Assert.ThrowsExactly<KeyNotFoundException>(() => ModuleFileContent.Create(TestModules.Straight(), _ => null));

    [TestMethod]
    public void EnvelopeFollowsTheSpecification()
    {
        var json = Envelope(DrawingFileSerializer.Serialize(ModuleFile(TestModules.Straight()), "1.2.3"));
        CollectionAssert.AreEqual(new[] { "format", "kind", "schemaVersion", "appVersion", "content" }, json.Select(p => p.Key).ToArray());
        Assert.AreEqual(DrawingFileFormat.Format, (string?)json["format"]);
        Assert.AreEqual("module", (string?)json["kind"]);
        Assert.AreEqual(1, (int)json["schemaVersion"]!);
        Assert.AreEqual("1.2.3", (string?)json["appVersion"]);
    }

    [TestMethod]
    public void AppVersionDefaultsToTheLibraryVersion()
    {
        var json = Envelope(DrawingFileSerializer.Serialize(TestProfiles.FremoSingle()));
        Assert.AreEqual(DrawingFileFormat.LibraryVersion, (string?)json["appVersion"]);
        Assert.DoesNotContain("+", DrawingFileFormat.LibraryVersion);
    }

    [TestMethod]
    public void ComputedPropertiesAreNotWritten()
    {
        var json = DrawingFileSerializer.Serialize(ModuleFile(TestModules.WithEverything()));
        foreach (var name in new[] { "\"defaultVariant\"", "\"hasScrewHoles\"", "\"slipRadius\"", "\"asVector\"", "\"isRamp\"" })
            Assert.DoesNotContain(name, json);
        Assert.DoesNotContain("\"kind\": \"passThrough\"", json);
    }

    [TestMethod]
    public void PosesAndAnglesAreFlat()
    {
        var json = Envelope(DrawingFileSerializer.Serialize(ModuleFile(TestModules.Straight())));
        var pose = json["content"]!["module"]!["variants"]![0]!["exits"]![1]!["pose"]!.AsObject();
        CollectionAssert.AreEqual(new[] { "x", "y", "heading" }, pose.Select(p => p.Key).ToArray());
        Assert.AreEqual(1200, (double)pose["x"]!, Precision);
    }

    [TestMethod]
    public void EveryEnumInTheFilesIsWrittenInCamelCase()
    {
        var enums = typeof(DrawingFileSerializer).Assembly.GetTypes().Where(t => t.IsEnum && t.GetCustomAttribute<FlagsAttribute>() is null);
        var checkedCount = 0;
        foreach (var type in enums)
        {
            var typeInfo = DrawingJsonContext.Default.GetTypeInfo(type);
            if (typeInfo is null) continue;
            var json = JsonSerializer.Serialize(Enum.GetValues(type).GetValue(0), typeInfo);
            Assert.IsTrue(char.IsLower(json[1]), $"{type.Name} is written as {json}; add a {nameof(CamelCaseEnumConverter<>)} for it to {nameof(DrawingJsonContext)}.");
            checkedCount++;
        }
        Assert.IsGreaterThanOrEqualTo(10, checkedCount);
    }

    [TestMethod]
    public void HeaderTellsTheKind()
    {
        var header = DrawingFileSerializer.ReadHeader(DrawingFileSerializer.Serialize(TestCatalogue.Instance, "9.9"));
        Assert.AreEqual(new DrawingFileHeader(DrawingFileKind.Catalogue, 1, "9.9"), header);
        Assert.IsTrue(header.IsSupported);
    }

    [TestMethod]
    public void StreamsCanBeRead()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(DrawingFileSerializer.Serialize(TestProfiles.FremoSingle())));
        Assert.AreEqual("TEST-FREMO-1", DrawingFileSerializer.DeserializeEndProfile(stream).Designation);
    }

    [TestMethod]
    public void InvalidJsonIsRefused() =>
        AssertRefused(DrawingFileError.InvalidJson, () => DrawingFileSerializer.DeserializeEndProfile("{ not json"));

    [TestMethod]
    [DataRow("""{ "kind": "endProfile", "schemaVersion": 1, "content": {} }""")]
    [DataRow("""{ "format": "other", "kind": "endProfile", "schemaVersion": 1, "content": {} }""")]
    [DataRow("""{ "format": "tellurian-drawing", "kind": "unknown", "schemaVersion": 1, "content": {} }""")]
    [DataRow("""{ "format": "tellurian-drawing", "kind": "endProfile", "content": {} }""")]
    [DataRow("""{ "format": "tellurian-drawing", "kind": "endProfile", "schemaVersion": 0, "content": {} }""")]
    [DataRow("""{ "format": "tellurian-drawing", "kind": "endProfile", "schemaVersion": 1 }""")]
    [DataRow("""[1, 2]""")]
    public void NonDrawingFilesAreRefused(string json) =>
        AssertRefused(DrawingFileError.NotADrawingFile, () => DrawingFileSerializer.DeserializeEndProfile(json));

    [TestMethod]
    public void WrongKindIsRefused() =>
        AssertRefused(DrawingFileError.WrongKind, () => DrawingFileSerializer.DeserializeModule(DrawingFileSerializer.Serialize(TestProfiles.FremoSingle())));

    // Spec §6: newer versions are refused with a clear message.
    [TestMethod]
    public void NewerSchemaVersionIsRefused()
    {
        var json = Envelope(DrawingFileSerializer.Serialize(TestProfiles.FremoSingle()));
        json["schemaVersion"] = 2;
        var exception = AssertRefused(DrawingFileError.NewerSchemaVersion, () => DrawingFileSerializer.DeserializeEndProfile(json.ToJsonString()));
        StringAssert.Contains(exception.Message, "Update the app");
        Assert.IsFalse(DrawingFileSerializer.ReadHeader(json.ToJsonString()).IsSupported);
    }

    [TestMethod]
    public void MissingRequiredPropertyIsInvalidContent()
    {
        var json = Envelope(DrawingFileSerializer.Serialize(TestProfiles.FremoSingle()));
        json["content"]!.AsObject().Remove("designation");
        AssertRefused(DrawingFileError.InvalidContent, () => DrawingFileSerializer.DeserializeEndProfile(json.ToJsonString()));
    }

    [TestMethod]
    public void UnknownPropertiesAreIgnored()
    {
        // Backward-compatible additions within a schema version must not break older readers (EX-7).
        var json = Envelope(DrawingFileSerializer.Serialize(TestProfiles.FremoSingle()));
        json["content"]!["futureProperty"] = new JsonObject { ["value"] = 1 };
        json["futureEnvelopeProperty"] = "x";
        Assert.AreEqual("TEST-FREMO-1", DrawingFileSerializer.DeserializeEndProfile(json.ToJsonString()).Designation);
    }

    [TestMethod]
    public void OlderVersionsAreMigratedStepByStep()
    {
        var json = Envelope(DrawingFileSerializer.Serialize(TestProfiles.FremoSingle()));
        json["content"]!.AsObject().Remove("designation", out var designation);
        json["content"]!["name"] = designation!.DeepClone();
        var applied = new List<int>();
        IReadOnlyList<DrawingFileMigration> migrations =
        [
            new(DrawingFileKind.EndProfile, 1, content => { applied.Add(1); return Rename(content, "name", "title"); }),
            new(DrawingFileKind.EndProfile, 2, content => { applied.Add(2); return Rename(content, "title", "designation"); }),
            new(DrawingFileKind.Module, 1, _ => throw new InvalidOperationException("Wrong kind.")),
        ];
        var profile = DrawingFileSerializer.Read(json, DrawingFileKind.EndProfile, DrawingJsonContext.Default.DrawingFileEndProfile, migrations, currentVersion: 3);
        Assert.AreEqual("TEST-FREMO-1", profile.Designation);
        CollectionAssert.AreEqual(new[] { 1, 2 }, applied);
    }

    [TestMethod]
    public void MissingMigrationIsReported() =>
        AssertRefused(DrawingFileError.InvalidContent, () => DrawingFileSerializer.Read(
            Envelope(DrawingFileSerializer.Serialize(TestProfiles.FremoSingle())), DrawingFileKind.EndProfile,
            DrawingJsonContext.Default.DrawingFileEndProfile, [], currentVersion: 2));

    [TestMethod]
    public void CurrentVersionNeedsNoMigrations() => Assert.IsEmpty(DrawingFileMigrations.All);

    [TestMethod]
    [DataRow(DrawingFileKind.EndProfile, ".tdendprofile")]
    [DataRow(DrawingFileKind.Module, ".tdmodule")]
    [DataRow(DrawingFileKind.Hall, ".tdhall")]
    [DataRow(DrawingFileKind.HallPlan, ".tdhallplan")]
    [DataRow(DrawingFileKind.Catalogue, ".tdcatalogue")]
    public void ExtensionsMatchKinds(DrawingFileKind kind, string extension)
    {
        Assert.AreEqual(extension, DrawingFileFormat.Extension(kind));
        Assert.AreEqual(kind, DrawingFileFormat.KindOf(extension));
        Assert.AreEqual(kind, DrawingFileFormat.KindOf(extension[1..].ToUpperInvariant()));
    }

    [TestMethod]
    public void UnknownExtensionHasNoKind() => Assert.IsNull(DrawingFileFormat.KindOf(".json"));

    private static JsonNode Rename(JsonNode content, string from, string to)
    {
        content.AsObject().Remove(from, out var value);
        content[to] = value;
        return content;
    }

    private static DrawingFileException AssertRefused(DrawingFileError error, Action read)
    {
        var exception = Assert.ThrowsExactly<DrawingFileException>(read);
        Assert.AreEqual(error, exception.Error, exception.Message);
        return exception;
    }
}
