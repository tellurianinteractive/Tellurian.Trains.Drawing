using Tellurian.Trains.Drawing.Model.Modules;
using Tellurian.Trains.Schedules.Model.Geometry;

namespace Tellurian.Trains.Drawing.Model.Tracks;

/// <summary>Where a station track's designation is written in a module drawing.</summary>
/// <param name="TrackId">The id of the station track.</param>
/// <param name="Designation">The designation, e.g. "1a".</param>
/// <param name="Pose">The middle of the track's longest element, heading along it.</param>
public sealed record StationTrackLabel(int TrackId, string Designation, Pose Pose);

/// <summary>
/// The designations of a module's station tracks as they are written in its drawing: derived from the list and the
/// drawing, never stored, so a renamed or reassigned track is shown at once (decided 2026-11-21).
/// </summary>
public static class StationTrackLabels
{
    /// <summary>
    /// One label per station track with a designation and at least one track element: at the middle of the longest
    /// route of its longest element. Buffer stops don't count.
    /// </summary>
    public static IReadOnlyList<StationTrackLabel> Of(Module module)
    {
        ArgumentNullException.ThrowIfNull(module);
        return Of(module.Tracks, module.Drawing);
    }

    /// <summary>The labels of <paramref name="tracks"/> in <paramref name="drawing"/>, e.g. while an editor has changes not yet in the module.</summary>
    public static IReadOnlyList<StationTrackLabel> Of(IEnumerable<Tellurian.Trains.Schedules.Model.Layouts.StationTrack> tracks, ModuleDrawing drawing)
    {
        ArgumentNullException.ThrowIfNull(tracks);
        ArgumentNullException.ThrowIfNull(drawing);
        var labels = new List<StationTrackLabel>();
        foreach (var track in tracks.Where(t => !string.IsNullOrWhiteSpace(t.Designation)))
        {
            var element = drawing.TrackElements
                .Where(e => e.StationTrackId == track.Id && e is not BufferStop)
                .MaxBy(e => e.TrackLength);
            if (element?.Routes().MaxBy(r => r.Length) is not { } route || route.Length <= Tolerance.Length) continue;
            labels.Add(new(track.Id, track.Designation.Trim(), route.PoseAt(route.Length / 2)));
        }
        return labels;
    }
}
