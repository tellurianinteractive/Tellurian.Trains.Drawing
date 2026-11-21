using System.Globalization;
using Tellurian.Trains.Drawing.Model.Modules;
using StationTrack = Tellurian.Trains.Schedules.Model.Layouts.StationTrack;
using static Tellurian.Trains.Schedules.Model.Layouts.LayoutPartCopying;

namespace Tellurian.Trains.Drawing.Model.Tracks;

/// <summary>
/// Gives a module the list of tracks that every module has (spec MD-8): at least one track, a through track or a
/// track ending. Auto-draw knows which tracks it has drawn, so it can make the list. Tracks drawn by hand are put in
/// the list by the user.
/// </summary>
public static class StationTrackList
{
    /// <summary>
    /// The module with a list of tracks made from its generated tracks: one main track that trains are planned on per
    /// generated track element, called 1, 2 and so on in the order they were drawn, which is from the viewer's side
    /// of the module, with the same numbers as ids. Only for a module without a list whose track elements are all
    /// generated; otherwise unchanged, since the user's list isn't touched and tracks drawn by hand can't be told
    /// apart by rule.
    /// </summary>
    public static Module WithGeneratedTrackList(this Module module)
    {
        ArgumentNullException.ThrowIfNull(module);
        var drawing = module.Drawing;
        var elements = drawing.TrackElements.Where(e => e is not BufferStop).ToList();
        if (module.Tracks.Count > 0 || elements.Count == 0 || drawing.TrackElements.Any(e => !e.IsGenerated || e.StationTrackId is not null)) return module;
        var tracks = elements.Select((element, index) => (element.Id, Track: Track(index + 1))).ToDictionary(t => t.Id, t => t.Track);
        return module.With(m =>
        {
            m.Tracks = [.. tracks.Values];
            m.Drawing = drawing with
            {
                TrackElements = [.. drawing.TrackElements.Select(e => tracks.TryGetValue(e.Id, out var track) ? e with { StationTrackId = track.Id } : e)],
            };
        });
    }

    /// <summary>The lowest positive number that no track of <paramref name="tracks"/> has as its id.</summary>
    public static int FreeId(IEnumerable<StationTrack> tracks)
    {
        ArgumentNullException.ThrowIfNull(tracks);
        var used = tracks.Select(t => t.Id).ToHashSet();
        var id = 1;
        while (used.Contains(id)) id++;
        return id;
    }

    /// <summary>The lowest number that no track of <paramref name="tracks"/> is called, as a designation.</summary>
    public static string FreeDesignation(IEnumerable<StationTrack> tracks)
    {
        ArgumentNullException.ThrowIfNull(tracks);
        var used = tracks.Select(t => t.Designation.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var number = 1;
        while (used.Contains(Designation(number))) number++;
        return Designation(number);
    }

    private static StationTrack Track(int number) => new(number, Designation(number), isMain: true, isScheduled: true)
    {
        DisplayOrder = number,
    };

    private static string Designation(int number) => number.ToString(CultureInfo.InvariantCulture);
}
