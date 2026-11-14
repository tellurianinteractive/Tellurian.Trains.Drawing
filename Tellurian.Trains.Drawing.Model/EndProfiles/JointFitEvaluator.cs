using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.Catalogue;

namespace Tellurian.Trains.Drawing.Model.EndProfiles;

/// <summary>
/// Classifies how two module ends fit (spec §3.2). End A's drawing, as built at its exit, is mirrored
/// about the centre line and laid over end B's, aligned on rail top and the centre line.
/// The rules are evaluated in order, the first match decides, and a catalogue override replaces the result.
/// </summary>
public static class JointFitEvaluator
{
    public static JointFitResult Evaluate(
        EndProfile a, ProfileOrientation orientationA,
        EndProfile b, ProfileOrientation orientationB,
        IEnumerable<EndProfileCompatibilityOverride>? overrides = null,
        FitTolerances? tolerances = null)
    {
        tolerances ??= FitTolerances.Default;
        var facingA = a.Oriented(orientationA).MirroredX();
        var endB = b.Oriented(orientationB);

        var differences = EndProfileComparison.CompareTracks(facingA.Tracks, endB.Tracks, tolerances.Joint);

        IReadOnlyList<int> unmatchedA = [];
        IReadOnlyList<int> unmatchedB = [];
        if (facingA.HasScrewHoles && endB.HasScrewHoles)
        {
            var match = EndProfileComparison.MatchCircles(facingA.ScrewHoles, endB.ScrewHoles, tolerances.Joint);
            unmatchedA = match.UnmatchedFirst;
            unmatchedB = match.UnmatchedSecond;
            if (!match.IsComplete && match.Pairs.Count < tolerances.MinimumBoltedScrewHoles)
                differences |= FitDifferences.ScrewHoles;
        }
        else if (!string.IsNullOrWhiteSpace(a.ScrewHolePattern) && !string.IsNullOrWhiteSpace(b.ScrewHolePattern))
        {
            if (!string.Equals(a.ScrewHolePattern.Trim(), b.ScrewHolePattern.Trim(), StringComparison.OrdinalIgnoreCase))
                differences |= FitDifferences.ScrewHoles;
        }
        else
        {
            differences |= FitDifferences.ScrewHolesUnknown;
        }

        var groundLineDeviation = default(ShapeDeviation?);
        if (facingA.GroundLine is not null && endB.GroundLine is not null)
        {
            groundLineDeviation = EndProfileComparison.CompareGroundLines(facingA.GroundLine, endB.GroundLine, tolerances.Landscape / 4);
            if (groundLineDeviation is not { } deviation || !deviation.IsWithin(tolerances.Landscape))
                differences |= FitDifferences.GroundLine;
        }
        else
        {
            differences |= FitDifferences.GroundLineUnknown;
        }

        if (facingA.Outline is not null && endB.Outline is not null &&
            !ShapeComparison.IsWithin(facingA.Outline.Tessellate(), endB.Outline.Tessellate(), tolerances.Joint, tolerances.Joint / 2))
            differences |= FitDifferences.Outline;

        var ruleFit =
            (differences & FitDifferences.Tracks) != 0 ? JointFit.Incompatible :
            (differences & FitDifferences.ScrewHoles) != 0 ? JointFit.FitsWithClamps :
            (differences & FitDifferences.GroundLine) != 0 ? JointFit.FitsWithMismatch :
            JointFit.Fits;

        var applied = overrides?.FirstOrDefault(o => o.Applies(a.Id, b.Id));
        return new()
        {
            Fit = applied?.Fit ?? ruleFit,
            RuleFit = ruleFit,
            Differences = differences,
            Override = applied,
            UnmatchedScrewHolesA = unmatchedA,
            UnmatchedScrewHolesB = unmatchedB,
            GroundLineDeviation = groundLineDeviation,
        };
    }
}
