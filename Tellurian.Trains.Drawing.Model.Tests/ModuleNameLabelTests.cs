using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class ModuleNameLabelTests
{
    private const double TrackHalfWidth = 15;
    private static readonly ModuleNameText WithOwner = new("SFjä 7900", "Stefan Fjällemark");
    private static readonly ModuleNameText TitleOnly = new("SFjä 7900", null);

    private static ModuleNamePlacement Place(Module module, double screenDegrees, bool withOwner = true) =>
        ModuleNameLabel.Place(module.DefaultVariant, withOwner ? WithOwner : TitleOnly, new(screenDegrees), TrackHalfWidth);

    [TestMethod]
    public void FremoDesignationIsWrittenInsteadOfTheName()
    {
        var module = TestModules.Straight() with { Name = "Tellby", FremoNumber = 7900, Owners = [new() { Prefix = "SFjä", Name = "Stefan Fjällemark" }] };
        Assert.AreEqual(new ModuleNameText("SFjä 7900", "Stefan Fjällemark"), ModuleNameLabel.Text(module));
    }

    [TestMethod]
    public void WithoutFremoDesignationTheNameIsWritten()
    {
        var module = TestModules.Straight() with { Name = "Tellby", FremoNumber = 7900, Owners = [] };
        Assert.AreEqual(new ModuleNameText("Tellby", null), ModuleNameLabel.Text(module));
        Assert.IsNull(ModuleNameLabel.Text(module with { Name = " ", FremoNumber = null }));
    }

    [TestMethod]
    public void IsCentredBetweenTheTrackEndsAboveTheTrack()
    {
        var placement = Place(TestModules.Straight(), 0);
        Assert.AreEqual(600, placement.Title.X, TestTolerance.Precision);
        Assert.AreEqual(600, placement.Subtitle!.Value.X, TestTolerance.Precision);
        Assert.IsTrue(placement.Subtitle.Value.Y - placement.SubtitleSize!.Value / 2 > TrackHalfWidth, "Clear of the track.");
        Assert.IsTrue(placement.Title.Y > placement.Subtitle.Value.Y, "The owner's name is under the title.");
        Assert.AreEqual(0, placement.Rotation.Signed.Degrees, TestTolerance.Precision);
    }

    [TestMethod]
    public void TurnedHalfAroundOnScreenItGoesToTheOtherSideAndStillReadsUpright()
    {
        var placement = Place(TestModules.Straight(), 180);
        Assert.AreEqual(600, placement.Title.X, TestTolerance.Precision);
        Assert.IsTrue(placement.Title.Y < placement.Subtitle!.Value.Y && placement.Subtitle.Value.Y < 0, "Below the track in module coordinates, above it on screen.");
        Assert.AreEqual(0, (placement.Rotation + new Angle(180)).Signed.Degrees, TestTolerance.Precision, "Upright on screen.");
    }

    [TestMethod]
    [DataRow(30.0)]
    [DataRow(89.0)]
    [DataRow(91.0)]
    [DataRow(200.0)]
    [DataRow(-120.0)]
    public void ReadsUprightAndIsAboveTheTrackOnScreen(double screenDegrees)
    {
        var placement = Place(TestModules.Straight(), screenDegrees);
        var onScreen = (placement.Rotation + new Angle(screenDegrees)).Signed.Degrees;
        Assert.IsTrue(onScreen is > -90 and <= 90, $"Reads upright: {onScreen}°.");
        var titleOnScreen = (placement.Title - new Point(600, 0)).Rotated(new(screenDegrees));
        Assert.IsTrue(titleOnScreen.Y > 0, "Above the track on screen.");
    }

    [TestMethod]
    public void DoubleTrackIsKeptClear()
    {
        var module = TestModules.Transition();
        var single = Place(TestModules.Straight(), 0, withOwner: false);
        var doubleTrack = Place(module, 0, withOwner: false);
        Assert.IsTrue(doubleTrack.Title.Y > single.Title.Y, "Beyond the outer track.");
    }

    [TestMethod]
    public void CurveIsLabelledAtTheMiddleOfTheTrack()
    {
        var module = TestModules.Curve();
        var main = module.DefaultVariant.MainPath!.ToPath();
        var middle = main.PoseAt(main.Length / 2);
        var placement = Place(module, 0, withOwner: false);
        var projection = main.Project(placement.Title);
        Assert.AreEqual(main.Length / 2, projection.Distance, Tolerance.Length, "Centred between the track ends.");
        Assert.AreEqual(middle.Heading.Signed.Degrees, placement.Rotation.Signed.Degrees, TestTolerance.Precision, "Along the track.");
    }

    [TestMethod]
    public void IsAsBigAsFitsBetweenTheTrackAndTheSide()
    {
        var module = TestModules.Straight();
        var placement = Place(module, 0);
        var outerEdge = placement.Title.Y + placement.TitleSize / 2;
        var side = module.DefaultVariant.Sides.SelectMany(s => s.Path.ToPath().Tessellate().Points).Max(p => p.Y);
        Assert.IsTrue(outerEdge <= side, "Inside the module.");
        Assert.IsTrue(outerEdge >= side - placement.TitleSize, "Fills the space up to the side.");
        Assert.AreEqual(placement.TitleSize * ModuleNameLabel.OwnerPerTitleSize, placement.SubtitleSize!.Value, TestTolerance.Precision);
    }

    [TestMethod]
    public void IsNotBiggerThanTheMaximum()
    {
        var placement = ModuleNameLabel.Place(TestModules.Station().DefaultVariant, new("A", null), Angle.Zero, TrackHalfWidth, maxTitleSize: 50);
        Assert.AreEqual(50, placement.TitleSize, TestTolerance.Precision);
    }

    [TestMethod]
    public void LongTextIsMadeSmallerToFitTheLength()
    {
        var module = TestModules.Straight();
        var text = new ModuleNameText(new string('M', 40), null);
        var placement = ModuleNameLabel.Place(module.DefaultVariant, text, Angle.Zero, TrackHalfWidth);
        Assert.IsTrue(text.Title.Length * placement.TitleSize * ModuleNameLabel.CharacterWidthPerSize <= 1200 * ModuleNameLabel.LengthShare + Tolerance.Length);
    }
}
