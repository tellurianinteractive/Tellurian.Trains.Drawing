using static Tellurian.Trains.Schedules.Model.Layouts.LayoutPartCopying;
using System.Text.RegularExpressions;
using Tellurian.Trains.Drawing.Components.Views;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.AutoDraw;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.Files;
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
        var drawing = module.Drawing;
        var publicSide = new PublicSide { Attachment = new() { SideIndex = 0, From = 100, To = 600 } };
        return module.With(c => c.Drawing = drawing with { PublicSides = [publicSide] });
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
        Assert.AreEqual(profile.Tracks.Count, Count(html, "ep-sleeper"));
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
        var drawing = module.Drawing;
        var html = await StaticRendering.RenderAsync<ModuleView>(new Dictionary<string, object?>
        {
            [nameof(Views.ModuleView.Module)] = module,
            [nameof(Views.ModuleView.EndProfiles)] = (Func<Guid, EndProfile?>)Catalogue.FindEndProfile,
            [nameof(Views.ModuleView.Standard)] = Catalogue.FindStandard("FREMO/H0-RE"),
        });
        Assert.AreEqual(drawing.Sections.Count, Count(html, "md-section"));
        // The end profile's short name is drawn at each exit; exit labels aren't.
        Assert.AreEqual(drawing.Exits.Count, Count(html, "md-profile-name"));
        StringAssert.Contains(html, ">B96</text>");
        Assert.AreEqual(0, Count(html, "md-exit-label"));
        // Every exit's end profile is shown by shading inside the module (ModuleDrawing §9.1).
        Assert.IsGreaterThanOrEqualTo(drawing.Exits.Count, Count(html, "md-profile"));
        Assert.AreEqual(drawing.Exits.Count, Count(html, "md-end-plate"));
        Assert.AreEqual(drawing.TrackElements.Sum(t => t.Routes().Count), Count(html, "md-track"));
        Assert.AreEqual(1, Count(html, "md-public-side"));
        StringAssert.Contains(html, "aria-label=\"Station\"");
    }

    [TestMethod]
    public async Task ModuleViewWritesTheDesignationsOfTheStationTracks()
    {
        var station = Station().With(c => c.OperationLocation = new Tellurian.Trains.Schedules.Model.Layouts.Station { Name = "Tellby", Signature = "Tby" });
        station = Tellurian.Trains.Drawing.Model.Tracks.StationTrackList.WithGeneratedTrackList(station);
        station = station.With(c => c.Tracks = [.. station.Tracks.Select(t => t.With(r => r.Designation = "1a"))]);
        var parameters = new Dictionary<string, object?> { [nameof(Views.ModuleView.Module)] = station };

        var html = await StaticRendering.RenderAsync<ModuleView>(parameters);
        var hidden = await StaticRendering.RenderAsync<ModuleView>(new Dictionary<string, object?>(parameters) { [nameof(Views.ModuleView.Layers)] = ModuleLayers.All & ~ModuleLayers.TrackDesignations });

        Assert.AreEqual(station.Tracks.Count, Count(html, "md-track-designation"));
        StringAssert.Contains(html, ">1a</text>");
        Assert.AreEqual(0, Count(hidden, "md-track-designation"));
        Assert.AreEqual(0, Count(await StaticRendering.RenderAsync<ModuleView>(new Dictionary<string, object?> { [nameof(Views.ModuleView.Module)] = Station() }), "md-track-designation"), "Plain line has no station tracks.");
    }

    [TestMethod]
    public async Task ModuleViewWritesTheFremoDesignationWithoutTheOwnerOnTheModule()
    {
        var module = Station().With(c => { c.Number = 7900; c.Prefix = "SFjä"; c.ModuleOwners = "Stefan Fjällemark"; });
        var html = await StaticRendering.RenderAsync<ModuleView>(new Dictionary<string, object?>
        {
            [nameof(Views.ModuleView.Module)] = module,
            [nameof(Views.ModuleView.EndProfiles)] = (Func<Guid, EndProfile?>)Catalogue.FindEndProfile,
        });
        StringAssert.Contains(html, ">SFjä 7900</text>");
        Assert.DoesNotContain(">Stefan Fjällemark</text>", html, "The prefix says who owns the module.");
        Assert.AreEqual(1, Count(html, "md-module-name"));
        Assert.AreEqual(0, Count(html, "md-module-owner"));
    }

    [TestMethod]
    public async Task ModuleViewWritesTheOwnersUnderANameWithoutPrefix()
    {
        var owned = await StaticRendering.RenderAsync<ModuleView>(new Dictionary<string, object?> { [nameof(Views.ModuleView.Module)] = Station().With(c => c.ModuleOwners = "Module Club") });
        StringAssert.Contains(owned, ">Module Club</text>");
    }

    [TestMethod]
    public async Task ModuleViewWritesTheNameWithoutOwner()
    {
        var html = await StaticRendering.RenderAsync<ModuleView>(new Dictionary<string, object?> { [nameof(Views.ModuleView.Module)] = Station() });
        StringAssert.Contains(html, ">Station</text>");
        Assert.AreEqual(0, Count(html, "md-module-owner"));
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
        Assert.AreEqual(module.Drawing.Exits.Count, Count(html, "md-end-plate"));
        Assert.AreEqual(0, Count(html, "md-track"));
    }

    [TestMethod]
    public async Task ModuleViewRendersAPrintSheetOnlyWithAPrintTitle()
    {
        var module = Station();
        var plain = await StaticRendering.RenderAsync<ModuleView>(new Dictionary<string, object?>
        {
            [nameof(Views.ModuleView.Module)] = module,
            [nameof(Views.ModuleView.EndProfiles)] = (Func<Guid, EndProfile?>)Catalogue.FindEndProfile,
        });
        Assert.AreEqual(0, Count(plain, "print-sheet"));

        var printable = await StaticRendering.RenderAsync<ModuleView>(new Dictionary<string, object?>
        {
            [nameof(Views.ModuleView.Module)] = module,
            [nameof(Views.ModuleView.EndProfiles)] = (Func<Guid, EndProfile?>)Catalogue.FindEndProfile,
            [nameof(Views.ModuleView.PrintTitle)] = "Station Nord",
        });
        Assert.AreEqual(1, Count(printable, "print-sheet"));
        Assert.AreEqual(1, Count(printable, "md-print-title"));
        StringAssert.Contains(printable, ">Station Nord</text>");
        // The drawing is in both the canvas and the sheet.
        Assert.AreEqual(2 * module.Drawing.Sections.Count, Count(printable, "md-section"));
    }

    // The app passes the current catalogue: a short name set after the module was drawn shows, although the
    // module file embeds the profile as it was.
    [TestMethod]
    public async Task ModuleViewFromAFilePrefersTheGivenEndProfiles()
    {
        var module = Station();
        var json = DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, Catalogue.FindEndProfile));
        var html = await StaticRendering.RenderAsync<ModuleView>(new Dictionary<string, object?>
        {
            [nameof(Views.ModuleView.Json)] = json,
            [nameof(Views.ModuleView.EndProfiles)] = (Func<Guid, EndProfile?>)(id => Catalogue.FindEndProfile(id) is { } p ? p.With(edited => { edited.ShortName = "NEW"; }) : null),
        });
        StringAssert.Contains(html, ">NEW</text>");
    }

    [TestMethod]
    public async Task DimensionLineShowsTheValueOrItsText()
    {
        var dimension = Dimension.Place(new(0, 0), new(1234.4, 0), new(600, 100));
        var html = await StaticRendering.RenderAsync<Rendering.DimensionLine>(new Dictionary<string, object?> { [nameof(Rendering.DimensionLine.Dimension)] = dimension });
        Assert.AreEqual(2, Count(html, "dimension-extension"));
        Assert.AreEqual(2, Count(html, "dimension-tick"));
        StringAssert.Contains(html, ">1234</text>");

        var labelled = await StaticRendering.RenderAsync<Rendering.DimensionLine>(new Dictionary<string, object?> { [nameof(Rendering.DimensionLine.Dimension)] = dimension with { Text = "c/c" } });
        StringAssert.Contains(labelled, ">c/c</text>");
    }

    [TestMethod]
    public void DimensionValuesAreWrittenInTheUiCulture()
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en");
            Assert.AreEqual("45.3", Rendering.DimensionLine.Value(45.26));
            Assert.AreEqual("1234", Rendering.DimensionLine.Value(1234.4));
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("sv");
            Assert.AreEqual("45,3", Rendering.DimensionLine.Value(45.26));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }
    }

    [TestMethod]
    public async Task HallView()
    {
        var hall = Hall.Rectangle("Club hall", 12000, 8000).With(h =>
        {
            h.Add(Obstacle.RoundPillar(new(4000, 4000), 400).With(o => o.Label = "P1"));
            h.Add(new Door { Position = new(new(1500, 0), new(90)), Width = 1000, Kind = DoorKind.EmergencyExit });
            h.Add(new Door { Position = new(new(6000, 0), new(90)), Width = 2000, IsModuleEntrance = true });
            h.Add(new NoGoZone { Area = PathDrawing.Rectangle(new(6000, 7800), 800, 400, Angle.Zero), Reason = "Fire hose" });
        });
        var html = await StaticRendering.RenderAsync<HallView>(new Dictionary<string, object?> { [nameof(Views.HallView.Json)] = DrawingFileSerializer.Serialize(hall) });
        Assert.AreEqual(1, Count(html, "hl-walls"));
        Assert.AreEqual(1, Count(html, "pillar"));
        Assert.AreEqual(1, Count(html, "hl-keep-free emergency"), "One of the two doors is an emergency exit.");
        Assert.AreEqual(1, Count(html, "hl-door emergency"));
        Assert.AreEqual(1, Count(html, "emergency-exit-sign"));
        Assert.AreEqual(1, Count(html, "hl-module-entrance"));
        Assert.AreEqual(1, Count(html, "hl-no-go"));
        StringAssert.Contains(html, ">Fire hose</text>");
        StringAssert.Contains(html, ">P1</text>");
    }
}
