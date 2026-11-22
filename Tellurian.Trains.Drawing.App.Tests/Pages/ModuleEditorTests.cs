using static Tellurian.Trains.Schedules.Model.Layouts.LayoutPartCopying;
using Tellurian.Trains.Drawing.App.Catalogue;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Documents;
using Tellurian.Trains.Drawing.App.Editors;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Drawing.Components.Canvas;
using Tellurian.Trains.Schedules.Model.Geometry;
using Tellurian.Trains.Drawing.Model.Tracks;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class ModuleEditorTests
{
    private const double Length = 2000;

    /// <summary>A straight module that is a station, with the station track of its through track; or plain line.</summary>
    private static IRenderedComponent<DrawingPage> OpenStraightModule(AppTestContext context, bool isStation = true) =>
        OpenModule(context, isStation, straight: Length);

    /// <summary>A curve of 30° with radius 2000 mm, plain line.</summary>
    private static IRenderedComponent<DrawingPage> OpenCurveModule(AppTestContext context) =>
        OpenModule(context, isStation: false, straight: 0, radius: 2000, angle: 30);

    private static IRenderedComponent<DrawingPage> OpenModule(AppTestContext context, bool isStation, double straight, double? radius = null, double? angle = null)
    {
        var catalogue = BuiltInCatalogue.Instance;
        AutoDrawExit west = new() { Label = "W", Direction = ExitSide.Left, EndProfile = "FREMO-H0-B96" };
        var module = ModuleAutoDraw.Create(new() { Name = "Tellby", Standard = "FREMO/H0-RE", Straight = straight, Radius = radius, Angle = angle, Exits = [west, west with { Label = "E", Direction = ExitSide.Right }] }, catalogue).Module!;
        // The editor gives a station without station tracks the list of its generated tracks, measured, when it is opened.
        if (isStation) module = module.With(c => c.OperationLocation = Tellby);
        var json = DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, catalogue.FindEndProfile));
        var draft = context.Services.GetRequiredService<DrawingDocuments>().CreateAsync(json).Result;
        var page = context.Render<DrawingPage>(p => p.Add(x => x.Id, draft.Id));
        page.WaitForElement(".sections-pane");
        return page;
    }

    private static Tellurian.Trains.Schedules.Model.Layouts.Station Tellby => new() { Name = "Tellby", Signature = "Tby" };

    [TestMethod]
    public void APlainLineModuleHasNoStationTracksAndNeedsNone()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context, isStation: false);

        Assert.IsEmpty(page.FindComponent<ModuleEditor>().Instance.Current!.Module.Tracks);
        Assert.IsEmpty(page.FindAll(".track-problems"));
    }

    private static Point OnTrack(IRenderedComponent<DrawingPage> page, double distance) =>
        page.FindComponent<ModuleEditor>().Instance.Current!.Module.Drawing.MainPath!.ToPath().PoseAt(distance).Position;

    /// <summary>A click in the drawing, as the canvas script reports it: in module millimetres.</summary>
    private static void Click(IRenderedComponent<DrawingPage> page, Point at)
    {
        var canvas = page.FindComponent<DrawingCanvas>();
        canvas.InvokeAsync(() => canvas.Instance.PointerDown(at.X, at.Y, 0, 0, 0, 1, 0, "mouse")).Wait();
        canvas.InvokeAsync(() => canvas.Instance.PointerUp(at.X, at.Y, 0, 0, 0, 0, 0, "mouse")).Wait();
    }

    [TestMethod]
    public void EndProfilesChangedInTheCatalogueAreUpdatedWhenTheModuleOpens()
    {
        using var context = new AppTestContext();
        var catalogue = BuiltInCatalogue.Instance;
        AutoDrawExit west = new() { Label = "W", Direction = ExitSide.Left, EndProfile = "FREMO-H0-B96" };
        var module = ModuleAutoDraw.Create(new() { Name = "Tellby", Standard = "FREMO/H0-RE", Straight = Length, Exits = [west, west with { Label = "E", Direction = ExitSide.Right }] }, catalogue).Module!;
        var json = DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, catalogue.FindEndProfile));
        var documents = context.Services.GetRequiredService<DrawingDocuments>();
        var draft = documents.CreateAsync(json).Result;
        var profile = DrawingFileSerializer.DeserializeModule(json).EndProfiles.Single();
        context.Services.GetRequiredService<CatalogueService>().SaveEndProfileAsync(profile.With(edited => { edited.ShortName = "CHANGED"; })).Wait();

        var page = context.Render<DrawingPage>(p => p.Add(x => x.Id, draft.Id));

        page.WaitForElement("#updated-end-profiles");
        StringAssert.Contains(page.Find("#updated-end-profiles").TextContent, profile.Designation);
        var stored = context.Workbench.Drafts[draft.Id];
        Assert.IsTrue(stored.HasUnsavedChanges, "Saving the module puts the new end profile in its file.");
        Assert.AreEqual("CHANGED", DrawingFileSerializer.DeserializeModule(stored.Json).FindEndProfile(profile.Id)?.ShortName);
    }

    [TestMethod]
    public void AnEndCanBeFlippedAndTheEndewenderFollows()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);
        var editor = page.FindComponent<ModuleEditor>().Instance;
        var before = editor.Current!.Module.IsEndProfileSwitcher;

        page.Find("#flip-end-E").Click();

        Assert.AreNotEqual(before, editor.Current!.Module.IsEndProfileSwitcher);
        Assert.AreEqual(!before, page.Find("#endewender").HasAttribute("checked"));
        page.Find("#undo").Click();
        Assert.AreEqual(before, editor.Current!.Module.IsEndProfileSwitcher, "Flipping can be undone.");
    }

    [TestMethod]
    public void AnotherEndProfileRedrawsTheModule()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);
        var editor = page.FindComponent<ModuleEditor>().Instance;
        var other = BuiltInCatalogue.Instance.EndProfiles.First(p => p.Scale == "H0" && p.Id != editor.Current!.Module.Drawing.Exit("W")!.EndProfile.Id);

        page.Find("#end-profile-W").Change(other.Id.ToString());

        Assert.AreEqual(other.Id, editor.Current!.Module.Drawing.Exit("W")!.EndProfile.Id);
        Assert.AreEqual("Tellby", editor.Current.Module.Name);
        Assert.IsEmpty(page.FindAll("#flip-curve"), "A straight module has no curve to flip.");
    }

    [TestMethod]
    public void AModuleHasItsThroughTrackInTheListAndMoreCanBeAdded()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);
        var editor = page.FindComponent<ModuleEditor>().Instance;

        var through = editor.Current!.Module.Tracks.Single();
        Assert.AreEqual(("1", true, true, Length / 1000, false), (through.Designation, through.IsMain, through.IsScheduled, through.Length, through.IsStub), "The through track runs from end to end.");
        Assert.AreEqual("2 m", page.Find(".track-length").TextContent);
        Assert.IsEmpty(page.FindAll(".track-problems"));

        page.Find("#add-track").Click();

        var added = editor.Current!.Module.Tracks[1];
        Assert.AreEqual(("2", true, 0d), (added.Designation, added.IsScheduled, added.Length));
        Assert.AreEqual("Track 2", page.Find("#track-title").TextContent);
        StringAssert.Contains(page.Find(".track-problems").TextContent, "Track 2 has no track elements");

        Assert.AreEqual(2, added.Id, "A new track gets the lowest id that no track has.");
        page.Find(".element-track").Change(added.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));

        Assert.AreEqual(Length / 1000, editor.Current!.Module.Tracks[1].Length, "The element now gives the added track its length.");
        StringAssert.Contains(page.Find(".track-problems").TextContent, "Track 1 has no track elements");

        page.Find("#undo").Click();
        Assert.AreEqual(through.Id, editor.Current!.Module.Drawing.TrackElements[0].StationTrackId, "Each change can be undone.");
    }

    [TestMethod]
    public void AStationWithoutStationTracksGetsItsListWhenOpened()
    {
        using var context = new AppTestContext();
        var catalogue = BuiltInCatalogue.Instance;
        AutoDrawExit west = new() { Label = "W", Direction = ExitSide.Left, EndProfile = "FREMO-H0-B96" };
        var module = ModuleAutoDraw.Create(new() { Name = "Tellby", Standard = "FREMO/H0-RE", Straight = Length, Exits = [west, west with { Label = "E", Direction = ExitSide.Right }] }, catalogue).Module!;
        var drawing = module.Drawing.With(edited => edited.TrackElements = [.. module.Drawing.TrackElements.Select(e => e.With(edited => edited.StationTrackId = null))]);
        var json = DrawingFileSerializer.Serialize(ModuleFileContent.Create(module.With(c => { c.Tracks = []; c.Drawing = drawing; c.OperationLocation = Tellby; }), catalogue.FindEndProfile));
        var draft = context.Services.GetRequiredService<DrawingDocuments>().CreateAsync(json).Result;

        var page = context.Render<DrawingPage>(p => p.Add(x => x.Id, draft.Id));
        page.WaitForElement(".sections-pane");

        var track = page.FindComponent<ModuleEditor>().Instance.Current!.Module.Tracks.Single();
        Assert.AreEqual(("1", Length / 1000), (track.Designation, track.Length));
        Assert.IsEmpty(page.FindAll(".track-problems"));
    }

    [TestMethod]
    public void TrackPropertiesAreEnteredInThePane()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);
        var editor = page.FindComponent<ModuleEditor>().Instance;
        page.Find(".track-link").Click();
        Tellurian.Trains.Schedules.Model.Layouts.StationTrack Track() => editor.Current!.Module.Tracks.Single();

        page.Find("#track-direction").Change(nameof(Tellurian.Trains.Schedules.Model.Layouts.TrackDirection.RightToLeft));
        page.Find("#track-main").Change(true);
        page.Find("#track-lockable").Change(true);
        page.Find("#track-scheduled").Change(false);

        Assert.AreEqual((Tellurian.Trains.Schedules.Model.Layouts.TrackDirection.RightToLeft, true, true, false), (Track().Direction, Track().IsMain, Track().IsLockable, Track().IsScheduled));
        CollectionAssert.AreEqual(
            new[] { "Both directions", "From W to E", "From E to W" },
            page.FindAll("#track-direction option").Select(o => o.TextContent).ToArray(),
            "Directions are offered by the names of the module's ends.");
    }

    [TestMethod]
    public void LengthAndStubFollowTheDrawingUntilTheUserEntersThem()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);
        var editor = page.FindComponent<ModuleEditor>().Instance;
        page.Find(".track-link").Click();
        Tellurian.Trains.Schedules.Model.Layouts.StationTrack Track() => editor.Current!.Module.Tracks.Single();
        Assert.IsTrue(page.Find("#track-stub").HasAttribute("disabled"));

        page.Find("#track-length-drawn").Change(false);
        page.Find("#track-stub-drawn").Change(false);
        page.Find("#track-stub").Change(true);

        Assert.IsTrue(Track() is { IsLengthEntered: true, IsStubEntered: true });
        Assert.AreEqual((Length / 1000, true), (Track().Length, Track().IsStub));

        page.Find("#track-stub-drawn").Change(true);
        Assert.IsFalse(Track().IsStubEntered);
        Assert.IsFalse(Track().IsStub);
    }

    [TestMethod]
    public void ARemovedTrackLeavesItsElementInTheDrawing()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);
        var editor = page.FindComponent<ModuleEditor>().Instance;
        page.Find(".track-link").Click();
        var elements = editor.Current!.Module.Drawing.TrackElements.Count;

        page.Find("#remove-track").Click();

        StringAssert.Contains(page.Find(".track-problems").TextContent, "An operation location has at least one");

        Assert.IsEmpty(editor.Current!.Module.Tracks);
        Assert.HasCount(elements, editor.Current.Module.Drawing.TrackElements);
        Assert.IsNull(editor.Current.Module.Drawing.TrackElements[0].StationTrackId);
        Assert.IsTrue(page.Find("#remove-track").HasAttribute("disabled"));
    }

    [TestMethod]
    public void TracksAreStoredInTheDraftWithTheirLengths()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);
        var editor = page.FindComponent<ModuleEditor>();
        page.Find(".track-link").Click();
        page.Find("#track-lockable").Change(true);

        Assert.IsTrue(editor.InvokeAsync(editor.Instance.FlushAsync).Result);

        var stored = DrawingFileSerializer.DeserializeModule(context.Workbench.Drafts.Values.Single().Json).Module;
        Assert.IsTrue(stored.Tracks.Single().IsLockable);
        Assert.AreEqual(Length / 1000, stored.Tracks.Single().Length);
        Assert.AreEqual(stored.Tracks.Single().Id, stored.Drawing.TrackElements[0].StationTrackId);
    }

    [TestMethod]
    public void AModuleIsMadeAnOperationLocationAndPlainLineAgain()
    {
        using var context = new AppTestContext();
        context.JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);
        var page = OpenStraightModule(context, isStation: false);
        var editor = page.FindComponent<ModuleEditor>().Instance;
        Assert.IsNull(editor.Current!.Module.OperationLocation, "A module is plain line until it is made a location.");
        Assert.IsNotNull(page.Find("#no-location"));

        page.Find("#add-location").Click();

        var location = editor.Current!.Module.OperationLocation!;
        Assert.AreEqual(("Tellby", Tellurian.Trains.Schedules.Model.Layouts.OperationLocationKind.Station, true, true), (location.Name, location.Kind, location.HasPassengerExchange, location.HasCargoExchange));
        StringAssert.Contains(page.Find(".location-problems").TextContent, "no signature");

        var fields = page.FindAll(".location-pane input.text-field-input");
        fields[1].Change(" Tby ");
        page.FindAll(".location-pane input.text-field-input")[2].Change("se");
        page.Find("#location-centre-middle").Change(false);

        location = editor.Current!.Module.OperationLocation!;
        Assert.AreEqual(("Tby", "SE", Length / 2000), (location.Signature, location.CountryCode, location.Centre));
        Assert.IsEmpty(page.FindAll(".location-problems"));

        page.Find("#undo").Click();
        Assert.IsNull(editor.Current!.Module.OperationLocation!.Centre, "Each change can be undone.");

        page.Find("#remove-location").Click();
        Assert.IsNull(editor.Current!.Module.OperationLocation);
        Assert.HasCount(1, editor.Current.Module.Tracks, "The tracks stay.");
    }

    [TestMethod]
    public void ChangingTheKindOfALocationAsksBeforeItClearsWhatTheKindCannotHave()
    {
        using var context = new AppTestContext();
        var confirm = context.JSInterop.Setup<bool>("confirm", _ => true);
        confirm.SetResult(false);
        var page = OpenStraightModule(context, isStation: false);
        var editor = page.FindComponent<ModuleEditor>().Instance;
        page.Find("#add-location").Click();
        const string Halt = nameof(Tellurian.Trains.Schedules.Model.Layouts.OperationLocationKind.Other);

        page.Find("#location-kind").Change(Halt);

        Assert.AreEqual(Tellurian.Trains.Schedules.Model.Layouts.OperationLocationKind.Station, editor.Current!.Module.OperationLocation!.Kind, "Declined: nothing changes.");
        StringAssert.Contains(confirm.Invocations.Single().Arguments[0]!.ToString(), "cargo");

        var accepted = context.JSInterop.Setup<bool>("confirm", _ => true);
        accepted.SetResult(true);
        page.Find("#location-kind").Change(Halt);

        var location = editor.Current!.Module.OperationLocation!;
        Assert.AreEqual((Tellurian.Trains.Schedules.Model.Layouts.OperationLocationKind.Other, false, true), (location.Kind, location.HasCargoExchange, location.HasPassengerExchange));
        Assert.IsTrue(page.Find("#location-cargo").HasAttribute("disabled"), "A halt can't have cargo.");
        Assert.IsEmpty(page.FindAll(".location-problems").Where(p => !p.TextContent.Contains("signature")));
    }

    [TestMethod]
    public void EveryModuleEditorPaneHasItsOwnHelp()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);

        Layout.HelpTests.AssertPaneHelp(page, ("archive", "Archive"), ("moduleeditor", "Edit"), ("moduleends", "Ends"), ("modulesections", "Sections"), ("moduletracks", "Tracks"), ("modulelocation", "Operation location"), ("moduleproperties", "Properties"));
    }

    [TestMethod]
    public void ModuleOpensInTheEditorAsOneSection()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);

        Assert.IsNotNull(page.FindComponent<ModuleEditor>());
        Assert.HasCount(1, page.FindAll(".module-sections tbody tr"));
        Assert.AreEqual("2000 mm", page.Find("#section-length-1").TextContent);
        Assert.IsNotNull(page.Find(".module-properties"), "The identification is edited beside the drawing.");
    }

    [TestMethod]
    public void ClickWithTheJointToolDividesTheModule()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);

        page.Find("#tool-joint").Click();
        Click(page, OnTrack(page, 700));

        Assert.HasCount(2, page.FindAll(".module-sections tbody tr"));
        Assert.AreEqual("700 mm", page.Find("#section-length-1").TextContent);
        Assert.AreEqual("1300 mm", page.Find("#section-length-2").TextContent);
    }

    [TestMethod]
    public void SelectedJointsPositionIsTypedExactly()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);
        page.Find("#tool-joint").Click();
        Click(page, OnTrack(page, 700));
        page.Find("#tool-select").Click();

        Click(page, OnTrack(page, 700));
        Assert.AreEqual("Joint between sections 1 and 2", page.Find("#joint-title").TextContent);
        var distance = page.Find(".joint-position .number-field-input");
        distance.Input("1234");
        distance.Change("1234");

        Assert.AreEqual("1234 mm", page.Find("#section-length-1").TextContent);
        Assert.HasCount(2, page.FindAll(".joint-points tbody tr"), "A straight joint has its two ends.");
    }

    [TestMethod]
    public async Task SectionsAreSavedToTheDraft()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);
        page.Find("#tool-joint").Click();
        Click(page, OnTrack(page, 500));

        var editor = page.FindComponent<ModuleEditor>();
        await page.InvokeAsync(() => editor.Instance.FlushAsync());

        var drawing = DrawingFileSerializer.DeserializeModule(context.Workbench.Drafts.Values.Single().Json).Module.Drawing;
        Assert.HasCount(2, drawing.Sections);
        Assert.AreEqual(("1", "2"), (drawing.InternalJoints[0].FirstSection, drawing.InternalJoints[0].SecondSection));
    }

    private static void Type(AngleSharp.Dom.IElement field, string text)
    {
        field.Input(text);
        field.Change(text);
    }

    [TestMethod]
    public void TheModuleIsDividedIntoSectionsOfTheSameLength()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);

        Type(page.Find(".section-count .number-field-input"), "4");

        Assert.HasCount(4, page.FindAll(".module-sections tbody tr"));
        Assert.IsTrue(Enumerable.Range(1, 4).All(i => page.Find($"#section-length-{i}").TextContent == "500 mm"));
    }

    [TestMethod]
    public void SectionsAreMadeEqualAgain()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);
        page.Find("#tool-joint").Click();
        Click(page, OnTrack(page, 700));

        page.Find("#equal-sections").Click();

        Assert.AreEqual(("1000 mm", "1000 mm"), (page.Find("#section-length-1").TextContent, page.Find("#section-length-2").TextContent));
    }

    [TestMethod]
    public void AJointOnACurveIsTypedAsAnAngle()
    {
        using var context = new AppTestContext();
        var page = OpenCurveModule(context);
        Type(page.Find(".section-count .number-field-input"), "2");
        page.Find("#tool-select").Click();
        Click(page, OnTrack(page, 2000 * Math.PI / 12));
        var fields = page.FindAll(".joint-position .number-field-input");
        Assert.HasCount(2, fields, "Distance and angle.");
        Assert.AreEqual("15", fields[1].GetAttribute("value"));

        Type(fields[1], "10");

        Assert.AreEqual($"{Math.Round(2000 * Math.PI / 18)} mm", page.Find("#section-length-1").TextContent);
    }

    [TestMethod]
    public void AJointOnAStraightModuleHasNoAngle()
    {
        using var context = new AppTestContext();
        var page = OpenStraightModule(context);
        Type(page.Find(".section-count .number-field-input"), "2");
        page.Find("#tool-select").Click();

        Click(page, OnTrack(page, 1000));

        Assert.HasCount(1, page.FindAll(".joint-position .number-field-input"));
    }
}
