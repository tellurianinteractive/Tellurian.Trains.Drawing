using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.AutoDraw;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class ModuleRedrawTests
{
    private static readonly EndProfile Asymmetric = TestProfiles.Asymmetric() with { Id = Guid.Parse("30000000-0000-0000-0000-000000000001"), Designation = "TEST-ASYM" };

    /// <summary>A single track 40 mm left of the plate's centre.</summary>
    private static readonly EndProfile OffCentre = EndProfileTemplate.Create(new()
    {
        Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
        Designation = "TEST-OFF-CENTRE",
        Scale = "H0",
        PlateWidth = 500,
        PlateTop = -40,
        PlateBottom = -140,
        Gauge = TestProfiles.H0Gauge,
        TrackOffsets = [40],
    });

    private static readonly DrawingCatalogue Catalogue = TestCatalogue.Instance.With(Asymmetric).With(OffCentre);

    /// <summary>A 30° left curve of radius 2000 mm with <paramref name="profile"/> at both ends.</summary>
    private static ModuleFileContent Curve(EndProfile profile, bool isEndProfileSwitcher = false)
    {
        AutoDrawExit a = new() { Label = "A", Direction = ExitDirection.West, EndProfile = profile.Designation, EndProfileId = profile.Id };
        var module = ModuleAutoDraw.Create(new()
        {
            Name = "Kurvan",
            Standard = "FREMO/H0-RE",
            Radius = 2000,
            Angle = 30,
            CurveDirection = CurveDirection.Left,
            IsEndProfileSwitcher = isEndProfileSwitcher,
            Exits = [a, a with { Label = "B", Direction = ExitDirection.East }],
        }, Catalogue).Module!;
        return ModuleFileContent.Create(module, Catalogue.FindEndProfile);
    }

    /// <summary>The module with one joint across the middle of the track, bent <paramref name="bend"/> mm along the track at its middle.</summary>
    private static ModuleFileContent WithJoint(ModuleFileContent content, double bend = 0)
    {
        var variant = content.Module.DefaultVariant;
        var outline = ModuleGeometry.Outline(variant, content.FindEndProfile)!;
        var main = variant.MainPath!.ToPath();
        var joint = ModuleSections.AcrossAt(outline, variant.MainPath!, main.Length / 2)!;
        if (bend != 0)
        {
            var pose = main.PoseAt(main.Length / 2);
            joint = joint with { Bends = [pose.Position + pose.Left * 100 + pose.Forward * bend] };
        }
        var cut = ModuleSections.WithJoints(variant, outline, [joint])!;
        return content with { Module = content.Module with { Variants = [cut] } };
    }

    private static (double Along, double Offset) TrackRelative(ModuleFileContent content, Point point)
    {
        var projection = content.Module.DefaultVariant.MainPath!.ToPath().Project(point);
        return (projection.Distance, (point - projection.Pose.Position).Dot(projection.Pose.Left));
    }

    [TestMethod]
    public void FlippingAnEndTurnsItsPlateAndTheEndewenderFollows()
    {
        var content = Curve(Asymmetric);
        Assert.IsFalse(content.Module.IsEndProfileSwitcher);

        var flipped = ModuleRedraw.WithEndFlipped(content, "B")!;

        Assert.AreNotEqual(content.Module.DefaultVariant.Exit("B")!.Orientation, flipped.Module.DefaultVariant.Exit("B")!.Orientation);
        Assert.AreEqual(content.Module.DefaultVariant.Exit("A")!.Orientation, flipped.Module.DefaultVariant.Exit("A")!.Orientation);
        Assert.IsTrue(flipped.Module.IsEndProfileSwitcher, "Both plates the same side out: the high side changes side of the track.");
        Assert.IsFalse(ModuleRedraw.WithEndFlipped(flipped, "B")!.Module.IsEndProfileSwitcher);
    }

    [TestMethod]
    public void OnlyAsymmetricEndProfilesCanBeFlipped()
    {
        Assert.IsTrue(ModuleRedraw.CanFlip(Asymmetric));
        Assert.IsFalse(ModuleRedraw.CanFlip(TestCatalogue.FremoSingle));
        Assert.IsFalse(ModuleRedraw.IsEndProfileSwitcher(Curve(TestCatalogue.FremoSingle).Module.DefaultVariant.Exits, Catalogue.FindEndProfile));
    }

    [TestMethod]
    public void FlippingTheCurveMirrorsTheModuleAndItsJoints()
    {
        var content = WithJoint(Curve(TestCatalogue.FremoSingle), bend: 30);
        var bend = content.Module.DefaultVariant.InternalJoints.Single().Bends!.Single();

        var flipped = ModuleRedraw.WithCurveFlipped(content, Catalogue)!;

        Assert.AreEqual(CurveDirection.Right, flipped.Module.DefaultVariant.AutoDraw!.Parameters.CurveDirection);
        var before = TrackRelative(content, bend);
        var after = TrackRelative(flipped, flipped.Module.DefaultVariant.InternalJoints.Single().Bends!.Single());
        Assert.AreEqual(before.Along, after.Along, TestTolerance.Precision);
        Assert.AreEqual(-before.Offset, after.Offset, TestTolerance.Precision);
        Assert.HasCount(2, flipped.Module.DefaultVariant.Sections);
        Assert.AreEqual("Kurvan", flipped.Module.Name);
        Assert.AreEqual(content.Module.Id, flipped.Module.Id);
    }

    [TestMethod]
    public void StraightModulesHaveNoCurveToFlip()
    {
        AutoDrawExit a = new() { Label = "A", Direction = ExitDirection.West, EndProfile = TestCatalogue.FremoSingle.Designation };
        var straight = ModuleAutoDraw.Create(new() { Standard = "FREMO/H0-RE", Straight = 1000, Exits = [a, a with { Label = "B", Direction = ExitDirection.East }] }, Catalogue).Module!;
        var content = ModuleFileContent.Create(straight, Catalogue.FindEndProfile);

        Assert.IsFalse(ModuleRedraw.HasCurve(content));
        Assert.IsNull(ModuleRedraw.WithCurveFlipped(content, Catalogue));
    }

    [TestMethod]
    public void ChangingAnEndProfileRedrawsTheModuleAndKeepsWhatWasDone()
    {
        var content = WithJoint(Curve(Asymmetric));
        content = ModuleRedraw.WithEndFlipped(content, "B")!;
        content = content with { Module = content.Module with { Name = "Ny kurva", FremoNumber = 12 } };

        var changed = ModuleRedraw.WithEndProfile(content, "A", TestCatalogue.Mmm480, Catalogue)!;

        var variant = changed.Module.DefaultVariant;
        Assert.AreEqual(TestCatalogue.Mmm480.Id, variant.Exit("A")!.EndProfile.Id);
        Assert.AreEqual(content.Module.DefaultVariant.Exit("B")!.Orientation, variant.Exit("B")!.Orientation, "The other end keeps its orientation.");
        Assert.AreEqual(("Ny kurva", 12), (changed.Module.Name, changed.Module.FremoNumber));
        Assert.HasCount(1, variant.InternalJoints);
        Assert.IsNotNull(changed.FindEndProfile(TestCatalogue.Mmm480.Id), "The new end profile is embedded.");
        Assert.AreEqual(TestCatalogue.Mmm480.Id, variant.AutoDraw!.Parameters.Exits.Single(e => e.Label == "A").EndProfileId);
    }

    [TestMethod]
    public void BendsChangeOnlyWithTracksOffTheCentre()
    {
        Assert.IsFalse(ModuleRedraw.BendsWillChange(WithJoint(Curve(OffCentre))), "No bends.");
        Assert.IsFalse(ModuleRedraw.BendsWillChange(WithJoint(Curve(TestCatalogue.FremoSingle), bend: 30)), "Centred track.");
        Assert.IsFalse(ModuleRedraw.BendsWillChange(WithJoint(Curve(TestCatalogue.FremoDouble), bend: 30)), "Centred double track.");
        Assert.IsTrue(ModuleRedraw.BendsWillChange(WithJoint(Curve(OffCentre), bend: 30)));
        Assert.IsTrue(ModuleRedraw.BendsWillChange(WithJoint(Curve(TestCatalogue.FremoSingle), bend: 30), OffCentre), "The new end profile counts too.");
    }
}
