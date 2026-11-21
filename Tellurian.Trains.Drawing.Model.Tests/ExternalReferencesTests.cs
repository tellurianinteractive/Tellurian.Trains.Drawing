using static Tellurian.Trains.Schedules.Model.Layouts.LayoutPartCopying;
using System.Text.Json.Nodes;
using Tellurian.Trains.Drawing.Model.Files;
using Module = Tellurian.Trains.Drawing.Model.Modules.Module;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>
/// What belongs to other systems, and what a newer version of the app has added, survives a save
/// (spec §6; tasks A1, A2, A3 and A6).
/// </summary>
[TestClass]
public sealed class ExternalReferencesTests
{
    private const string Unknown = "x-unknown";
    private const string RegistryEntry = """{"stationId":1234,"version":{"mark":"abc","savedAt":"2026-11-18T10:15:00Z"},"somethingNew":[1,{"a":null}]}""";

    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();

    private static string ModuleJson(Module module) => DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, TestCatalogue.Lookup));

    [TestMethod]
    public void AnEntryIsWrittenBackUnchanged()
    {
        var module = TestModules.Straight().With(c => c.ExternalReferences = TestReferences.Registry(RegistryEntry));
        var read = DrawingFileSerializer.DeserializeModule(ModuleJson(module)).Module;
        Assert.AreEqual(RegistryEntry, TestReferences.RegistryEntry(read.ExternalReferences));
        Assert.IsTrue(ExternalReferences.Same(module.ExternalReferences, read.ExternalReferences));
    }

    [TestMethod]
    public void EntriesOfOtherSystemsAreKeptBesideTheRegistrys()
    {
        var file = Parse(ModuleJson(TestModules.Straight()));
        file["content"]!["module"]![ExternalReferences.Section] = new JsonObject
        {
            [ExternalReferences.ModuleRegistry] = JsonNode.Parse(RegistryEntry),
            ["anotherSystem"] = "any JSON value",
        };
        var written = Parse(DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeModule(file.ToJsonString())));
        Assert.IsTrue(JsonNode.DeepEquals(file["content"]!["module"]![ExternalReferences.Section], written["content"]!["module"]![ExternalReferences.Section]));
    }

    [TestMethod]
    public void EveryFileKindHasTheSection()
    {
        var references = TestReferences.Registry(RegistryEntry);
        string[] files =
        [
            ModuleJson(TestModules.Straight().With(c => c.ExternalReferences = references)),
            DrawingFileSerializer.Serialize(TestProfiles.FremoSingle() with { ExternalReferences = references }),
            DrawingFileSerializer.Serialize(TestCatalogue.Instance with { ExternalReferences = references }),
            DrawingFileSerializer.Serialize(TestHalls.Club() with { ExternalReferences = references }),
            DrawingFileSerializer.Serialize(TestHalls.Plan(TestCatalogue.Instance) with { ExternalReferences = references }),
        ];
        foreach (var json in files)
        {
            var header = DrawingFileSerializer.ReadHeader(json);
            SchemaValidator.AssertValid(header.Kind, json);
            var content = Parse(json)["content"]!;
            var drawing = header.Kind == DrawingFileKind.Module ? content["module"]! : content;
            Assert.AreEqual(RegistryEntry, drawing[ExternalReferences.Section]![ExternalReferences.ModuleRegistry]!.ToJsonString(), header.Kind.ToString());
        }
    }

    [TestMethod]
    public void TheSchemaSaysOnlyThatTheSectionIsAnObject()
    {
        var file = Parse(ModuleJson(TestModules.Straight()));
        file["content"]!["module"]![ExternalReferences.Section] = "text";
        Assert.IsFalse(SchemaValidator.IsValid(DrawingFileKind.Module, file.ToJsonString()));
    }

    // A2: modules, end profiles and standards embedded in a hall plan carry their sections with them.
    [TestMethod]
    public void EmbeddedCopiesKeepTheirSections()
    {
        var plan = TestHalls.Plan(TestCatalogue.Instance);
        plan = plan with
        {
            Modules = [.. plan.Modules.Select(m => m.With(c => c.ExternalReferences = TestReferences.Registry("""{"stationId":1}""")))],
            EndProfiles = [.. plan.EndProfiles.Select(p => p with { ExternalReferences = TestReferences.Registry("""{"endProfileId":2}""") })],
            Standards = [.. plan.Standards.Select(s => s with { ExternalReferences = TestReferences.Registry("""{"standardId":3}""") })],
        };
        var read = DrawingFileSerializer.DeserializeHallPlan(DrawingFileSerializer.Serialize(plan));
        Assert.IsTrue(read.Modules.All(m => TestReferences.RegistryEntry(m.ExternalReferences) == """{"stationId":1}"""));
        Assert.IsTrue(read.EndProfiles.All(p => TestReferences.RegistryEntry(p.ExternalReferences) == """{"endProfileId":2}"""));
        Assert.IsTrue(read.Standards.All(s => TestReferences.RegistryEntry(s.ExternalReferences) == """{"standardId":3}"""));
        Assert.IsNotEmpty(read.Modules);
    }

    [TestMethod]
    public void AModuleFileEmbedsEndProfilesWithTheirSections()
    {
        var profile = TestCatalogue.FremoSingle with { ExternalReferences = TestReferences.Registry("""{"endProfileId":2}""") };
        var content = ModuleFileContent.Create(TestModules.Straight(), id => id == profile.Id ? profile : TestCatalogue.Lookup(id));
        var read = DrawingFileSerializer.DeserializeModule(DrawingFileSerializer.Serialize(content));
        Assert.AreEqual("""{"endProfileId":2}""", TestReferences.RegistryEntry(read.EndProfiles.Single().ExternalReferences));
    }

    // A3: the one function that makes a new drawing from an existing one today.
    [TestMethod]
    public void ACopyOfAnEndProfileLosesTheSection()
    {
        var original = TestCatalogue.FremoSingle with { ExternalReferences = TestReferences.Registry("""{"endProfileId":2}""") };
        var copy = original.Copy("MY-B96");
        Assert.AreNotEqual(original.Id, copy.Id);
        Assert.IsNull(copy.ExternalReferences);
        Assert.DoesNotContain(ExternalReferences.Section, DrawingFileSerializer.Serialize(copy));
    }

    [TestMethod]
    public void SameComparesContentNotInstances()
    {
        var a = TestReferences.Registry("""{ "endProfileId": 2 }""");
        Assert.IsTrue(ExternalReferences.Same(a, TestReferences.Registry("""{"endProfileId":2}""")));
        Assert.IsFalse(ExternalReferences.Same(a, TestReferences.Registry("""{"endProfileId":3}""")));
        Assert.IsFalse(ExternalReferences.Same(a, null));
        Assert.IsTrue(ExternalReferences.Same(null, new Dictionary<string, System.Text.Json.JsonElement>()));
    }

    // A6: a property that this version doesn't know, on any object of any file, is still there after a save.
    [TestMethod]
    public void UnknownPropertiesSurviveASaveEverywhere()
    {
        string[] files =
        [
            ModuleJson(TestModules.WithEverything()),
            ModuleJson(TestModules.Station()),
            DrawingFileSerializer.Serialize(TestProfiles.Asymmetric() with { Source = new() { Title = "T" } }),
            DrawingFileSerializer.Serialize(Catalogue.BuiltInCatalogue.Instance),
            DrawingFileSerializer.Serialize(TestHalls.Club()),
            DrawingFileSerializer.Serialize(TestHalls.Plan(TestCatalogue.Instance)),
        ];
        foreach (var json in files)
        {
            var file = Parse(json);
            var kind = DrawingFileSerializer.ReadHeader(json).Kind;
            var added = AddUnknownProperties(file["content"]!);
            Assert.IsGreaterThan(10, added, kind.ToString());

            var written = Parse(Rewrite(kind, file.ToJsonString()));

            Assert.AreEqual(null, FirstDifference(file["content"]!, written["content"]!, "content"), $"{kind}: lost or changed by a save.");
            SchemaValidator.AssertValid(kind, written.ToJsonString());
        }
    }

    [TestMethod]
    public void UnknownPropertiesDoNotMakeRecordsDiffer()
    {
        var file = Parse(DrawingFileSerializer.Serialize(TestProfiles.FremoSingle() with { Source = new() { Title = "T" } }));
        var plain = DrawingFileSerializer.DeserializeEndProfile(file.ToJsonString());
        file["content"]!["source"]![Unknown] = 1;
        var extended = DrawingFileSerializer.DeserializeEndProfile(file.ToJsonString());
        Assert.IsNotNull(extended.Source!.ExtensionData);
        Assert.AreEqual(plain.Source, extended.Source);
        Assert.AreEqual(plain.Source!.GetHashCode(), extended.Source.GetHashCode());
    }

    private static string Rewrite(DrawingFileKind kind, string json) => kind switch
    {
        DrawingFileKind.EndProfile => DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeEndProfile(json)),
        DrawingFileKind.Module => DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeModule(json)),
        DrawingFileKind.Catalogue => DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeCatalogue(json)),
        DrawingFileKind.Hall => DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeHall(json)),
        DrawingFileKind.HallPlan => DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeHallPlan(json)),
        _ => throw new NotSupportedException(kind.ToString()),
    };

    /// <summary>
    /// Adds a property to every object below <paramref name="node"/>, except points and poses: they are
    /// coordinates, written by their own converters, and have nothing to add to.
    /// </summary>
    private static int AddUnknownProperties(JsonNode node)
    {
        var added = 0;
        if (node is JsonObject item)
        {
            foreach (var child in item.Select(p => p.Value).OfType<JsonNode>().ToList()) added += AddUnknownProperties(child);
            if (!IsCoordinates(item))
            {
                item[Unknown] = new JsonObject { ["value"] = 1, ["list"] = new JsonArray("a", null) };
                added++;
            }
        }
        else if (node is JsonArray items)
        {
            foreach (var child in items.OfType<JsonNode>()) added += AddUnknownProperties(child);
        }
        return added;
    }

    private static bool IsCoordinates(JsonObject item) =>
        item.ContainsKey("x") && item.ContainsKey("y") && item.All(p => p.Key is "x" or "y" or "heading");

    /// <summary>The path of the first place where <paramref name="actual"/> lacks or changes something of <paramref name="expected"/>.</summary>
    private static string? FirstDifference(JsonNode? expected, JsonNode? actual, string path)
    {
        switch (expected)
        {
            case JsonObject item:
                if (actual is not JsonObject other) return path;
                foreach (var (name, value) in item)
                {
                    if (!other.ContainsKey(name)) return $"{path}.{name}";
                    if (FirstDifference(value, other[name], $"{path}.{name}") is { } difference) return difference;
                }
                return null;
            case JsonArray items:
                if (actual is not JsonArray others || others.Count != items.Count) return path;
                for (var i = 0; i < items.Count; i++)
                {
                    if (FirstDifference(items[i], others[i], $"{path}[{i}]") is { } difference) return difference;
                }
                return null;
            default:
                return JsonNode.DeepEquals(expected, actual) ? null : path;
        }
    }
}
