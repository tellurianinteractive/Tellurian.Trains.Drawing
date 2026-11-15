using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class DrawingIdentityTests
{
    [TestMethod]
    public void ReadsEndProfileDesignation()
    {
        var profile = TestProfiles.FremoSingle() with { Revision = 4 };
        var identity = DrawingIdentity.Read(DrawingFileSerializer.Serialize(profile));
        Assert.AreEqual(new DrawingIdentity(DrawingFileKind.EndProfile, profile.Id, profile.Designation, 4), identity);
    }

    [TestMethod]
    public void ReadsTheModuleInAModuleFile()
    {
        var module = TestModules.Straight() with { Revision = 2 };
        var identity = DrawingIdentity.Read(DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, TestCatalogue.Lookup)));
        Assert.AreEqual(new DrawingIdentity(DrawingFileKind.Module, module.Id, module.Name, 2), identity);
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
