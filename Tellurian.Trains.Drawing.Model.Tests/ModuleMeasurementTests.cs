using Tellurian.Trains.Drawing.Model.AutoDraw;
using Tellurian.Trains.Drawing.Model.Measures;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class ModuleMeasurementTests
{
    private const string Fremo = "FREMO/H0-RE";
    private const double PlateWidth = 500;

    private static readonly AutoDrawExit West = new() { Label = "W", Direction = ExitDirection.West, EndProfile = "FREMO-H0-B96" };
    private static readonly AutoDrawExit East = new() { Label = "E", Direction = ExitDirection.East, EndProfile = "FREMO-H0-B96" };

    private static ModuleVariant Draw(AutoDrawParameters parameters)
    {
        var result = ModuleAutoDraw.Create(parameters, TestCatalogue.Instance);
        Assert.IsNotNull(result.Module, string.Join(" ", result.Notes));
        return result.Module.DefaultVariant;
    }

    private static ModuleMeasures Measure(ModuleVariant variant) => ModuleMeasurement.Derive(variant, TestCatalogue.Lookup);

    private static AutoDrawParameters Parameters(double? straight = null, double? radius = null, double? angle = null) =>
        new() { Name = "Test", Standard = Fremo, Straight = straight, Radius = radius, Angle = angle, Exits = [West, East] };

    [TestMethod]
    public void StraightModule()
    {
        var measures = Measure(Draw(Parameters(straight: 1000)));
        Assert.AreEqual(1000, measures.Length!.Value, Precision);
        Assert.AreEqual(1000, measures.Straight!.Value, Precision);
        Assert.AreEqual(0, measures.Angle!.Value, Precision);
        Assert.IsNull(measures.Radius);
        Assert.AreEqual(PlateWidth, measures.Width!.Value, Precision);
        Assert.AreEqual(1000, measures.TrackLength, Precision);
        Assert.AreEqual(1, measures.NumberOfSections);
        Assert.IsFalse(measures.IsRamp);
    }

    // ModuleDrawing §11, example 2.
    [TestMethod]
    public void CurvedModule()
    {
        var measures = Measure(Draw(Parameters(radius: 2000, angle: 30)));
        Assert.AreEqual(2000 * Math.PI / 6, measures.Length!.Value, Precision);
        Assert.AreEqual(0, measures.Straight!.Value, Precision);
        Assert.AreEqual(2000, measures.Radius!.Value, Precision);
        Assert.AreEqual(30, measures.Angle!.Value, Precision);
        Assert.AreEqual(PlateWidth, measures.Width!.Value, Precision);
    }

    // Example 3.
    [TestMethod]
    public void StraightAndCurveModule()
    {
        var measures = Measure(Draw(Parameters(straight: 500, radius: 2500, angle: 15) with { CurveDirection = CurveDirection.Right }));
        Assert.AreEqual(500 + 2500 * Math.PI / 12, measures.Length!.Value, Precision);
        Assert.AreEqual(500, measures.Straight!.Value, Precision);
        Assert.AreEqual(2500, measures.Radius!.Value, Precision);
        Assert.AreEqual(15, measures.Angle!.Value, Precision);
        Assert.AreEqual(PlateWidth, measures.Width!.Value, Precision);
    }

    [TestMethod]
    public void SplitStraightCountsBothParts()
    {
        var measures = Measure(Draw(Parameters(straight: 600, radius: 2000, angle: 10) with { StraightPosition = StraightPosition.Split }));
        Assert.AreEqual(600, measures.Straight!.Value, Precision);
    }

    // Example 6: the widened body is measured, not the plates.
    [TestMethod]
    public void WidenedStationWithSections()
    {
        var variant = Draw(Parameters(straight: 3000) with { Width = 600, NumberOfSections = 3, IsOperationPlace = true });
        var measures = Measure(variant);
        Assert.AreEqual(600, measures.Width!.Value, Precision);
        Assert.AreEqual(3, measures.NumberOfSections);
    }

    [TestMethod]
    public void WidenedCurve()
    {
        var measures = Measure(Draw(Parameters(radius: 2000, angle: 45) with { Width = 700 }));
        Assert.AreEqual(700, measures.Width!.Value, Precision);
    }

    // Example 5: the back edge doesn't add to the width.
    [TestMethod]
    public void EndOfLineModule()
    {
        var variant = Draw(new() { Name = "Terminus", Standard = Fremo, Straight = 1200, Exits = [West], NumberOfThroughTracks = 1, IsEndOfLine = true });
        var measures = Measure(variant);
        Assert.AreEqual(1200, measures.Length!.Value, Precision);
        Assert.AreEqual(PlateWidth, measures.Width!.Value, Precision);
        Assert.AreEqual(1200 - ModuleAutoDraw.BufferStopMargin, measures.TrackLength, Precision);
        Assert.IsFalse(measures.IsRamp);
    }

    [TestMethod]
    public void DoubleTrackCurveSumsBothTracks()
    {
        var variant = Draw(new()
        {
            Name = "Double",
            Standard = Fremo,
            Radius = 2000,
            Angle = 30,
            NumberOfThroughTracks = 2,
            Exits = [West with { EndProfile = "FREMO-H0-2E96" }, East with { EndProfile = "FREMO-H0-2E96" }],
        });
        // Concentric tracks at R ± c/c/2 sum to twice the main path length.
        Assert.AreEqual(2 * 2000 * Math.PI / 6, Measure(variant).TrackLength, Precision);
    }

    [TestMethod]
    public void RampHasHeightDifferenceAndGradient()
    {
        var variant = Draw(Parameters(straight: 1000));
        variant = variant with { Exits = [variant.Exits[0], variant.Exits[1] with { RailTopOffset = 20 }] };
        var measures = Measure(variant);
        Assert.IsTrue(measures.IsRamp);
        Assert.AreEqual(20, measures.RampHeightDifference!.Value, Precision);
        Assert.AreEqual(20, measures.RampGradient!.Value, Precision);
    }

    [TestMethod]
    public void DescendingRampHasNegativeGradient()
    {
        var variant = Draw(Parameters(straight: 2000));
        variant = variant with { Exits = [variant.Exits[0] with { RailTopOffset = 10 }, variant.Exits[1] with { RailTopOffset = -10 }] };
        var measures = Measure(variant);
        Assert.AreEqual(20, measures.RampHeightDifference!.Value, Precision);
        Assert.AreEqual(-10, measures.RampGradient!.Value, Precision);
    }

    [TestMethod]
    public void WithoutMainPathOnlyDrawingMeasuresAreDerived()
    {
        var measures = Measure(Draw(Parameters(straight: 1000)) with { MainPath = null });
        Assert.IsNull(measures.Length);
        Assert.IsNull(measures.Width);
        Assert.IsNull(measures.Angle);
        Assert.AreEqual(1000, measures.TrackLength, Precision);
    }

    // ModuleDrawing §8, step 6: an auto-drawn module agrees with the parameters it was drawn from.
    [TestMethod]
    [DataRow(1200.0, null, null, null, 1200.0)]
    [DataRow(null, 2000.0, 30.0, null, 1047.0)]
    [DataRow(500.0, 2500.0, 15.0, 500.0, 1154.0)]
    [DataRow(2000.0, null, null, 650.0, 2000.0)]
    public void AutoDrawnModuleMatchesItsRegistryValues(double? straight, double? radius, double? angle, double? width, double length)
    {
        var variant = Draw(Parameters(straight, radius, angle) with { Width = width, Length = length, NumberOfSections = 1 });
        var differences = MeasureComparison.Compare(Measure(variant), variant.RegistryMeasures);
        Assert.IsNotEmpty(differences);
        Assert.IsFalse(differences.Any(d => d.IsFlagged), string.Join(", ", differences.Where(d => d.IsFlagged)));
    }

    [TestMethod]
    public void DifferencesBeyondToleranceAreFlagged()
    {
        var measures = Measure(Draw(Parameters(straight: 1200)));
        var entered = new RegistryMeasures { Length = 1250, Width = 500.5, Radius = 0, Angle = 0, NumberOfSections = 2 };
        var differences = MeasureComparison.Compare(measures, entered).ToDictionary(d => d.Kind);
        Assert.HasCount(5, differences);
        Assert.IsTrue(differences[MeasureKind.Length].IsFlagged);
        Assert.AreEqual(-50, differences[MeasureKind.Length].Difference!.Value, Precision);
        Assert.IsFalse(differences[MeasureKind.Width].IsFlagged);
        Assert.IsFalse(differences[MeasureKind.Radius].IsFlagged, "A straight module has radius 0.");
        Assert.IsFalse(differences[MeasureKind.Angle].IsFlagged);
        Assert.IsTrue(differences[MeasureKind.NumberOfSections].IsFlagged);
    }

    [TestMethod]
    public void MissingDerivedValueIsFlagged()
    {
        var measures = new ModuleMeasures { NumberOfSections = 1 };
        var difference = MeasureComparison.Compare(measures, new() { Width = 500, Radius = 0 });
        Assert.IsTrue(difference.All(d => d.IsFlagged && d.Derived is null && d.Difference is null));
    }

    [TestMethod]
    public void NoEnteredValuesGiveNoDifferences() =>
        Assert.IsEmpty(MeasureComparison.Compare(new ModuleMeasures(), null));

    [TestMethod]
    public void CustomTolerancesApply()
    {
        var measures = Measure(Draw(Parameters(radius: 2000, angle: 30)));
        var differences = MeasureComparison.Compare(measures, new() { Angle = 30.5 }, new() { Angle = 1 });
        Assert.IsFalse(differences.Single().IsFlagged);
    }
}
