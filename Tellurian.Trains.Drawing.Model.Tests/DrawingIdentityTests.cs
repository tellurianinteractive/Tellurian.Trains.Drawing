using static Tellurian.Trains.Schedules.Model.Layouts.LayoutPartCopying;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class DrawingIdentityTests
{
    [TestMethod]
    public void ReadsEndProfileDesignation()
    {
        var profile = TestProfiles.FremoSingle().With(edited => { edited.Revision = 4; });
        var identity = DrawingIdentity.Read(DrawingFileSerializer.Serialize(profile));
        Assert.AreEqual(new DrawingIdentity(DrawingFileKind.EndProfile, profile.Id, profile.Designation, 4), identity);
    }

    [TestMethod]
    public void ReadsTheModuleInAModuleFile()
    {
        var module = TestModules.Straight().With(c => c.Revision = 2);
        var identity = DrawingIdentity.Read(DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, TestCatalogue.Lookup)));
        Assert.AreEqual(new DrawingIdentity(DrawingFileKind.Module, module.Id, module.Name, 2), identity);
    }

    [TestMethod]
    public void ModuleNameIsTheDesignationOfAModuleThatIsNoOperationLocation()
    {
        var module = TestModules.Straight().With(c => { c.Name = "Tellby"; c.Number = 12; c.Prefix = "SFjä"; });
        var identity = DrawingIdentity.Read(DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, TestCatalogue.Lookup)));
        Assert.AreEqual("SFjä 0012", identity.Name);
        Assert.AreEqual(module.Title, identity.Name);
    }

    [TestMethod]
    public void AnOperationLocationIsNamedByTheDesignationAndTheLocation()
    {
        var module = TestModules.Straight().With(c => { c.Name = "Tellby station"; c.Number = 12; c.Prefix = "SFjä"; c.OperationLocation = new Tellurian.Trains.Schedules.Model.Layouts.Station { Name = "Tellby", Signature = "Tby" }; });
        var identity = DrawingIdentity.Read(DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, TestCatalogue.Lookup)));
        Assert.AreEqual("SFjä 0012 Tellby", identity.Name);
        Assert.AreEqual(module.Title, identity.Name);
    }

    [TestMethod]
    public void ModuleWithoutANameIsNamedByItsFremoDesignation()
    {
        var module = TestModules.Straight().With(c => { c.Name = ""; c.Number = 1234; c.Prefix = "SFjä"; });
        var identity = DrawingIdentity.Read(DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, TestCatalogue.Lookup)));
        Assert.AreEqual("SFjä 1234", identity.Name);
    }

    [TestMethod]
    public void AModuleFileThatIsNotUpgradedYetIsNamedByItsFirstOwnersPrefixAndFremoNumber()
    {
        const string json = """
            { "format": "tellurian-drawing", "kind": "module", "schemaVersion": 1,
              "content": { "module": { "id": "40000000-0000-0000-0000-000000000001", "name": "Tellby", "fremoNumber": 12, "revision": 3,
                "owners": [ { "name": "S. F.", "prefix": "SFjä" }, { "name": "Club", "prefix": "MC" } ] } } }
            """;
        Assert.AreEqual(new DrawingIdentity(DrawingFileKind.Module, Guid.Parse("40000000-0000-0000-0000-000000000001"), "SFjä 0012", 3), DrawingIdentity.Read(json));
    }

    [TestMethod]
    public void AModuleWithoutANumberIsNamedWithItsConfiguration()
    {
        var module = TestModules.Straight().With(c => { c.Name = "Tellby"; c.ConfigurationLabel = "Short"; });
        var identity = DrawingIdentity.Read(DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, TestCatalogue.Lookup)));
        Assert.AreEqual(module.Title, identity.Name);
        Assert.AreEqual("Tellby short", identity.Name);
    }

    [TestMethod]
    public void ReadsHallsAndHallPlans()
    {
        var plan = TestHalls.Plan(TestCatalogue.Instance);
        Assert.AreEqual(new DrawingIdentity(DrawingFileKind.Hall, plan.Hall.Id, plan.Hall.Name, 0), DrawingIdentity.Read(DrawingFileSerializer.Serialize(plan.Hall)));
        Assert.AreEqual(new DrawingIdentity(DrawingFileKind.HallPlan, plan.Id, plan.Name, 0), DrawingIdentity.Read(DrawingFileSerializer.Serialize(plan)));
    }

    [TestMethod]
    public void CatalogueHasNoId()
    {
        var identity = DrawingIdentity.Read(DrawingFileSerializer.Serialize(TestCatalogue.Instance with { Version = "1.2" }));
        Assert.IsNull(identity.Id);
        Assert.AreEqual("1.2", identity.Name);
    }

    [TestMethod]
    public void InvalidIdIsReported()
    {
        var profile = TestProfiles.FremoSingle();
        var json = DrawingFileSerializer.Serialize(profile).Replace(profile.Id.ToString(), "not-a-guid", StringComparison.Ordinal);
        Assert.AreEqual(DrawingFileError.InvalidContent, Assert.ThrowsExactly<DrawingFileException>(() => DrawingIdentity.Read(json)).Error);
    }
}
