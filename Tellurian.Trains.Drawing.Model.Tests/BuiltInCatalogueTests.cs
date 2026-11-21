using Tellurian.Trains.Drawing.Model.AutoDraw;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class BuiltInCatalogueTests
{
    private static DrawingCatalogue Catalogue => BuiltInCatalogue.Instance;

    private static EndProfile Profile(string designation, string scale = "H0") =>
        Catalogue.FindEndProfile(designation, scale) ?? throw new AssertFailedException($"{designation} isn't in the built-in catalogue.");

    private static JointFit Fit(string a, string b, string scaleB = "H0") =>
        JointFitEvaluator.Evaluate(Profile(a), ProfileOrientation.Normal, Profile(b, scaleB), ProfileOrientation.Normal, Catalogue.Overrides).Fit;

    [TestMethod]
    public void LoadsTheEmbeddedCatalogue()
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(Catalogue.Version));
        CollectionAssert.IsSubsetOf(new[] { "FREMO/H0-RE", "MMM-2R", "MMM-3R", "FREMO/N-RE" }, Catalogue.Standards.Select(s => s.ShortName).ToArray());
        CollectionAssert.AreEquivalent(new[] { "MMM-Summer", "MMM-Modern" }, Catalogue.AddOnsOf(Catalogue.FindStandard("MMM-2R")!.Id).Select(a => a.ShortName).ToArray());
        Assert.AreSame(Catalogue, BuiltInCatalogue.Instance);
    }

    [TestMethod]
    public void ReferencesAreConsistent()
    {
        foreach (var standard in Catalogue.Standards)
        {
            Assert.IsNotNull(Catalogue.FindScale(standard.Scale), $"{standard} has an unknown scale.");
            foreach (var id in new[] { standard.DefaultSingleTrackEndProfileId, standard.DefaultDoubleTrackEndProfileId }.OfType<Guid>())
            {
                var profile = Catalogue.FindEndProfile(id);
                Assert.IsNotNull(profile, $"A default end profile of {standard} is missing.");
                Assert.IsTrue(profile.Standards.Any(s => s.Id == standard.Id), $"{profile} doesn't belong to {standard}.");
            }
        }
        foreach (var profile in Catalogue.EndProfiles)
        {
            Assert.IsNotEmpty(profile.Standards, $"{profile} belongs to no standard.");
            Assert.IsTrue(profile.Standards.All(r => Catalogue.FindStandard(r.Id)?.ShortName == r.Name), $"{profile} refers to an unknown standard.");
            Assert.IsTrue(profile.Tracks.All(t => t.RailSystem == Catalogue.FindStandard(profile.Standards[0].Id)!.RailSystem), $"{profile} has tracks of another rail system than its standard.");
            Assert.IsNotNull(profile.Source, $"{profile} has no source document (CA-1).");
        }
        Assert.IsTrue(Catalogue.AddOns.All(a => Catalogue.FindStandard(a.BaseStandardId) is not null));
    }

    [TestMethod]
    public void IdsAreUnique()
    {
        var ids = Catalogue.Standards.Select(s => s.Id).Concat(Catalogue.AddOns.Select(a => a.Id)).Concat(Catalogue.EndProfiles.Select(p => p.Id)).ToList();
        Assert.HasCount(ids.Count, ids.Distinct());
    }

    // Phase 5 replaces the hand-entered profiles with drawings from the FREMO and MMM PDFs.
    [TestMethod]
    public void EveryProfileIsDrawnOrMarkedProvisional()
    {
        Assert.IsTrue(Catalogue.EndProfiles.All(p => !string.IsNullOrWhiteSpace(p.Source!.Revision)));
        Assert.AreNotEqual(BuiltInCatalogue.ProvisionalRevision, Profile("FREMO-H0-B96").Source!.Revision);
        Assert.AreNotEqual(BuiltInCatalogue.ProvisionalRevision, Profile("FREMO-H0-B96f").Source!.Revision);
    }

    [TestMethod]
    [DataRow("FREMO-H0-B96", "B96")]
    [DataRow("FREMO-H0-B96f", "B96f")]
    [DataRow("FREMO-H0-2E96", "2E96")]
    [DataRow("MMM480", "MMM480")]
    public void EndProfilesHaveShortNames(string designation, string shortName) =>
        Assert.AreEqual(shortName, Profile(designation).ShortNameOrDesignation);

    [TestMethod]
    public void SameSymmetricProfileFits() => Assert.AreEqual(JointFit.Fits, Fit("MMM480", "MMM480"));

    // B96 is a mountain profile: two ends fit only when one of them is built mirrored.
    [TestMethod]
    public void AsymmetricProfileFitsWhenOneEndIsMirrored()
    {
        var b96 = Profile("FREMO-H0-B96");
        Assert.AreEqual(JointFit.Fits, JointFitEvaluator.Evaluate(b96, ProfileOrientation.Normal, b96, ProfileOrientation.Mirrored).Fit);
        Assert.AreEqual(JointFit.FitsWithMismatch, JointFitEvaluator.Evaluate(b96, ProfileOrientation.Normal, b96, ProfileOrientation.Normal).Fit);
    }

    private static ModuleDrawing Straight(string standard, string west, string east, bool isEndewender = false) =>
        ModuleAutoDraw.Create(new()
        {
            Name = "Test",
            Standard = standard,
            Straight = 1000,
            IsEndProfileSwitcher = isEndewender,
            Exits =
            [
                new() { Label = "W", Direction = ExitDirection.Left, EndProfile = west },
                new() { Label = "E", Direction = ExitDirection.Right, EndProfile = east },
            ],
        }, Catalogue).Module!.Drawing;

    private static JointFit Joint(ModuleExit a, ModuleExit b) =>
        JointFitEvaluator.Evaluate(Profile(a.EndProfile.Name), a.Orientation, Profile(b.EndProfile.Name), b.Orientation, Catalogue.Overrides).Fit;

    // ModuleDrawing §4.3: the far end of an asymmetric profile is mirrored, so the high side of B96 stays on
    // the same side of the track and two such modules join without mismatch.
    [TestMethod]
    public void AutoDrawMirrorsAnAsymmetricProfileAtTheFarEnd()
    {
        var module = Straight("FREMO/H0-RE", "FREMO-H0-B96", "FREMO-H0-B96");
        CollectionAssert.AreEqual(new[] { ProfileOrientation.Normal, ProfileOrientation.Mirrored }, module.Exits.Select(e => e.Orientation).ToArray());
        Assert.AreEqual(JointFit.Fits, Joint(module.Exits[1], module.Exits[0]));
    }

    // An Endewender turns the profile over: the high side of B96 changes side, so the far end isn't mirrored.
    [TestMethod]
    public void EndewenderDoesNotMirrorTheFarEnd()
    {
        var module = Straight("FREMO/H0-RE", "FREMO-H0-B96", "FREMO-H0-B96", isEndewender: true);
        Assert.IsTrue(module.Exits.All(e => e.Orientation == ProfileOrientation.Normal));
        Assert.AreEqual(JointFit.FitsWithMismatch, Joint(module.Exits[1], module.Exits[0]));
    }

    [TestMethod]
    public void AutoDrawDoesNotMirrorASymmetricProfile() =>
        Assert.IsTrue(Straight("MMM-2R", "MMM480", "MMM480").Exits.All(e => e.Orientation == ProfileOrientation.Normal));

    // A module may have end profiles of different standards, e.g. FREMO at one end and MMM at the other.
    [TestMethod]
    public void TemplateCanHaveAnEndProfileOfAnotherStandardAtEachEnd()
    {
        var result = new ModuleTemplate
        {
            Kind = ModuleTemplateKind.Straight,
            Name = "Transition",
            Standard = Catalogue.FindStandard("FREMO/H0-RE")!,
            EndProfile = Profile("FREMO-H0-B96"),
            SecondEndProfile = Profile("MMM480"),
            Straight = 1000,
        }.Create(Catalogue);
        Assert.IsTrue(result.IsDrawn, string.Join("; ", result.Notes));
        CollectionAssert.AreEqual(new[] { "FREMO-H0-B96", "MMM480" }, result.Module!.Drawing.Exits.Select(e => e.EndProfile.Name).ToArray());
    }

    // Exit criterion of Phase 2: the built-in pairs are classified as expected.
    [TestMethod]
    public void FremoH0AndMmm2RFitWithClamps()
    {
        Assert.AreEqual(JointFit.FitsWithClamps, Fit("FREMO-H0-B96", "MMM480"));
        Assert.AreEqual(JointFit.FitsWithClamps, Fit("MMM480", "FREMO-H0-B96"));
    }

    [TestMethod]
    public void TwoRailAndThreeRailAreIncompatible()
    {
        Assert.AreEqual(JointFit.Incompatible, Fit("MMM480", "MMM480-3R"));
        var result = JointFitEvaluator.Evaluate(Profile("MMM480"), ProfileOrientation.Normal, Profile("MMM480-3R"), ProfileOrientation.Normal);
        Assert.IsTrue(result.Differences.HasFlag(FitDifferences.RailSystem));
    }

    [TestMethod]
    public void SingleAndDoubleTrackAreIncompatible() => Assert.AreEqual(JointFit.Incompatible, Fit("FREMO-H0-B96", "FREMO-H0-2E96"));

    [TestMethod]
    public void DifferentScalesAreIncompatible() => Assert.AreEqual(JointFit.Incompatible, Fit("FREMO-H0-B96", "FREMO-N-RE", "N"));

    [TestMethod]
    public void OnlyTheMountainProfilesAreAsymmetric()
    {
        var asymmetric = Catalogue.EndProfiles.Where(p => !EndProfileAnalysis.Analyze(p).IsSymmetric).Select(p => p.Designation).ToArray();
        CollectionAssert.AreEquivalent(new[] { "FREMO-H0-B96", "FREMO-H0-B96f" }, asymmetric);
        Assert.AreEqual(FitDifferences.GroundLine, EndProfileAnalysis.AsymmetricElements(Profile("FREMO-H0-B96")) & ~FitDifferences.Outline);
    }

    [TestMethod]
    [DataRow("FREMO/H0-RE", 1, "FREMO-H0-B96")]
    [DataRow("FREMO/H0-RE", 2, "FREMO-H0-2E96")]
    [DataRow("MMM-2R", 1, "MMM480")]
    [DataRow("MMM-3R", 1, "MMM480-3R")]
    [DataRow("FREMO/N-RE", 1, "FREMO-N-RE")]
    public void AutoDrawInfersExitsFromTheStandard(string standard, int tracks, string expectedProfile)
    {
        var result = ModuleAutoDraw.Create(new() { Name = "Test", Standard = standard, Straight = 1000, NumberOfThroughTracks = tracks }, Catalogue);
        Assert.AreEqual(AutoDrawStatus.Complete, result.Status, string.Join(" ", result.Notes));
        Assert.IsTrue(result.Module!.Drawing.Exits.All(e => e.EndProfile.Name == expectedProfile));
        Assert.HasCount(tracks, result.Module.Drawing.TrackElements);
    }

    [TestMethod]
    public void CatalogueFileMatchesTheSchemaAndRoundTrips()
    {
        var json = DrawingFileSerializer.Serialize(Catalogue);
        SchemaValidator.AssertValid(DrawingFileKind.Catalogue, json);
        Assert.AreEqual(json, DrawingFileSerializer.Serialize(DrawingFileSerializer.DeserializeCatalogue(json)));
    }

    // CA-2: user entries are kept apart from the built-in ones and are merged into one catalogue.
    [TestMethod]
    public void UserEntriesAreAddedToTheBuiltInOnes()
    {
        var copy = Profile("FREMO-H0-B96") with { Id = Guid.NewGuid(), Designation = "MY-B96" };
        var user = new DrawingCatalogue { EndProfiles = [copy], Scales = [new() { ShortName = "0", Denominator = 45 }] };
        var merged = DrawingCatalogue.Merge(Catalogue, user);
        Assert.HasCount(Catalogue.EndProfiles.Count + 1, merged.EndProfiles);
        Assert.IsNotNull(merged.FindEndProfile("FREMO-H0-B96", "H0"));
        Assert.IsNotNull(merged.FindEndProfile("MY-B96", "H0"));
        Assert.IsNotNull(merged.FindScale("0"));
        Assert.AreEqual(Catalogue.Version, merged.Version);
        Assert.HasCount(Catalogue.EndProfiles.Count, BuiltInCatalogue.Instance.EndProfiles, "The built-in catalogue is unchanged.");
    }

    [TestMethod]
    public void UserEntryReplacesBuiltInEntryWithTheSameKey()
    {
        var standard = Catalogue.FindStandard("MMM-2R")! with { DefaultCurveRadius = 1800 };
        var user = new DrawingCatalogue { Standards = [standard], Scales = [new() { ShortName = " h0 ", Denominator = 87 }] };
        var merged = DrawingCatalogue.Merge(Catalogue, user);
        Assert.HasCount(Catalogue.Standards.Count, merged.Standards);
        Assert.AreEqual(1800, merged.FindStandard("MMM-2R")!.DefaultCurveRadius);
        Assert.HasCount(Catalogue.Scales.Count, merged.Scales);
    }

    [TestMethod]
    public void UserOverrideReplacesBuiltInOverrideOfTheSamePair()
    {
        var a = Profile("FREMO-H0-B96").Id;
        var b = Profile("MMM480").Id;
        var builtIn = Catalogue with { Overrides = [new() { FirstEndProfileId = a, SecondEndProfileId = b, Fit = JointFit.FitsWithClamps }] };
        var user = new DrawingCatalogue { Overrides = [new() { FirstEndProfileId = b, SecondEndProfileId = a, Fit = JointFit.Fits, Note = "Our club adapter." }] };
        var merged = DrawingCatalogue.Merge(builtIn, user);
        Assert.AreEqual(JointFit.Fits, merged.Overrides.Single().Fit);
    }
}
