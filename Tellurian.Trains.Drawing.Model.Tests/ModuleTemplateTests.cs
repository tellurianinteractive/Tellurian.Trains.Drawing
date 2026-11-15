using Tellurian.Trains.Drawing.Model.AutoDraw;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Measures;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class ModuleTemplateTests
{
    private static DrawingCatalogue Catalogue => TestCatalogue.Instance;

    private static ModuleTemplate Template(ModuleTemplateKind kind) => new()
    {
        Kind = kind,
        Name = "New module",
        Standard = Catalogue.FindStandard(TestCatalogue.FremoId)!,
        EndProfile = TestCatalogue.FremoSingle,
        Straight = 1000,
        Radius = 2000,
        Angle = 30,
    };

    private static ModuleMeasures Measures(AutoDrawResult result) =>
        ModuleMeasurement.Derive(result.Module!.DefaultVariant, Catalogue.FindEndProfile);

    [TestMethod]
    public void StraightTemplateHasTwoExitsAndTheGivenLength()
    {
        var result = Template(ModuleTemplateKind.Straight).Create(Catalogue);
        Assert.AreEqual(AutoDrawStatus.Complete, result.Status, string.Join("; ", result.Notes));
        var variant = result.Module!.DefaultVariant;
        CollectionAssert.AreEqual(new[] { ModuleTemplate.FirstExit, ModuleTemplate.SecondExit }, variant.Exits.Select(e => e.Label).ToArray());
        Assert.IsTrue(variant.Exits.All(e => e.EndProfile.Id == TestCatalogue.FremoSingle.Id));
        Assert.AreEqual(1000, Measures(result).Length!.Value, Precision);
    }

    [TestMethod]
    public void CurveTemplateIgnoresTheStraight()
    {
        var parameters = Template(ModuleTemplateKind.Curve).ToParameters();
        Assert.IsNull(parameters.Straight);
        Assert.AreEqual(2000, parameters.Radius);
        var measures = Measures(Template(ModuleTemplateKind.Curve).Create(Catalogue));
        Assert.AreEqual(2000, measures.Radius!.Value, Precision);
        Assert.AreEqual(30, measures.Angle!.Value, Precision);
    }

    [TestMethod]
    public void StraightAndCurveTemplateHasBoth()
    {
        var parameters = Template(ModuleTemplateKind.StraightAndCurve).ToParameters();
        Assert.AreEqual((1000.0, 2000.0, 30.0), (parameters.Straight, parameters.Radius, parameters.Angle));
        Assert.IsTrue(Template(ModuleTemplateKind.StraightAndCurve).Create(Catalogue).IsDrawn);
    }

    [TestMethod]
    public void DoubleTrackProfileGivesTwoThroughTracks() =>
        Assert.AreEqual(2, (Template(ModuleTemplateKind.Straight) with { EndProfile = TestCatalogue.FremoDouble }).ToParameters().NumberOfThroughTracks);

    [TestMethod]
    public void TemplateModuleCanBeSaved()
    {
        var module = Template(ModuleTemplateKind.Curve).Create(Catalogue).Module!;
        var json = DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, Catalogue.FindEndProfile));
        Assert.AreEqual(module.Id, DrawingFileSerializer.DeserializeModule(json).Module.Id);
    }

    [TestMethod]
    public void BuiltInStandardsCanUseTheTemplates()
    {
        var catalogue = BuiltInCatalogue.Instance;
        foreach (var standard in catalogue.Standards)
        {
            var profile = catalogue.EndProfilesOf(standard.Id).FirstOrDefault(p => p.Tracks.Count == 1);
            if (profile is null) continue;
            var result = (Template(ModuleTemplateKind.Straight) with { Standard = standard, EndProfile = profile }).Create(catalogue);
            Assert.IsTrue(result.IsDrawn, $"{standard.ShortName}: {string.Join("; ", result.Notes)}");
        }
    }
}
