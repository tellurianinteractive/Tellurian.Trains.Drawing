using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.EndProfiles;

/// <summary>Properties derived from an end profile drawing (spec EP-7).</summary>
public sealed record EndProfileProperties
{
    /// <summary>The bounds of the end-plate outline; empty when there is no outline.</summary>
    public required BoundingBox PlateBounds { get; init; }

    public double PlateWidth => PlateBounds.Width;
    public double PlateHeight => PlateBounds.Height;

    /// <summary>The track offsets from left to right.</summary>
    public required IReadOnlyList<double> TrackOffsets { get; init; }

    /// <summary>The centre distances (c/c) between neighbouring tracks, from left to right.</summary>
    public required IReadOnlyList<double> TrackCentreDistances { get; init; }

    public required IReadOnlyList<Point2> ScrewHolePositions { get; init; }

    /// <summary>The elements that differ from their mirror image; <see cref="FitDifferences.None"/> when symmetric.</summary>
    public required FitDifferences AsymmetricElements { get; init; }

    public int TrackCount => TrackOffsets.Count;

    /// <summary>
    /// True when the elements that decide fit (tracks, screw holes, ground line) equal their mirror image.
    /// Then the profile orientation at an exit doesn't matter.
    /// </summary>
    public bool IsSymmetric => (AsymmetricElements & ~(FitDifferences.Outline | FitDifferences.Dowels)) == FitDifferences.None;
}
