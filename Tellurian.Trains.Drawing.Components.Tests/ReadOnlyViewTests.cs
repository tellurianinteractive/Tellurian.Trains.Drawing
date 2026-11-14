using System.Text.RegularExpressions;
using Tellurian.Trains.Drawing.Components.Views;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.AutoDraw;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Halls;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Components.Tests;

/// <summary>EX-8, NF-10: the read-only views render statically, without any JS, from the model or from a file.</summary>
[TestClass]
public sealed partial class ReadOnlyViewTests
{
    private static DrawingCatalogue Catalogue => BuiltInCatalogue.Instance;

    private static Module Station()
    {
        var standard = Catalogue.FindStandard("FREMO/H0-RE")!;
        var module = new ModuleTemplate
        {
            Kind = ModuleTemplateKind.StraightAndCurve,
            Name = "Station",
            Standard = standard,
            EndProfile = Catalogue.FindEndProfile("FREMO-H0-B96", "H0")!,
            Straight = 1500,
            Radius = 2000,
            Angle = 15,
        }.Create(Catalogue).Module!;
        var variant = module.DefaultVariant;
        var publicSide = new PublicSide { Attachment = new() { SideIndex = 0, From = 100, To = 600 } };
        return module with { Variants = [variant with { PublicSides = [publicSide] }] };
    }

    private static int Count(string html, string cssClass) => ClassPattern(cssClass).Matches(html).Count;

    /// <summary>A class attribute that contains <paramref name="cssClass"/> as a whole class name.</summary>
    private static Regex ClassPattern(string cssClass) => new($"class=\"(?:[^\"]* )?{Regex.Escape(cssClass)}(?: [^\"]*)?\"");

    [TestMethod]
    public async Task EndProfileView()
    {
        var profile = Catalogue.FindEndProfile("FREMO-H0-B96", "H0")!;
        var html = await StaticRendering.RenderAsync<EndProfileView>(new Dictionary<string, object?>
        {
            [nameof(Views.EndProfileView.Profile)] = profile,
            [nameof(Views.EndProfileView.RailTopText)] = "RÖK",
        });
        Assert.AreEqual(1, Count(html, "ep-outline"));
        Assert.AreEqual(profile.Tracks.Count * 2, Count(html, "ep-rail"));
        Assert.AreEqual(profile.ScrewHoles.Count, Count(html, "ep-screw-hole"));
        Assert.AreEqual(1, Count(html, "ep-rail-top"));
        StringAssert.Contains(html, ">RÖK</text>");
        StringAssert.Contains(html, "aria-label=\"FREMO-H0-B96\"");
    }

    [TestMethod]
    public async Task EndProfileViewFromAFileWithLayersTurnedOff()
    {
        var json = DrawingFileSerializer.Serialize(Catalogue.FindEndProfile("FREMO-H0-B96", "H0")!);
        var html = await StaticRendering.RenderAsync<EndProfileView>(new Dictionary<string, object?>
        {
            [nameof(Views.EndProfileView.Json)] = json,
            [nameof(Views.EndProfileView.Layers)] = EndProfileLayers.Outline | EndProfileLayers.Tracks,
        });
        Assert.AreEqual(1, Count(html, "ep-outline"));
        Assert.AreEqual(0, Count(html, "ep-screw-hole"));
        Assert.AreEqual(0, Count(html, "ep-rail-top"));
    }

    [TestMethod]
    public async Task UnreadableFileShowsTheError()
    {
        var html = await StaticRendering.RenderAsync<EndProfileView>(new Dictionary<string, object?>
        {
            [nameof(Views.EndProfileView.Json)] = "{",
            [nameof(Views.EndProfileView.Error)] = "Kan inte visas.",
        });
        StringAssert.Contains(html, "Kan inte visas.");
        Assert.AreEqual(0, Count(html, "drawing-canvas"));
    }

    [TestMethod]
    public async Task ModuleView()
    {
        var module = Station();
        var variant = module.DefaultVariant;
        var html = await StaticRendering.RenderAsync<ModuleView>(new Dictionary<string, object?>
        {
            [nameof(Views.ModuleView.Module)] = module,
            [nameof(Views.ModuleView.EndProfiles)] = (Func<Guid, Model.EndProfiles.EndProfile?>)Catalogue.FindEndProfile,
            [nameof(Views.ModuleView.Standard)] = Catalogue.FindStandard("FREMO/H0-RE"),
        });
        Assert.AreEqual(variant.Sections.Count, Count(html, "md-section"));
        Assert.AreEqual(variant.Exits.Count, Count(html, "md-exit"));
        Assert.AreEqual(variant.Exits.Count, Count(html, "md-end-plate"));
        Assert.AreEqual(variant.Tracks.Sum(t => t.Routes().Count), Count(html, "md-track"));
        Assert.AreEqual(1, Count(html, "md-public-side"));
        StringAssert.Contains(html, ">A</text>");
        StringAssert.Contains(html, "aria-label=\"Station\"");
    }

    [TestMethod]
    public async Task ModuleViewFromAFileUsesItsEmbeddedEndProfiles()
    {
        var module = Station();
        var json = DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, Catalogue.FindEndProfile));
        var html = await StaticRendering.RenderAsync<ModuleView>(new Dictionary<string, object?>
        {
            [nameof(Views.ModuleView.Json)] = json,
            [nameof(Views.ModuleView.Layers)] = ModuleLayers.Sections | ModuleLayers.EndPlates,
        });
        Assert.AreEqual(module.DefaultVariant.Exits.Count, Count(html, "md-end-plate"));
        Assert.AreEqual(0, Count(html, "md-track"));
    }

    [TestMethod]
    public async Task HallView()
    {
        var hall = Hall.Rectangle("Club hall", 12000, 8000) with
        {
            Obstacles = [Obstacle.RoundPillar(new(4000, 4000), 400) with { Label = "P1" }],
            Doors = [new() { Position = new(new(1500, 0), new(90)), Width = 1000, Kind = DoorKind.EmergencyExit }],
            NoGoZones = [new() { Area = PathDrawing.Rectangle(new(6000, 7800), 800, 400, Angle.Zero), Reason = "Fire hose" }],
        };
        var html = await StaticRendering.RenderAsync<HallView>(new Dictionary<string, object?> { [nameof(Views.HallView.Json)] = DrawingFileSerializer.Serialize(hall) });
        Assert.AreEqual(1, Count(html, "hl-walls"));
        Assert.AreEqual(1, Count(html, "pillar"));
        Assert.AreEqual(1, Count(html, "emergency"));
        Assert.AreEqual(1, Count(html, "hl-no-go"));
        StringAssert.Contains(html, ">Fire hose</text>");
        StringAssert.Contains(html, ">P1</text>");
    }
}
