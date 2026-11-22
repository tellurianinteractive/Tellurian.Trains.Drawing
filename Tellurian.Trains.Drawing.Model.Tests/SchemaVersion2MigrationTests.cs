using static Tellurian.Trains.Schedules.Model.Layouts.LayoutPartCopying;
using System.Text.Json.Nodes;
using Tellurian.Trains.Drawing.Model.HallPlans;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Modules;
using Tellurian.Trains.Drawing.Model.Tracks;
using OperationLocationKind = Tellurian.Trains.Schedules.Model.Layouts.OperationLocationKind;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>
/// Files of schema version 1 are upgraded on load (spec §6). The files in <c>SchemaVersion1/</c> are the
/// published examples as release 0.9.0 wrote them; they must not be regenerated. Version 2 files written by
/// development builds, before version 2 got the shape it has, are brought to it too.
/// </summary>
[TestClass]
public sealed class SchemaVersion2MigrationTests
{
    private const string ModuleFile = "station-3-sections.module.json";
    private const string HallPlanFile = "club-meeting.hallplan.json";
    private const string EndProfileFile = "fremo-h0-b96.endprofile.json";
    private const string HallFile = "club-hall.hall.json";

    private static readonly string[] RemovedProperties =
    [
        "registryId", "registryModuleId", "registryPersonId", "registryGroupId", "share", "west", "east",
        "fremoNumber", "owners", "stationTracks", "isLengthEntered", "isStubEntered", "variants",
    ];

    private static JsonObject Version1(string fileName)
    {
        var path = Path.Combine(RepositoryFiles.Root, "Tellurian.Trains.Drawing.Model.Tests", "SchemaVersion1", fileName);
        var file = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        Assert.AreEqual(1, (int)file["schemaVersion"]!, $"{fileName} must stay a version 1 file.");
        return file;
    }

    private static JsonObject ModuleOf(JsonObject file) => file["content"]!["module"]!.AsObject();

    private static JsonObject VariantOf(JsonObject file) => ModuleOf(file)["variants"]![0]!.AsObject();

    private static void AssertNothingOldIsWritten(string json)
    {
        foreach (var name in RemovedProperties) Assert.DoesNotContain($"\"{name}\"", json);
    }

    [TestMethod]
    public void Version1ModuleFileIsReadWithLeftAndRight()
    {
        var content = DrawingFileSerializer.DeserializeModule(Version1(ModuleFile).ToJsonString());
        var drawing = content.Module.Drawing;
        CollectionAssert.AreEqual(new[] { ExitSide.Left, ExitSide.Right }, drawing.Exits.Select(e => e.Direction).ToArray());
        CollectionAssert.AreEqual(new[] { ExitSide.Left, ExitSide.Right }, drawing.AutoDraw!.Parameters.Exits.Select(e => e.Direction).ToArray());
        CollectionAssert.AreEqual(new[] { "W", "E" }, drawing.Exits.Select(e => e.Label).ToArray(), "Exit labels are not changed.");
        Assert.IsNull(content.Module.ExternalReferences);
    }

