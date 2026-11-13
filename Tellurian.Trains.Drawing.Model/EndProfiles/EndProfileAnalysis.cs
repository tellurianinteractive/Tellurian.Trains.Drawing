using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.EndProfiles;

/// <summary>Derives properties from an end profile drawing, including symmetry (spec EP-7).</summary>
public static class EndProfileAnalysis
{
    public static EndProfileProperties Analyze(EndProfile profile, FitTolerances? tolerances = null)
    {
        tolerances ??= FitTolerances.Default;
        var offsets = profile.Tracks.Select(t => t.Offset).Order().ToList();
        return new()
        {
            PlateBounds = profile.Outline?.ToPath().Bounds ?? BoundingBox.Empty,
            TrackOffsets = offsets,
            TrackCentreDistances = [.. offsets.Skip(1).Select((offset, i) => offset - offsets[i])],
            ScrewHolePositions = [.. profile.ScrewHoles.Select(h => h.Center)],
            AsymmetricElements = AsymmetricElements(profile, tolerances),
        };
    }

    /// <summary>The elements of <paramref name="profile"/> that differ from their mirror image.</summary>
    public static FitDifferences AsymmetricElements(EndProfile profile, FitTolerances? tolerances = null)
    {
        tolerances ??= FitTolerances.Default;
        var mirrored = profile.MirroredX();
        var result = FitDifferences.None;
        if (EndProfileComparison.CompareTracks(profile.Tracks, mirrored.Tracks, tolerances.Joint) != FitDifferences.None)
            result |= FitDifferences.TrackPositions;
        if (!EndProfileComparison.MatchCircles(profile.ScrewHoles, mirrored.ScrewHoles, tolerances.Joint).IsComplete)
            result |= FitDifferences.ScrewHoles;
        if (!EndProfileComparison.MatchCircles(profile.Dowels, mirrored.Dowels, tolerances.Joint).IsComplete)
            result |= FitDifferences.Dowels;
        if (profile.GroundLine is not null && mirrored.GroundLine is not null &&
            !ShapeComparison.MaximumDeviation(profile.GroundLine.Tessellate(), mirrored.GroundLine.Tessellate(), tolerances.Landscape / 4).IsWithin(tolerances.Landscape))
            result |= FitDifferences.GroundLine;
        if (profile.Outline is not null && mirrored.Outline is not null &&
            !ShapeComparison.MaximumDeviation(profile.Outline.Tessellate(), mirrored.Outline.Tessellate(), tolerances.Joint / 2).IsWithin(tolerances.Joint))
            result |= FitDifferences.Outline;
        return result;
    }
}
