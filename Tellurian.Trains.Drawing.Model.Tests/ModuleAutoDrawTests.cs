using Tellurian.Trains.Drawing.Model.Modules;
using Tellurian.Trains.Drawing.Model.Tracks;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class ModuleAutoDrawTests
{
    private const string Fremo = "FREMO/H0-RE";

    private static readonly AutoDrawExit West = new() { Label = "W", Direction = ExitSide.Left, EndProfile = "FREMO-H0-B96" };
    private static readonly AutoDrawExit East = new() { Label = "E", Direction = ExitSide.Right, EndProfile = "FREMO-H0-B96" };

    private static AutoDrawParameters Parameters(double? straight = null, double? radius = null, double? angle = null, params AutoDrawExit[] exits) =>
        new() { Name = "Test", Standard = Fremo, Straight = straight, Radius = radius, Angle = angle, Exits = exits };

    private static AutoDrawResult Draw(AutoDrawParameters parameters)
    {
        var result = ModuleAutoDraw.Create(parameters, TestCatalogue.Instance);
        if (result.Module is not null) AssertConsistent(result.Module.Drawing);
        return result;
    }

    private static ModuleDrawing Drawing(AutoDrawResult result)
    {
        Assert.IsNotNull(result.Module, string.Join(" ", result.Notes));
        return result.Module.Drawing;
    }

    /// <summary>Checks that hold for every generated drawing.</summary>
    private static void AssertConsistent(ModuleDrawing drawing)
    {
        var outline = ModuleGeometry.Outline(drawing, TestCatalogue.Lookup)!.Tessellate(0.01);
        Assert.IsTrue(outline.IsCounterClockwise, "The outline must be counter-clockwise.");
        var sectionArea = drawing.Sections.Sum(s => s.Outline.Tessellate(0.01).SignedArea);
        Assert.AreEqual(outline.SignedArea, sectionArea, outline.SignedArea * RelativeArea, "The sections must cover the outline.");
        Assert.HasCount(Math.Max(0, drawing.Sections.Count - 1), drawing.InternalJoints);
        var areas = drawing.Sections.Select(s => s.Outline).Concat(drawing.OperatorSpaces.Select(o => o.Area)).Concat(drawing.DuckUnderAreas.Select(d => d.Area));
        Assert.IsTrue(areas.All(a => a.Tessellate().IsCounterClockwise), "All areas must be counter-clockwise.");

        // MD-7: generated tracks start and end on the end profile track positions of their exits.
        var trackPositions = drawing.Exits.SelectMany(e => ModuleGeometry.TrackPositions(e, TestCatalogue.Lookup(e.EndProfile.Id)!)).ToList();
        foreach (var track in drawing.TrackElements.OfType<FlexTrack>())
        {
            var start = track.ConnectionPoints()[0].Position;
            Assert.IsTrue(trackPositions.Any(p => p.Position.IsCloseTo(start, Tolerance.Length)), $"Track start {start} is not on an exit track position.");
        }
    }

    // ModuleDrawing §11, example 1.
    [TestMethod]
    public void StraightLineModule()
    {
        var result = Draw(Parameters(straight: 1000, exits: [West, East]));
        Assert.AreEqual(AutoDrawStatus.Complete, result.Status);
        Assert.IsEmpty(result.Notes);
        var drawing = Drawing(result);
        Assert.AreEqual(ModuleKind.PassThrough, drawing.Kind);
        Assert.IsTrue(drawing.Exits[0].Pose.IsCloseTo(new(Point.Origin, Angle.Half)));
        Assert.IsTrue(drawing.Exits[1].Pose.IsCloseTo(new(new(1000, 0), Angle.Zero)));
        Assert.AreEqual(1000 * 500, ModuleGeometry.Outline(drawing, TestCatalogue.Lookup)!.Tessellate().SignedArea, Tolerance.Length);
        var track = (FlexTrack)drawing.TrackElements.Single();
        Assert.AreEqual(1000, track.TrackLength, Precision);
        Assert.IsTrue(track.IsGenerated);
        Assert.HasCount(1, drawing.Sections);
        Assert.IsEmpty(drawing.OperatorSpaces);
        Assert.IsEmpty(drawing.DuckUnderAreas);
    }

    // Example 2.
    [TestMethod]
    public void CurveWithInferredExits()
    {
        var result = Draw(Parameters(radius: 2000, angle: 30));
        Assert.AreEqual(AutoDrawStatus.Complete, result.Status);
        Assert.AreEqual(AutoDrawNoteCode.ExitsInferred, result.Notes.Single().Code);
        StringAssert.Contains(result.Notes[0].Message, "2 × FREMO-H0-B96");
        var drawing = Drawing(result);
        CollectionAssert.AreEqual(new[] { "W", "E" }, drawing.Exits.Select(e => e.Label).ToArray());
        Assert.AreEqual(2000 * Math.PI / 6, drawing.MainPath!.ToPath().Length, Precision);
        Assert.AreEqual(1047, Math.Round(drawing.MainPath.ToPath().Length));
        Assert.IsTrue(drawing.Exits[1].Pose.IsCloseTo(new(new(1000, 2000 - 2000 * Math.Cos(Math.PI / 6)), new Angle(30))));
        Assert.IsTrue(drawing.Sides.All(s => s.Path.Steps.Single() is ArcStep), "Curve sides are single concentric arcs.");
    }

    // Example 3.
    [TestMethod]
    public void StraightPlusCurve()
    {
        var drawing = Drawing(Draw(Parameters(500, 2500, 15, West, East)));
        Assert.AreEqual(1154, Math.Round(drawing.MainPath!.ToPath().Length));
        Assert.IsInstanceOfType<LineStep>(drawing.MainPath.Steps[0]);
    }

    [TestMethod]
    [DataRow(StraightPosition.Before, 0)]
    [DataRow(StraightPosition.After, 1)]
    public void StraightPositionOrdersThePath(StraightPosition position, int straightIndex)
    {
        var drawing = Drawing(Draw(Parameters(500, 2500, 15, West, East) with { StraightPosition = position }));
        Assert.IsInstanceOfType<LineStep>(drawing.MainPath!.Steps[straightIndex]);
    }

    [TestMethod]
    public void SplitStraightGoesOnBothSides()
    {
        var steps = Drawing(Draw(Parameters(500, 2500, 15, West, East) with { StraightPosition = StraightPosition.Split })).MainPath!.Steps;
        Assert.HasCount(3, steps);
        Assert.IsInstanceOfType<ArcStep>(steps[1]);
    }

    [TestMethod]
    public void RightCurveTurnsRight()
    {
        var exit = Drawing(Draw(Parameters(radius: 2000, angle: 30) with { CurveDirection = CurveDirection.Right })).Exits[1];
        Assert.IsLessThan(0, exit.Pose.Position.Y);
        Assert.AreEqual(-30, exit.Pose.Heading.Signed.Degrees, Precision);
    }

    // Example 4.
    [TestMethod]
    public void DoubleTrackTransitionWithReverseCurves()
    {
        var result = Draw(Parameters(straight: 1000, exits:
        [
            new() { Label = "W", Direction = ExitSide.Left, EndProfile = "FREMO-H0-2E96" },
            new() { Label = "E", Direction = ExitSide.Right, EndProfile = "TEST-2-55" },
        ]));
        Assert.AreEqual(AutoDrawStatus.Complete, result.Status);
        var tracks = Drawing(result).TrackElements.Cast<FlexTrack>().ToList();
        Assert.HasCount(2, tracks);
        var ends = tracks.Select(t => t.ConnectionPoints()[1].Position).OrderBy(p => p.Y).ToList();
        Assert.IsTrue(ends[0].IsCloseTo(new(1000, -27.5), Tolerance.Length));
        Assert.IsTrue(ends[1].IsCloseTo(new(1000, 27.5), Tolerance.Length));
        Assert.IsTrue(tracks.All(t => t.Path.ToPath().IsTangentContinuous()));
        Assert.IsTrue(tracks.All(t => t.ConnectionPoints()[1].Heading.IsCloseTo(Angle.Zero)));
    }

    [TestMethod]
    public void ReverseCurvesOnACurveAreLeftForDrawingByHand()
    {
        var result = Draw(Parameters(radius: 2000, angle: 30, exits:
        [
            new() { Label = "W", Direction = ExitSide.Left, EndProfile = "FREMO-H0-2E96" },
            new() { Label = "E", Direction = ExitSide.Right, EndProfile = "TEST-2-55" },
        ]));
        Assert.AreEqual(AutoDrawStatus.NeedsManualCompletion, result.Status);
        Assert.AreEqual(2, result.Notes.Count(n => n.Code == AutoDrawNoteCode.TrackNotGenerated));
        Assert.IsEmpty(Drawing(result).TrackElements);
    }

    [TestMethod]
    public void SingleToDoubleTransitionContinuesOneTrack()
    {
        var result = Draw(Parameters(straight: 1000, exits: [West, new() { Label = "E", Direction = ExitSide.Right, EndProfile = "FREMO-H0-2E96" }]));
        Assert.AreEqual(AutoDrawStatus.NeedsManualCompletion, result.Status);
        Assert.AreEqual(1, result.Notes.Count(n => n.Code == AutoDrawNoteCode.TrackNotGenerated));
        Assert.HasCount(1, Drawing(result).TrackElements);
    }

    // Example 5.
    [TestMethod]
    public void TerminusWithBufferStops()
    {
        var result = Draw(Parameters(straight: 1200, exits: [new() { Label = "A", EndProfile = "FREMO-H0-2E96" }]) with { IsEndOfLine = true });
        Assert.AreEqual(AutoDrawStatus.Complete, result.Status);
        var drawing = Drawing(result);
        Assert.AreEqual(ModuleKind.EndOfLine, drawing.Kind);
        Assert.IsTrue(result.Module!.IsEndOfLine);
        Assert.HasCount(1, drawing.Sides);
        Assert.AreEqual(1200 * 500, ModuleGeometry.Outline(drawing, TestCatalogue.Lookup)!.Tessellate().SignedArea, Tolerance.Length);
        var tracks = drawing.TrackElements.OfType<FlexTrack>().ToList();
        var stops = drawing.TrackElements.OfType<BufferStop>().ToList();
        Assert.HasCount(2, tracks);
        Assert.HasCount(2, stops);
        Assert.IsTrue(tracks.All(t => Math.Abs(t.TrackLength - (1200 - ModuleAutoDraw.BufferStopMargin)) < Precision));
        Assert.IsTrue(stops.All(s => s.Pose.Heading.IsCloseTo(Angle.Half)));
    }

    // Example 6.
    [TestMethod]
    public void StationWithWidenedBodyOperatorSpaceAndSections()
    {
        var result = Draw(Parameters(straight: 3000, exits: [West, East]) with { Width = 600, IsOperationPlace = true, NumberOfSections = 3 });
        Assert.AreEqual(AutoDrawStatus.Complete, result.Status);
        var drawing = Drawing(result);
        var outline = ModuleGeometry.Outline(drawing, TestCatalogue.Lookup)!.Tessellate();
        // 500 wide at the plates, 600 in the middle, widening at 45° on the left (non-operator) side.
        Assert.AreEqual(600, outline.Bounds.Height, Precision);
        Assert.AreEqual(-250, outline.Bounds.Min.Y, Precision);
        Assert.AreEqual(3000 * 500 + 100 * (3000 - 200) + 2 * 100 * 100 / 2.0, outline.SignedArea, Tolerance.Length);

        Assert.HasCount(3, drawing.Sections);
        Assert.HasCount(2, drawing.InternalJoints);
        Assert.IsTrue(drawing.InternalJoints[0].Start.IsCloseTo(new(1000, -250)));
        Assert.IsTrue(drawing.InternalJoints[0].End.IsCloseTo(new(1000, 350)));

        var space = drawing.OperatorSpaces.Single();
        Assert.AreEqual(0, space.Attachment!.SideIndex);
        Assert.AreEqual(800, space.Depth);
        var area = space.Area.Tessellate();
        Assert.AreEqual(3000 * 800, area.SignedArea, Tolerance.Length);
        Assert.AreEqual(-1050, area.Bounds.Min.Y, Precision);
    }

    [TestMethod]
    public void WideningToTheRight()
    {
        var drawing = Drawing(Draw(Parameters(straight: 3000, exits: [West, East]) with { Width = 600, WideningSide = WideningSide.Right }));
        var bounds = ModuleGeometry.Outline(drawing, TestCatalogue.Lookup)!.Bounds;
        Assert.AreEqual(-350, bounds.Min.Y, Precision);
        Assert.AreEqual(250, bounds.Max.Y, Precision);
    }

    [TestMethod]
    public void ShortModuleCanNotWidenFully()
    {
        var result = Draw(Parameters(straight: 150, exits: [West, East]) with { Width = 600 });
        Assert.IsTrue(result.Notes.Any(n => n.Code == AutoDrawNoteCode.WideningShortened));
        Assert.AreEqual(250 + 250 + 75, ModuleGeometry.Outline(Drawing(result), TestCatalogue.Lookup)!.Bounds.Height, Precision);
    }

    [TestMethod]
    public void WidenedTerminusKeepsItsWidthToTheBackEdge()
    {
        var drawing = Drawing(Draw(Parameters(straight: 1200, exits: [new() { Label = "A", EndProfile = "FREMO-H0-B96" }]) with { IsEndOfLine = true, Width = 700 }));
        var outline = ModuleGeometry.Outline(drawing, TestCatalogue.Lookup)!.Tessellate();
        Assert.AreEqual(700, outline.Bounds.Height, Precision);
        Assert.AreEqual(1200 * 500 + 200 * (1200 - 200) + 200 * 200 / 2.0, outline.SignedArea, Tolerance.Length);
    }

    // Example 7.
    [TestMethod]
    public void JunctionPlacesExtraExitsProvisionally()
    {
        var result = Draw(Parameters(straight: 1500, exits: [West, East, East with { Label = "E2" }]));
        Assert.AreEqual(AutoDrawStatus.NeedsManualCompletion, result.Status);
        Assert.IsTrue(result.Notes.Any(n => n.Code == AutoDrawNoteCode.JunctionNeedsCompletion));
        var drawing = Drawing(result);
        Assert.AreEqual(ModuleKind.Junction, drawing.Kind);
        Assert.HasCount(3, drawing.Sides);
        var e2 = drawing.Exit("E2")!;
        Assert.IsTrue(e2.IsPositionProvisional);
        Assert.IsTrue(e2.Pose.IsCloseTo(new(new(1500, 600), Angle.Zero)));
        Assert.IsFalse(drawing.Exit("E")!.IsPositionProvisional);
        Assert.HasCount(1, drawing.TrackElements);
    }

    [TestMethod]
    public void JunctionWithLeftExtrasAndOperationPlace()
    {
        var result = Draw(Parameters(straight: 1500, exits: [West, West with { Label = "W2" }, East]) with { IsOperationPlace = true, NumberOfSections = 2, IsDuckunder = true });
        var drawing = Drawing(result);
        CollectionAssert.AreEqual(new[] { "W", "E", "W2" }, drawing.Exits.Select(e => e.Label).ToArray());
        Assert.IsTrue(result.Notes.Any(n => n.Code == AutoDrawNoteCode.SectionsNotGenerated));
        Assert.AreEqual(1500 * 800, drawing.OperatorSpaces.Single().Area.Tessellate().SignedArea, Tolerance.Length);
        Assert.HasCount(1, drawing.DuckUnderAreas);
    }

    [TestMethod]
    public void NoGeometryNeedsManualDrawing()
    {
        var result = Draw(Parameters(exits: [West, East]));
        Assert.AreEqual(AutoDrawStatus.NeedsManualDrawing, result.Status);
        Assert.IsFalse(result.IsDrawn);
        Assert.AreEqual(AutoDrawNoteCode.NoGeometry, result.Notes.Single().Code);
    }

    [TestMethod]
    public void UnknownStandardNeedsManualDrawing()
    {
        var result = Draw(Parameters(straight: 1000) with { Standard = "AmericaN" });
        Assert.AreEqual(AutoDrawStatus.NeedsManualDrawing, result.Status);
        Assert.AreEqual(AutoDrawNoteCode.UnknownStandard, result.Notes.Single().Code);
    }

    [TestMethod]
    public void AngleWithoutRadiusUsesDefaultRadius()
    {
        var result = Draw(Parameters(angle: 30, exits: [West, East]));
        Assert.AreEqual(AutoDrawNoteCode.DefaultRadiusUsed, result.Notes.Single().Code);
        Assert.AreEqual(2000 * Math.PI / 6, Drawing(result).MainPath!.ToPath().Length, Precision);
    }

    [TestMethod]
    public void AngleWithoutRadiusAndWithoutDefaultNeedsManualDrawing()
    {
        var result = Draw(Parameters(angle: 30) with { Standard = "MMM-2R" });
        Assert.AreEqual(AutoDrawStatus.NeedsManualDrawing, result.Status);
        Assert.IsTrue(result.Notes.Any(n => n.Code == AutoDrawNoteCode.NoGeometry));
    }

    [TestMethod]
    public void WidthBelowPlateIsIgnored()
    {
        var result = Draw(Parameters(straight: 1000, exits: [West, East]) with { Width = 100 });
        Assert.AreEqual(AutoDrawNoteCode.WidthBelowPlateIgnored, result.Notes.Single().Code);
        Assert.AreEqual(500, ModuleGeometry.Outline(Drawing(result), TestCatalogue.Lookup)!.Bounds.Height, Precision);
    }

    [TestMethod]
    public void InternalExitsBecomeSectionJoints()
    {
        var result = Draw(Parameters(straight: 2000, exits: [West, new() { Label = "I", EndProfile = "internal" }, East]) with { NumberOfSections = 1 });
        Assert.IsTrue(result.Notes.Any(n => n.Code == AutoDrawNoteCode.InternalExitsBecameJoints));
        var drawing = Drawing(result);
        Assert.AreEqual(ModuleKind.PassThrough, drawing.Kind);
        Assert.HasCount(2, drawing.Sections);
        Assert.IsTrue(drawing.InternalJoints.Single().Start.IsCloseTo(new(1000, -250)));
    }

    [TestMethod]
    public void SummerStandardMapsToMmmWithAddOn()
    {
        var result = Draw(Parameters(straight: 1000) with { Standard = "MMM-2R/SSM" });
        Assert.IsTrue(result.Notes.Any(n => n.Code == AutoDrawNoteCode.StandardMapped));
        var module = result.Module!;
        Assert.AreEqual("MMM-2R", module.Standard!.ShortName);
        Assert.AreEqual(TestCatalogue.Instance.FindStandard("MMM-2R")!.Id, module.Standard.Id);
        Assert.AreEqual("H0", module.Scale?.ShortName);
        Assert.AreEqual("MMM-Summer", module.AddOns.Single().Name);
        Assert.IsTrue(module.Drawing.Exits.All(e => e.EndProfile.Name == "MMM480"));
    }

    [TestMethod]
    public void UnknownEndProfileFallsBackToDefault()
    {
        var result = Draw(Parameters(straight: 1000, exits: [West with { EndProfile = "FREMO-H0-XYZ" }, East]));
        Assert.AreEqual(AutoDrawNoteCode.EndProfileNotFound, result.Notes.Single().Code);
        Assert.AreEqual("FREMO-H0-B96", Drawing(result).Exits[0].EndProfile.Name);
    }

    [TestMethod]
    public void EndProfileIdTakesPrecedence()
    {
        var drawing = Drawing(Draw(Parameters(straight: 1000, exits: [West with { EndProfileId = TestCatalogue.FremoDouble.Id }, East with { EndProfileId = TestCatalogue.FremoDouble.Id }])));
        Assert.IsTrue(drawing.Exits.All(e => e.EndProfile.Id == TestCatalogue.FremoDouble.Id));
        Assert.HasCount(2, drawing.TrackElements);
    }

    [TestMethod]
    public void MissingDefaultEndProfileNeedsManualDrawing()
    {
        var result = Draw(Parameters(straight: 1000) with { Standard = "MMM-2R", NumberOfThroughTracks = 2 });
        Assert.AreEqual(AutoDrawStatus.NeedsManualDrawing, result.Status);
        Assert.AreEqual(AutoDrawNoteCode.MissingDefaultEndProfile, result.Notes.Single().Code);
    }

    [TestMethod]
    public void DoubleTrackCountInfersDoubleTrackProfile()
    {
        var result = Draw(Parameters(straight: 1000) with { NumberOfThroughTracks = 2 });
        StringAssert.Contains(result.Notes.Single().Message, "FREMO-H0-2E96");
        Assert.HasCount(2, Drawing(result).TrackElements);
    }

    [TestMethod]
    public void ThroughTrackCountMismatchFollowsTheProfile()
    {
        var result = Draw(Parameters(straight: 1000, exits: [West, East]) with { NumberOfThroughTracks = 2 });
        Assert.AreEqual(AutoDrawNoteCode.ThroughTracksMismatch, result.Notes.Single().Code);
        Assert.HasCount(1, Drawing(result).TrackElements);
    }

    [TestMethod]
    public void ZeroThroughTracksDrawsBaseboardOnly() =>
        Assert.IsEmpty(Drawing(Draw(Parameters(straight: 1000, exits: [West, East]) with { NumberOfThroughTracks = 0 })).TrackElements);

    [TestMethod]
    public void UndefinedDirectionsUseRegisteredOrder()
    {
        var result = Draw(Parameters(straight: 1000, exits: [East with { Label = "B", Direction = ExitSide.Undefined }, West with { Label = "A", Direction = ExitSide.Undefined }]));
        Assert.AreEqual(AutoDrawNoteCode.ExitDirectionsUndefined, result.Notes.Single().Code);
        Assert.AreEqual("B", Drawing(result).Exits[0].Label);
    }

    [TestMethod]
    public void LeftExitIsFirstWhateverTheOrder()
    {
        var drawing = Drawing(Draw(Parameters(straight: 1000, exits: [East, West])));
        Assert.AreEqual("W", drawing.Exits[0].Label);
        Assert.IsTrue(drawing.Exits[0].Pose.Position.IsCloseTo(Point.Origin));
    }

    [TestMethod]
    [DataRow(true, 2)]
    [DataRow(false, 1)]
    public void EndOfLineFlagMismatchIsNoted(bool flag, int exits)
    {
        var parameters = Parameters(straight: 1000, exits: exits == 2 ? [West, East] : [West]) with { IsEndOfLine = flag };
        Assert.IsTrue(Draw(parameters).Notes.Any(n => n.Code == AutoDrawNoteCode.EndOfLineFlagMismatch));
    }

    [TestMethod]
    public void OneExitAndHalfCircleIsALoopModule()
    {
        var result = Draw(Parameters(radius: 600, angle: 180, exits: [West]) with { IsEndOfLine = true });
        Assert.AreEqual(AutoDrawStatus.Check, result.Status);
        Assert.IsTrue(result.Notes.Any(n => n.Code == AutoDrawNoteCode.LoopModule));
        Assert.IsEmpty(Drawing(result).TrackElements);
    }

    [TestMethod]
    public void TurntableIsMarkedForChecking()
    {
        var result = Draw(Parameters(straight: 1000, exits: [West, East]) with { IsTurntable = true });
        Assert.AreEqual(AutoDrawStatus.Check, result.Status);
        Assert.IsTrue(result.Module!.IsTurntable);
    }

    [TestMethod]
    public void TooShortTerminusHasNoTracks()
    {
        var result = Draw(Parameters(straight: 40, exits: [West]) with { IsEndOfLine = true });
        Assert.AreEqual(AutoDrawStatus.NeedsManualCompletion, result.Status);
        Assert.IsEmpty(Drawing(result).TrackElements);
    }

    [TestMethod]
    public void ImpossibleGeometryIsReported()
    {
        // The inner side of a 600 mm wide body can't follow a 200 mm radius.
        var result = Draw(Parameters(radius: 200, angle: 90, exits: [West, East]) with { Width = 600 });
        Assert.AreEqual(AutoDrawStatus.NeedsManualDrawing, result.Status);
        Assert.AreEqual(AutoDrawNoteCode.InvalidGeometry, result.Notes[^1].Code);
    }

    [TestMethod]
    public void DuckUnderAreaIsTheWholeOutline()
    {
        var result = Draw(Parameters(straight: 1000, exits: [West, East]) with { IsDuckunder = true });
        Assert.IsTrue(result.Module!.IsDuckunder);
        Assert.AreEqual(1000 * 500, Drawing(result).DuckUnderAreas.Single().Area.Tessellate().SignedArea, Tolerance.Length);
    }

    [TestMethod]
    public void ResultRecordsParametersAndRegistryData()
    {
        var id = Guid.NewGuid();
        var parameters = Parameters(500, 2500, 15, West, East) with { ModuleId = id, Name = "Kvarntorp", Length = 1154, Width = 500, NumberOfSections = 1 };
        var module = Draw(parameters).Module!;
        Assert.AreEqual(id, module.Id);
        Assert.AreEqual("Kvarntorp", module.Name);
        Assert.AreEqual("H0", module.Scale?.ShortName);
        var drawing = module.Drawing;
        Assert.AreEqual(1154, drawing.RegistryMeasures!.Length);
        Assert.AreEqual(ModuleAutoDraw.GeneratorVersion, drawing.AutoDraw!.GeneratorVersion);
        Assert.AreSame(parameters, drawing.AutoDraw.Parameters);
    }

    [TestMethod]
    public void NullArgumentsAreRejected()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => ModuleAutoDraw.Create(null!, TestCatalogue.Instance));
        Assert.ThrowsExactly<ArgumentNullException>(() => ModuleAutoDraw.Create(Parameters(straight: 1), null!));
    }
}