    [TestMethod]
    public void MigratedFilesAreWrittenAsVersion2AndMatchTheSchema()
    {
        var module = DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeModule(Version1(ModuleFile).ToJsonString()));
        var hallPlan = DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeHallPlan(Version1(HallPlanFile).ToJsonString()));
        var endProfile = DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeEndProfile(Version1(EndProfileFile).ToJsonString()));
        var hall = DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeHall(Version1(HallFile).ToJsonString()));
        foreach (var (kind, json) in new[] { (DrawingFileKind.Module, module), (DrawingFileKind.HallPlan, hallPlan), (DrawingFileKind.EndProfile, endProfile), (DrawingFileKind.Hall, hall) })
        {
            Assert.AreEqual(2, DrawingFileSerializer.ReadHeader(json).SchemaVersion, kind.ToString());
            SchemaValidator.AssertValid(kind, json);
            AssertNothingOldIsWritten(json);
        }
    }

    [TestMethod]
    public void RegistryModuleIdsMoveToTheExternalReferences()
    {
        var file = Version1(ModuleFile);
        var drawing = VariantOf(file);
        drawing["registryModuleId"] = 5678;
        drawing["autoDraw"]!["parameters"]!["registryModuleId"] = 5678;

        var content = DrawingFileSerializer.DeserializeModule(file.ToJsonString());

        Assert.AreEqual("""{"moduleId":5678}""", TestReferences.RegistryEntry(content.Module.ExternalReferences));
        AssertNothingOldIsWritten(DrawingFileSerializer.Serialize(content));
    }

    [TestMethod]
    public void TheThemeOfAModuleIsDropped()
    {
        var file = Version1(ModuleFile);
        ModuleOf(file)["theme"] = "Sweden 1970s";

        var content = DrawingFileSerializer.DeserializeModule(file.ToJsonString());

        Assert.IsNull(content.Module.ExtensionData, "The theme isn't kept as an unknown property either.");
        Assert.DoesNotContain("\"theme\"", DrawingFileSerializer.Serialize(content));
    }

    [TestMethod]
    public void TheDefaultVariantBecomesTheDrawing()
    {
        var file = Version1(ModuleFile);
        var variants = ModuleOf(file)["variants"]!.AsArray();
        var first = VariantOf(file);
        var other = first.DeepClone().AsObject();
        first["isDefault"] = false;
        first["exits"] = new JsonArray();
        other["isDefault"] = true;
        other["name"] = "Long";
        other["registryModuleId"] = 5678;
        variants.Add(other);

        var content = DrawingFileSerializer.DeserializeModule(file.ToJsonString());

        Assert.HasCount(2, content.Module.Drawing.Exits, "The default variant is the one drawn.");
        Assert.AreEqual("""{"moduleId":5678}""", TestReferences.RegistryEntry(content.Module.ExternalReferences));
        var written = JsonNode.Parse(DrawingFileSerializer.Serialize(content))!["content"]!;
        var module = written["module"]!.AsObject();
        Assert.IsFalse(module.ContainsKey("variants"));
        var drawing = module["drawing"]!.AsObject();
        foreach (var name in new[] { "id", "name", "isDefault" }) Assert.IsFalse(drawing.ContainsKey(name), name);
        Assert.IsNull(content.Measures, "The measures per variant are left out; they are derived again at the next save.");
    }

    [TestMethod]
    public void ADevelopmentVersion2FileWithVariantsIsRead()
    {
        var file = JsonNode.Parse(DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeModule(Version1(ModuleFile).ToJsonString())))!.AsObject();
        var module = ModuleOf(file);
        module.Remove("drawing", out var drawing);
        drawing!["id"] = Guid.NewGuid().ToString();
        module["variants"] = new JsonArray(drawing);
        file["content"]!["measures"] = new JsonArray();

        var content = DrawingFileSerializer.DeserializeModule(file.ToJsonString());

        Assert.HasCount(2, content.Module.Drawing.Exits);
    }

    [TestMethod]
    public void AModuleWithoutARegistryModuleLeavesNoEntry()
    {
        var file = Version1(ModuleFile);
        VariantOf(file)["registryModuleId"] = null;
        Assert.IsNull(DrawingFileSerializer.DeserializeModule(file.ToJsonString()).Module.ExternalReferences);
    }

    [TestMethod]
    public void OwnersBecomeThePrefixOfTheFirstAndOneTextWithAllNames()
    {
        var file = Version1(ModuleFile);
        JsonArray Owners() =>
        [
            new JsonObject { ["name"] = "A. Owner", ["prefix"] = "AOw", ["share"] = 0.5, ["registryPersonId"] = 17 },
            new JsonObject { ["name"] = " ", ["prefix"] = "X" },
            new JsonObject { ["name"] = "Club", ["registryGroupId"] = 3 },
        ];
        ModuleOf(file)["owners"] = Owners();
        VariantOf(file)["autoDraw"]!["parameters"]!["owners"] = Owners();

        var content = DrawingFileSerializer.DeserializeModule(file.ToJsonString());

        var parameters = content.Module.Drawing.AutoDraw!.Parameters;
        Assert.AreEqual(("AOw", "A. Owner, Club"), (content.Module.Prefix, content.Module.ModuleOwners));
        Assert.AreEqual(("AOw", "A. Owner, Club"), (parameters.Prefix, parameters.ModuleOwners));
        Assert.IsNull(content.Module.ExtensionData, "The removed properties must not be kept as unknown ones.");
        Assert.IsNull(parameters.ExtensionData);
        AssertNothingOldIsWritten(DrawingFileSerializer.Serialize(content));
    }

    [TestMethod]
    public void AModuleWithoutOwnersHasNoPrefixAndNoOwnersText()
    {
        var file = Version1(ModuleFile);
        ModuleOf(file)["owners"] = new JsonArray(new JsonObject { ["name"] = "" });

        var module = DrawingFileSerializer.DeserializeModule(file.ToJsonString()).Module;

        Assert.IsNull(module.Prefix);
        Assert.IsNull(module.ModuleOwners);
    }

    [TestMethod]
    public void FremoNumberBecomesNumber()
    {
        var file = Version1(ModuleFile);
        ModuleOf(file)["fremoNumber"] = 12;
        VariantOf(file)["autoDraw"]!["parameters"]!["fremoNumber"] = 12;

        var content = DrawingFileSerializer.DeserializeModule(file.ToJsonString());

        Assert.AreEqual(12, content.Module.Number);
        Assert.AreEqual(12, content.Module.Drawing.AutoDraw!.Parameters.Number);
        AssertNothingOldIsWritten(DrawingFileSerializer.Serialize(content));
    }

    [TestMethod]
    public void TheStandardBecomesItsIdAndTheSharedStandardWithItsScale()
    {
        var file = Version1(ModuleFile);
        var old = ModuleOf(file);
        var (id, name, scale) = ((string)old["standard"]!["id"]!, (string)old["standard"]!["name"]!, (string)old["scale"]!);

        var content = DrawingFileSerializer.DeserializeModule(file.ToJsonString());

        var module = content.Module;
        Assert.AreEqual(Guid.Parse(id), module.Standard?.Id);
        Assert.AreEqual((name, scale), (module.Standard!.ShortName, module.Standard.Scale!.ShortName));
        Assert.AreEqual(scale, module.Scale?.ShortName);
        Assert.IsNull(module.Standard.ExtensionData);
        var written = ModuleOf(JsonNode.Parse(DrawingFileSerializer.Serialize(content))!.AsObject());
        Assert.IsFalse(written.ContainsKey("standardId"), "The standard carries its id.");
        Assert.IsFalse(written.ContainsKey("scale"), "The scale is written in the standard only.");
        var standard = written["standard"]!.AsObject();
        Assert.AreEqual(id, (string)standard["id"]!);
        Assert.AreEqual((name, scale), ((string)standard["shortName"]!, (string)standard["scale"]!["shortName"]!));
        CollectionAssert.AreEqual(new[] { "id", "shortName", "scale" }, standard.Select(p => p.Key).ToArray());
    }

    [TestMethod]
    public void TheTrackElementsOfTheDrawingAreCalledTrackElements()
    {
        var file = Version1(ModuleFile);
        var count = VariantOf(file)["tracks"]!.AsArray().Count;

        var content = DrawingFileSerializer.DeserializeModule(file.ToJsonString());

        Assert.HasCount(count, content.Module.Drawing.TrackElements);
        Assert.IsNull(content.Module.Drawing.ExtensionData);
        var drawing = ModuleOf(JsonNode.Parse(DrawingFileSerializer.Serialize(content))!.AsObject())["drawing"]!.AsObject();
        Assert.IsFalse(drawing.ContainsKey("tracks"));
        Assert.HasCount(count, drawing["trackElements"]!.AsArray());
    }

    [TestMethod]
    public void TheConfigurationLabelOfTheParametersIsKept()
    {
        var file = Version1(ModuleFile);
        VariantOf(file)["autoDraw"]!["parameters"]!["configurationLabel"] = "Short";

        var parameters = DrawingFileSerializer.DeserializeModule(file.ToJsonString()).Module.Drawing.AutoDraw!.Parameters;

        Assert.AreEqual("Short", parameters.ConfigurationLabel);
        Assert.IsNull(parameters.ExtensionData);
    }

    [TestMethod]
    public void PackageLabelAndOperationLocationStayWhereTheyAre()
    {
        var file = Version1(ModuleFile);
        ModuleOf(file)["packageLabel"] = "Box 1";
        ModuleOf(file)["operationLocation"] = new JsonObject { ["name"] = "Tellby", ["signature"] = "Tby", ["kind"] = "signalControlled" };

        var module = DrawingFileSerializer.DeserializeModule(file.ToJsonString()).Module;

        Assert.AreEqual("Box 1", module.PackageLabel);
        Assert.AreEqual(("Tellby", "Tby", OperationLocationKind.SignalControlled), (module.OperationLocation!.Name, module.OperationLocation.Signature, module.OperationLocation.Kind));
    }

    /// <summary>
    /// <paramref name="content"/> as a development build of version 2 wrote it before the module became that of the
    /// shared layout model: the tracks in the drawing with GUIDs as ids and their marks of being entered, the track
    /// elements in <c>tracks</c>, owners as a list, <c>fremoNumber</c>, and the standard as a reference with the scale beside it.
    /// </summary>
    private static JsonObject DevelopmentVersion2(ModuleFileContent content)
    {
        var file = JsonNode.Parse(DrawingFileSerializer.Serialize(content))!.AsObject();
        var module = ModuleOf(file);
        var drawing = module["drawing"]!.AsObject();

        var guids = new Dictionary<int, string>();
        // Version 4.0 of the shared model writes the tracks of a location module with the location.
        var tracks = module["operationLocation"] is JsonObject location && location["tracks"] is JsonArray { Count: > 0 } locationTracks
            ? location.Remove("tracks", out _) ? locationTracks : null
            : module.Remove("tracks", out var moduleTracks) ? moduleTracks : null;
        if (tracks is JsonArray trackList)
        {
            foreach (var track in trackList.Select(t => t!.AsObject()))
            {
                var id = (int)track["id"]!;
                guids.Add(id, Guid.NewGuid().ToString());
                track["id"] = guids[id];
                track["isLengthEntered"] = (bool?)track["isLengthEntered"] ?? false;
                track["isStubEntered"] = (bool?)track["isStubEntered"] ?? false;
            }
            drawing["stationTracks"] = trackList;
        }
        drawing.Remove("trackElements", out var elements);
        foreach (var element in elements!.AsArray().Select(e => e!.AsObject()))
        {
            element["id"] = Guid.NewGuid().ToString();
            if (element.ContainsKey("stationTrackId")) element["stationTrackId"] = guids[(int)element["stationTrackId"]!];
        }
        drawing["tracks"] = elements;
        // Sides referred to their exits by label.
        var exits = drawing["exits"]!.AsArray().Select(e => ((string)e!["direction"]!, (string)e["label"]!)).ToList();
        string Label(JsonNode place) => exits.Where(e => e.Item1 == (string)place["side"]!).ElementAt((int)place["number"]! - 1).Item2;
        foreach (var side in drawing["sides"]?.AsArray().Select(s => s!.AsObject()) ?? [])
        {
            side["fromExit"] = Label(side["fromExit"]!);
            side["toExit"] = Label(side["toExit"]!);
        }

        void Identification(JsonObject node)
        {
            if (node.Remove("number", out var number)) node["fremoNumber"] = number;
            node.Remove("prefix", out var prefix);
            node.Remove("moduleOwners", out var names);
            var owners = new JsonArray();
            foreach (var name in ((string?)names ?? string.Empty).Split(", ", StringSplitOptions.RemoveEmptyEntries)) owners.Add(new JsonObject { ["name"] = name });
            if (prefix is not null)
            {
                if (owners.Count == 0) owners.Add(new JsonObject { ["name"] = "" });
                owners[0]!["prefix"] = prefix;
            }
            node["owners"] = owners;
        }

        Identification(module);
        if (drawing["autoDraw"]?["parameters"] is JsonObject parameters) Identification(parameters);
        var standard = module["standard"]!.AsObject();
        module["scale"] = (string)standard["scale"]!["shortName"]!;
        module["standard"] = new JsonObject { ["id"] = standard["id"]?.DeepClone(), ["name"] = (string)standard["shortName"]! };
        return file;
    }

    [TestMethod]
    [DataRow(false, DisplayName = "Drawn by hand, with everything")]
    [DataRow(true, DisplayName = "Auto-drawn, with parameters")]
    public void ADevelopmentVersion2FileBecomesTheModuleOfTheSharedModel(bool isAutoDrawn)
    {
        var module = isAutoDrawn
            ? TestModules.Straight().With(c => { c.Number = 12; c.Prefix = "AOw"; c.ModuleOwners = "A. Owner, Club"; c.OperationLocation = new Tellurian.Trains.Schedules.Model.Layouts.Station { Name = "Tellby", Signature = "Tby" }; }).WithGeneratedTrackList()
            : TestModules.WithEverything();
        if (isAutoDrawn) module = module.With(c => c.Drawing = module.Drawing.With(edited => edited.AutoDraw = module.Drawing.AutoDraw! with { Parameters = module.Drawing.AutoDraw.Parameters with { Number = 12, Prefix = "AOw", ModuleOwners = "A. Owner, Club" } }));
        var content = ModuleFileContent.Create(module, TestCatalogue.Lookup);
        var development = DevelopmentVersion2(content);
        Assert.IsTrue(ModuleOf(development)["drawing"]!["stationTracks"]!.AsArray().Count > 0, "The test needs a module with tracks.");

        var read = DrawingFileSerializer.DeserializeModule(development.ToJsonString());

        Assert.AreEqual(DrawingFileSerializer.Serialize(content), DrawingFileSerializer.Serialize(read));
        CollectionAssert.AreEqual(Enumerable.Range(1, read.Module.Tracks.Count).ToArray(), read.Module.Tracks.Select(t => t.Id).ToArray());
        Assert.IsNull(read.Module.ExtensionData);
        Assert.IsNull(read.Module.Drawing.ExtensionData);
        Assert.IsTrue(read.Module.Tracks.All(t => t.ExtensionData is null), "The marks of being entered must not be kept as unknown properties.");
    }

    [TestMethod]
    public void TracksOfADevelopmentVersion2FileAreNumberedInTheirOrder()
    {
        var station = new TestStation();
        // Ids that aren't 1 and 2 in the order of the list: the development file loses them, being GUIDs there.
        var module = station.Module.With(c => c.Tracks = [.. station.Module.Tracks.Reverse().Select(t => t.With(e => { e.IsLengthEntered = true; e.IsStubEntered = t.Id == TestStation.Track2; }))]);
        var development = DevelopmentVersion2(ModuleFileContent.Create(module, TestCatalogue.Lookup));
        var elements = ModuleOf(development)["drawing"]!["tracks"]!.AsArray();
        elements[0]!["stationTrackId"] = Guid.NewGuid().ToString();

        var read = DrawingFileSerializer.DeserializeModule(development.ToJsonString()).Module;

        CollectionAssert.AreEqual(new[] { (1, "2"), (2, "1") }, read.Tracks.Select(t => (t.Id, t.Designation)).ToArray());
        CollectionAssert.AreEqual(new[] { (1, true, true), (2, true, false) }, read.Tracks.Select(t => (t.Id, t.IsLengthEntered, t.IsStubEntered)).ToArray(), "Track 2 is the first in the list.");
        Assert.IsNull(read.Drawing.TrackElements[0].StationTrackId, "An element that refers to a track that isn't in the list gets no track.");
        CollectionAssert.AreEqual(
            station.Drawing.TrackElements.Skip(1).Select(e => (int?)(e.StationTrackId == TestStation.Track2 ? 1 : 2)).ToArray(),
            read.Drawing.TrackElements.Skip(1).Select(e => e.StationTrackId).ToArray());
    }

    [TestMethod]
    public void AFileInTheShapeOfTodayIsReadAndWrittenBackUnchanged()
    {
        var plan = TestHalls.Plan(TestCatalogue.Instance);
        var files = new (DrawingFileKind Kind, string Json, Func<string, string> ReadAndWrite)[]
        {
            (DrawingFileKind.Module, DrawingFileSerializer.Serialize(ModuleFileContent.Create(TestModules.WithEverything(), TestCatalogue.Lookup)), json => DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeModule(json))),
            (DrawingFileKind.Module, DrawingFileSerializer.Serialize(ModuleFileContent.Create(TestModules.Junction().With(c => { c.Number = 12; c.Prefix = "AOw"; c.ModuleOwners = "A. Owner, Club"; c.ConfigurationLabel = "Short"; }), TestCatalogue.Lookup)), json => DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeModule(json))),
            (DrawingFileKind.HallPlan, DrawingFileSerializer.Serialize(plan), json => DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeHallPlan(json))),
            (DrawingFileKind.Hall, DrawingFileSerializer.Serialize(plan.Hall), json => DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeHall(json))),
            (DrawingFileKind.Catalogue, DrawingFileSerializer.Serialize(TestCatalogue.Instance), json => DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeCatalogue(json))),
            (DrawingFileKind.EndProfile, DrawingFileSerializer.Serialize(TestProfiles.Asymmetric()), json => DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeEndProfile(json))),
        };
        foreach (var (kind, json, readAndWrite) in files)
        {
            Assert.AreEqual(json, readAndWrite(json), kind.ToString());
            // The steps from version 1 are run on every version 2 file, so each must leave today's shape as it is.
            var content = JsonNode.Parse(json)!["content"]!;
            var again = DrawingFileMigrations.UpgradeDraft(kind, content.DeepClone(), DrawingFileFormat.CurrentSchemaVersion(kind));
            Assert.AreEqual(content.ToJsonString(), again.ToJsonString(), $"The steps must not change a {kind} file of today.");
        }
    }

    [TestMethod]
    public void AVersion1HallIsReadAndWrittenAsVersion2WithEnumValuesAsInTheCode()
    {
        var file = Version1(HallFile);
        Assert.AreEqual("hall", (string)file["kind"]!, "The version 1 file has its enum values in camelCase.");

        var hall = DrawingFileSerializer.DeserializeHall(file.ToJsonString());

        Assert.AreEqual((string)file["content"]!["name"]!, hall.Name);
        CollectionAssert.Contains(hall.Obstacles.Select(o => o.Kind).ToArray(), ObstacleKind.Pillar);
        CollectionAssert.Contains(hall.Doors.Select(d => d.Kind).ToArray(), DoorKind.EmergencyExit);
        var written = DrawingFileSerializer.Serialize(hall);
        Assert.AreEqual(new DrawingFileHeader(DrawingFileKind.Hall, 2, ModuleDrawVersion.Current), DrawingFileSerializer.ReadHeader(written));
        StringAssert.Contains(written, "\"kind\": \"Hall\"");
        StringAssert.Contains(written, "\"kind\": \"Pillar\"");
        StringAssert.Contains(written, "\"kind\": \"EmergencyExit\"");
        Assert.DoesNotContain("\"kind\": \"pillar\"", written);
        SchemaValidator.AssertValid(DrawingFileKind.Hall, written);
    }

    [TestMethod]
    public void EnumValuesInCamelCaseAreStillRead()
    {
        var content = ModuleFileContent.Create(TestModules.WithEverything(), TestCatalogue.Lookup);
        var json = DrawingFileSerializer.Serialize(content);
        var camelCase = json;
        foreach (var (written, earlier) in new[]
        {
            ("\"kind\": \"Module\"", "\"kind\": \"module\""),
            ("\"direction\": \"Left\"", "\"direction\": \"left\""),
            ("\"direction\": \"Right\"", "\"direction\": \"right\""),
            ("\"direction\": \"LeftToRight\"", "\"direction\": \"leftToRight\""),
            ("\"kind\": \"Station\"", "\"kind\": \"station\""),
            ("\"kind\": \"CurvedRight\"", "\"kind\": \"curvedRight\""),
            ("\"kind\": \"StationName\"", "\"kind\": \"stationName\""),
            ("\"orientation\": \"Normal\"", "\"orientation\": \"normal\""),
        })
        {
            StringAssert.Contains(camelCase, written, "Enum values are written as in the code.");
            camelCase = camelCase.Replace(written, earlier, StringComparison.Ordinal);
        }
        Assert.AreNotEqual(json, camelCase);

        Assert.AreEqual(DrawingFileKind.Module, DrawingFileSerializer.ReadHeader(camelCase).Kind);
        Assert.AreEqual(json, DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeModule(camelCase)));
    }

    [TestMethod]
    public void DesignatedTrackElementsBecomeAListOfTracks()
    {
        var file = Version1(ModuleFile);
        var elements = VariantOf(file)["tracks"]!.AsArray();
        JsonObject Straight(double x, string? designation, bool isMain) => new()
        {
            ["type"] = "straight",
            ["id"] = Guid.NewGuid().ToString(),
            ["designation"] = designation,
            ["isMainTrack"] = isMain,
            ["start"] = new JsonObject { ["x"] = x, ["y"] = 100, ["heading"] = 0 },
            ["length"] = 100,
        };
        elements.Add(Straight(0, "2", false));
        elements.Add(Straight(100, " 2 ", true));
        elements.Add(Straight(300, "3a", false));
        elements.Add(Straight(500, " ", true));
        var generated = elements.Count - 4;

        var module = DrawingFileSerializer.DeserializeModule(file.ToJsonString()).Module;
        var drawing = module.Drawing;

        CollectionAssert.AreEqual(
            new[] { (1, "2", 1, true), (2, "3a", 2, false) },
            module.Tracks.Select(t => (t.Id, t.Designation, t.DisplayOrder, t.IsMain)).ToArray());
        Assert.IsTrue(module.Tracks.All(t => t is { Length: 0, IsScheduled: false }), "Lengths are worked out by the app.");
        Assert.IsTrue(module.Tracks.All(t => !t.IsLengthEntered && !t.IsStubEntered));
        CollectionAssert.AreEqual(
            new int?[] { 1, 1, 2, null },
            drawing.TrackElements.Skip(generated).Select(t => t.StationTrackId).ToArray());
        Assert.IsTrue(drawing.TrackElements.Take(generated).All(t => t.StationTrackId is null), "Track without a designation is plain line.");
        Assert.IsTrue(drawing.TrackElements.All(t => t.ExtensionData is null), "The removed properties must not be kept as unknown ones.");
        Assert.DoesNotContain("isMainTrack", DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeModule(file.ToJsonString())));
    }

    [TestMethod]
    public void AModuleThatIsPlainLineGetsNoListOfTracks()
    {
        var module = DrawingFileSerializer.DeserializeModule(Version1(ModuleFile).ToJsonString()).Module;
        Assert.IsEmpty(module.Tracks);
        Assert.IsNotEmpty(module.Drawing.TrackElements);
    }

    [TestMethod]
    public void EndProfileRegistryIdMovesToTheExternalReferences()
    {
        var file = Version1(EndProfileFile);
        file["content"]!["registryId"] = 12;

        var profile = DrawingFileSerializer.DeserializeEndProfile(file.ToJsonString());

        Assert.AreEqual("""{"endProfileId":12}""", TestReferences.RegistryEntry(profile.ExternalReferences));
        AssertNothingOldIsWritten(DrawingFileSerializer.Serialize(profile));
    }

    [TestMethod]
    public void EmbeddedEndProfilesAreMigratedToo()
    {
        var file = Version1(ModuleFile);
        file["content"]!["endProfiles"]![0]!["registryId"] = 12;
        var content = DrawingFileSerializer.DeserializeModule(file.ToJsonString());
        Assert.AreEqual("""{"endProfileId":12}""", TestReferences.RegistryEntry(content.EndProfiles[0].ExternalReferences));
    }

    [TestMethod]
    public void CatalogueRegistryIdsMoveToTheExternalReferences()
    {
        var file = JsonNode.Parse(DrawingFileSerializer.Serialize(TestCatalogue.Instance))!.AsObject();
        file["schemaVersion"] = 1;
        file["content"]!["standards"]![0]!["registryId"] = 3;
        file["content"]!["endProfiles"]![0]!["registryId"] = 12;

        var catalogue = DrawingFileSerializer.DeserializeCatalogue(file.ToJsonString());

        Assert.AreEqual("""{"standardId":3}""", TestReferences.RegistryEntry(catalogue.Standards[0].ExternalReferences));
        Assert.AreEqual("""{"endProfileId":12}""", TestReferences.RegistryEntry(catalogue.EndProfiles[0].ExternalReferences));
        Assert.IsNull(catalogue.Standards[1].ExternalReferences);
    }

    [TestMethod]
    public void HallPlanRefersToExitsBySideAndNumberAndItsModulesAreTurnedOrNot()
    {
        var plan = DrawingFileSerializer.DeserializeHallPlan(Version1(HallPlanFile).ToJsonString());

        var layout = plan.Layouts[0];
        var main = layout.Lines[0].Entries;
        CollectionAssert.AreEqual(new[] { false, true, false }, main.Select(e => e.IsTurned).ToArray(), "The curve was entered at its east exit.");
        Assert.IsTrue(main.All(e => e.ContinuesFrom is null), "Each line continues from the first exit on the other side.");
        Assert.AreEqual(new ExitReference(plan.Modules[2].Id!.Value, ExitSide.Right, 2), layout.Lines[1].StartAt);
        Assert.IsFalse(layout.Lines[1].Entries[0].IsTurned, "The terminus is entered at its only exit, on the left.");
        Assert.AreEqual(ExitPlace.Left, layout.AnchorDimensions!.ReferencePoint.Exit);
        Assert.AreEqual((ExitPlace.Right, ExitPlace.Right), (plan.Joints[0].First.Place, plan.Joints[0].Second.Place));
        var written = DrawingFileSerializer.Serialize(plan);
        foreach (var old in new[] { "entryExit", "exitExit", "exitLabel", "variantId" }) Assert.DoesNotContain($"\"{old}\"", written);
        Assert.AreEqual(written, DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeHallPlan(written)), "A version 2 plan is read as it is.");
    }

    [TestMethod]
    public void HallPlanRefersToObstaclesByTheirNewNumbers()
    {
        var file = Version1(HallPlanFile);
        var content = file["content"]!;
        var dimensions = content["layouts"]![0]!["anchorDimensions"]!;
        dimensions["first"] = new JsonObject { ["kind"] = "obstacle", ["obstacleId"] = Guid.NewGuid().ToString() };
        dimensions["second"]!["obstacleId"] = (string)content["hall"]!["obstacles"]![1]!["id"]!;

        var plan = DrawingFileSerializer.DeserializeHallPlan(file.ToJsonString());

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, plan.Hall.Obstacles.Select(o => o.Id).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 2 }, plan.Hall.Doors.Select(d => d.Id).ToArray());
        var anchor = plan.Layouts[0].AnchorDimensions!;
        Assert.AreEqual(2, anchor.Second.ObstacleId);
        Assert.AreSame(plan.Hall.Obstacles[1], plan.Hall.FindObstacle(anchor.Second.ObstacleId!.Value));
        Assert.IsNull(anchor.First.ObstacleId, "A reference to an obstacle the hall doesn't have refers to none, as before.");
        var written = DrawingFileSerializer.Serialize(plan);
        Assert.AreEqual(written, DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeHallPlan(written)), "A version 2 plan is read as it is.");
    }

    [TestMethod]
    public void HallPlanMigratesItsEmbeddedModulesEndProfilesAndStandards()
    {
        var file = Version1(HallPlanFile);
        var content = file["content"]!;
        var module = content["modules"]![0]!;
        var drawing = module["variants"]![0]!;
        drawing["registryModuleId"] = 5678;
        module["owners"] = new JsonArray(new JsonObject { ["name"] = "A. Owner", ["share"] = 1 });
        content["endProfiles"]![0]!["registryId"] = 12;
        content["standards"]![0]!["registryId"] = 3;

        var plan = DrawingFileSerializer.DeserializeHallPlan(file.ToJsonString());

        var directions = plan.Modules.SelectMany(m => m.Drawing.Exits).Select(e => e.Direction).Distinct().ToList();
        CollectionAssert.AreEquivalent(new[] { ExitSide.Left, ExitSide.Right }, directions);
        Assert.AreEqual("""{"moduleId":5678}""", TestReferences.RegistryEntry(plan.Modules[0].ExternalReferences));
        Assert.AreEqual("A. Owner", plan.Modules[0].ModuleOwners);
        Assert.IsNull(plan.Modules[0].Prefix);
        Assert.IsTrue(plan.Modules.All(m => m.Standard is { Scale: not null, Id: not null }), "Every embedded module has the standard of the shared model.");
        Assert.AreEqual("""{"endProfileId":12}""", TestReferences.RegistryEntry(plan.EndProfiles[0].ExternalReferences));
        Assert.AreEqual("""{"standardId":3}""", TestReferences.RegistryEntry(plan.Standards[0].ExternalReferences));
        AssertNothingOldIsWritten(DrawingFileSerializer.Serialize(plan));
    }

    [TestMethod]
    public void AVersion1FileWithAnotherShapeIsRefusedNotCrashed()
    {
        var file = Version1(ModuleFile);
        file["content"]!["module"] = 5;
        var exception = Assert.ThrowsExactly<DrawingFileException>(() => DrawingFileSerializer.DeserializeModule(file.ToJsonString()));
        Assert.AreEqual(DrawingFileError.InvalidContent, exception.Error);
    }

    [TestMethod]
    public void TheSampleCatalogueIsAVersion2File()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(SampleCatalogue.Json));
        Assert.AreEqual(DrawingFileFormat.CurrentSchemaVersion(DrawingFileKind.Catalogue), DrawingFileSerializer.ReadHeader(stream).SchemaVersion);
    }
}
