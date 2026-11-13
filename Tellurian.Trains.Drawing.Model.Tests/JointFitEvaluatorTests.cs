using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class JointFitEvaluatorTests
{
    private const ProfileOrientation Normal = ProfileOrientation.Normal;
    private const ProfileOrientation Mirrored = ProfileOrientation.Mirrored;

    private static JointFitResult Evaluate(EndProfile a, EndProfile b, ProfileOrientation oa = Normal, ProfileOrientation ob = Normal) =>
        JointFitEvaluator.Evaluate(a, oa, b, ob);

    [TestMethod]
    [DataRow(Normal, Normal)]
    [DataRow(Normal, Mirrored)]
    [DataRow(Mirrored, Normal)]
    [DataRow(Mirrored, Mirrored)]
    public void SymmetricProfilesFitInEveryOrientation(ProfileOrientation oa, ProfileOrientation ob)
    {
        var result = Evaluate(TestProfiles.FremoSingle(), TestProfiles.FremoSingle(), oa, ob);
        Assert.AreEqual(JointFit.Fits, result.Fit);
        Assert.AreEqual(JoinMethod.Bolted, result.JoinMethod);
        Assert.AreEqual(FitDifferences.None, result.Differences);
    }

    [TestMethod]
    [DataRow(Normal, Mirrored, JointFit.Fits)]
    [DataRow(Mirrored, Normal, JointFit.Fits)]
    [DataRow(Normal, Normal, JointFit.FitsWithMismatch)]
    [DataRow(Mirrored, Mirrored, JointFit.FitsWithMismatch)]
    public void AsymmetricGroundLineDependsOnOrientation(ProfileOrientation oa, ProfileOrientation ob, JointFit expected)
    {
        // A's right side meets B's left side, so two identical asymmetric ends only match when one is mirrored.
        var profile = TestProfiles.Asymmetric();
        var result = Evaluate(profile, profile, oa, ob);
        Assert.AreEqual(expected, result.Fit);
        Assert.AreEqual(JoinMethod.Bolted, result.JoinMethod);
        if (expected == JointFit.FitsWithMismatch)
        {
            Assert.IsTrue(result.Differences.HasFlag(FitDifferences.GroundLine));
            Assert.IsNotNull(result.GroundLineDeviation);
            Assert.AreEqual(30, result.GroundLineDeviation.Value.Maximum, 1);
        }
    }

    [TestMethod]
    [DataRow(Normal)]
    [DataRow(Mirrored)]
    public void SymmetricMeetsAsymmetricWithMismatch(ProfileOrientation orientation) =>
        Assert.AreEqual(JointFit.FitsWithMismatch, Evaluate(TestProfiles.FremoSingle(), TestProfiles.Asymmetric(), Normal, orientation).Fit);

    [TestMethod]
    [DataRow(Normal, Mirrored, JointFit.Fits)]
    [DataRow(Normal, Normal, JointFit.FitsWithClamps)]
    public void AsymmetricScrewHolesDependOnOrientation(ProfileOrientation oa, ProfileOrientation ob, JointFit expected)
    {
        var profile = TestProfiles.AsymmetricHoles();
        var result = Evaluate(profile, profile, oa, ob);
        Assert.AreEqual(expected, result.Fit);
        if (expected == JointFit.FitsWithClamps)
        {
            CollectionAssert.AreEqual(new[] { 0 }, result.UnmatchedScrewHolesA.ToArray());
            CollectionAssert.AreEqual(new[] { 0 }, result.UnmatchedScrewHolesB.ToArray());
        }
    }

    [TestMethod]
    public void SingleTrackAcrossStandardsNeedsClamps()
    {
        var result = Evaluate(TestProfiles.FremoSingle(), TestProfiles.MmmSingle());
        Assert.AreEqual(JointFit.FitsWithClamps, result.Fit);
        Assert.AreEqual(JoinMethod.Clamped, result.JoinMethod);
        Assert.HasCount(4, result.UnmatchedScrewHolesA);
        Assert.HasCount(2, result.UnmatchedScrewHolesB);
        // Plates of different size: informational only.
        Assert.IsTrue(result.Differences.HasFlag(FitDifferences.Outline));
        Assert.IsFalse(result.Differences.HasFlag(FitDifferences.GroundLine));
    }

    [TestMethod]
    public void DifferentDoubleTrackCentreDistanceIsIncompatible()
    {
        var result = Evaluate(TestProfiles.Double(45), TestProfiles.Double(55));
        Assert.AreEqual(JointFit.Incompatible, result.Fit);
        Assert.IsNull(result.JoinMethod);
        Assert.IsTrue(result.Differences.HasFlag(FitDifferences.TrackPositions));
    }

    [TestMethod]
    public void CentreDistanceWithinToleranceFits() =>
        Assert.AreEqual(JointFit.Fits, Evaluate(TestProfiles.Double(45), TestProfiles.Double(45.4)).Fit);

    [TestMethod]
    public void DifferentNumberOfTracksIsIncompatible()
    {
        var result = Evaluate(TestProfiles.FremoSingle(), TestProfiles.Double(45));
        Assert.AreEqual(JointFit.Incompatible, result.Fit);
        Assert.IsTrue(result.Differences.HasFlag(FitDifferences.TrackCount));
    }

    [TestMethod]
    public void TwoRailAndThreeRailAreIncompatible()
    {
        var result = Evaluate(TestProfiles.Double(45), TestProfiles.Double(45, RailSystem.ThreeRail));
        Assert.AreEqual(JointFit.Incompatible, result.Fit);
        Assert.AreEqual(FitDifferences.RailSystem, result.Differences & FitDifferences.Tracks);
    }

    [TestMethod]
    public void DifferentGaugeIsIncompatible()
    {
        var narrow = TestProfiles.FremoSingle() with { Tracks = [new() { Offset = 0, Gauge = 12 }] };
        var result = Evaluate(TestProfiles.FremoSingle(), narrow);
        Assert.AreEqual(JointFit.Incompatible, result.Fit);
        Assert.IsTrue(result.Differences.HasFlag(FitDifferences.Gauge));
    }

    [TestMethod]
    public void OffCentreTrackMustMeetMirroredPosition()
    {
        // A track 20 mm right of centre on A meets a track 20 mm left of centre on B.
        var right = TestProfiles.FremoSingle() with { Tracks = [new() { Offset = 20, Gauge = TestProfiles.H0Gauge }] };
        var left = TestProfiles.FremoSingle() with { Tracks = [new() { Offset = -20, Gauge = TestProfiles.H0Gauge }] };
        Assert.AreEqual(JointFit.Fits, Evaluate(right, left).Fit);
        Assert.AreEqual(JointFit.Incompatible, Evaluate(right, right).Fit);
        Assert.AreEqual(JointFit.Fits, Evaluate(right, right, Normal, Mirrored).Fit);
    }

    [TestMethod]
    public void TrackGeometryIsCheckedBeforeScrewHoles()
    {
        var result = Evaluate(TestProfiles.Double(45), TestProfiles.MmmSingle());
        Assert.AreEqual(JointFit.Incompatible, result.Fit);
        Assert.IsTrue(result.Differences.HasFlag(FitDifferences.ScrewHoles));
    }

    [TestMethod]
    public void MissingScrewHolesWithoutPatternAreUnknownAndBolted()
    {
        var result = Evaluate(TestProfiles.WithoutScrewHoles(null), TestProfiles.FremoSingle());
        Assert.AreEqual(JointFit.Fits, result.Fit);
        Assert.AreEqual(JoinMethod.Bolted, result.JoinMethod);
        Assert.IsTrue(result.Differences.HasFlag(FitDifferences.ScrewHolesUnknown));
    }

    [TestMethod]
    [DataRow("FREMO-H0", JointFit.Fits)]
    [DataRow(" fremo-h0 ", JointFit.Fits)]
    [DataRow("MMM", JointFit.FitsWithClamps)]
    public void MissingScrewHolesFallBackToPattern(string pattern, JointFit expected)
    {
        // FremoSingle has drawn holes and the pattern "FREMO-H0".
        var result = Evaluate(TestProfiles.WithoutScrewHoles(pattern), TestProfiles.FremoSingle());
        Assert.AreEqual(expected, result.Fit);
        Assert.IsFalse(result.Differences.HasFlag(FitDifferences.ScrewHolesUnknown));
    }

    [TestMethod]
    public void MissingGroundLineIsNotAMismatch()
    {
        var result = Evaluate(TestProfiles.FremoSingle() with { GroundLine = null }, TestProfiles.Asymmetric());
        Assert.AreEqual(JointFit.Fits, result.Fit);
        Assert.IsTrue(result.Differences.HasFlag(FitDifferences.GroundLineUnknown));
        Assert.IsNull(result.GroundLineDeviation);
    }

    [TestMethod]
    public void GroundLinesWithoutCommonRangeMismatch()
    {
        var left = TestProfiles.FremoSingle() with { GroundLine = PathDrawing.Polyline([new(-250, -40), new(-100, -40)]) };
        var result = Evaluate(left, left);
        // Mirrored A covers 100…250 and B covers −250…−100: nothing in common.
        Assert.AreEqual(JointFit.FitsWithMismatch, result.Fit);
        Assert.IsNull(result.GroundLineDeviation);
    }

    [TestMethod]
    public void OverrideReplacesTheRuleResult()
    {
        var a = TestProfiles.FremoSingle();
        var b = TestProfiles.MmmSingle();
        var overrides = new[] { new EndProfileCompatibilityOverride { FirstEndProfileId = b.Id, SecondEndProfileId = a.Id, Fit = JointFit.Fits, Note = "Known adapter holes" } };
        var result = JointFitEvaluator.Evaluate(a, Normal, b, Normal, overrides);
        Assert.AreEqual(JointFit.Fits, result.Fit);
        Assert.AreEqual(JointFit.FitsWithClamps, result.RuleFit);
        Assert.IsTrue(result.IsOverridden);
        Assert.AreEqual(JoinMethod.Bolted, result.JoinMethod);
    }

    [TestMethod]
    public void OverrideForOtherPairIsIgnored()
    {
        var overrides = new[] { new EndProfileCompatibilityOverride { FirstEndProfileId = Guid.NewGuid(), SecondEndProfileId = Guid.NewGuid(), Fit = JointFit.Incompatible } };
        var result = JointFitEvaluator.Evaluate(TestProfiles.FremoSingle(), Normal, TestProfiles.FremoSingle(), Normal, overrides);
        Assert.IsFalse(result.IsOverridden);
        Assert.AreEqual(JointFit.Fits, result.Fit);
    }

    [TestMethod]
    public void LooserTolerancesAcceptSmallDifferences()
    {
        var tolerances = new FitTolerances { Joint = 15 };
        Assert.AreEqual(JointFit.Incompatible, Evaluate(TestProfiles.Double(45), TestProfiles.Double(55)).Fit);
        Assert.AreEqual(JointFit.Fits, JointFitEvaluator.Evaluate(TestProfiles.Double(45), Normal, TestProfiles.Double(55), Normal, tolerances: tolerances).Fit);
    }

    [TestMethod]
    [DataRow(JointFit.Fits, JoinMethod.Bolted)]
    [DataRow(JointFit.FitsWithMismatch, JoinMethod.Bolted)]
    [DataRow(JointFit.FitsWithClamps, JoinMethod.Clamped)]
    public void JoinMethodFollowsFit(JointFit fit, JoinMethod expected) =>
        Assert.AreEqual(expected, JointFitResult.JoinMethodFor(fit));

    [TestMethod]
    public void IncompatibleHasNoJoinMethod() =>
        Assert.IsNull(JointFitResult.JoinMethodFor(JointFit.Incompatible));
}
