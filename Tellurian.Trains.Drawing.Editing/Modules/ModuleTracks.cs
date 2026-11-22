using static Tellurian.Trains.Schedules.Model.Layouts.LayoutPartCopying;
using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Modules;
using Tellurian.Trains.Drawing.Model.Tracks;
using StationTrack = Tellurian.Trains.Schedules.Model.Layouts.StationTrack;

namespace Tellurian.Trains.Drawing.Editing.Modules;

/// <summary>
/// The commands that change a module's list of tracks (MD-8) and which track each track element is part of. After each change the lengths and stub marks that follow the drawing are worked out again,
/// with the fouling distance of the module's standard.
/// </summary>
/// <param name="standard">The module's standard; without it a track's length is the whole of it.</param>
/// <param name="catalogue">Finds the standard's end profiles that the module doesn't embed.</param>
public sealed class ModuleTracks(ModuleStandard? standard, Func<Guid, EndProfile?>? catalogue = null)
{
    /// <summary>
    /// Adds a track with <paramref name="designation"/> last in the list, with <paramref name="id"/> or else the lowest
    /// id that no track has. Trains are planned to use it until the user says otherwise.
    /// </summary>
    public IEditCommand<ModuleFileContent> Add(string designation, int? id = null) =>
        Change("Add track", module => module.With(c => c.Tracks = [
                .. module.Tracks,
                new()
                {
                    Id = id ?? StationTrackList.FreeId(module.Tracks),
                    Designation = designation.Trim(),
                    DisplayOrder = module.Tracks.Select(t => t.DisplayOrder).DefaultIfEmpty().Max() + 1,
                    IsScheduled = true,
                },
            ]));

    /// <summary>Removes a track from the list; its track elements stay in the drawing, part of no track.</summary>
    public IEditCommand<ModuleFileContent> Remove(int trackId) =>
        Change("Remove track", module => module.With(copy =>
        {
            copy.Tracks = [.. module.Tracks.Where(t => t.Id != trackId)];
            foreach (var element in copy.Drawing.TrackElements.Where(e => e.StationTrackId == trackId)) element.StationTrackId = null;
        }));

    /// <summary>Changes the properties of a track that don't follow the drawing.</summary>
    public IEditCommand<ModuleFileContent> Set(int trackId, Func<StationTrack, StationTrack> change) =>
        Change("Change track", module => WithTrack(module, trackId, change));

    /// <summary>Sets the length of a track, in metres; null lets it follow the drawing again.</summary>
    public IEditCommand<ModuleFileContent> SetLength(int trackId, double? metres) =>
        Change("Change track length", module => WithTrack(module, trackId, t => t.With(c =>
        {
            if (metres is { } length) c.Length = length;
            c.IsLengthEntered = metres is not null;
        })));

    /// <summary>Says whether a track is a stub; null lets it follow the drawing again.</summary>
    public IEditCommand<ModuleFileContent> SetStub(int trackId, bool? isStub) =>
        Change("Change track", module => WithTrack(module, trackId, t => t.With(c =>
        {
            if (isStub is { } stub) c.IsStub = stub;
            c.IsStubEntered = isStub is not null;
        })));

    /// <summary>Makes a track element part of a track of the list, or of none.</summary>
    public IEditCommand<ModuleFileContent> Assign(int elementId, int? trackId) =>
        Change("Change track of element", module =>
            trackId is { } id && module.Tracks.All(t => t.Id != id)
                ? module
                : module.With(copy =>
                {
                    foreach (var element in copy.Drawing.TrackElements.Where(e => e.Id == elementId)) element.StationTrackId = trackId;
                }));

    /// <summary>The module with its tracks measured again, e.g. after its shape was changed.</summary>
    public ModuleFileContent Measured(ModuleFileContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return content.WithMeasuredTracks(standard, catalogue);
    }

    private static Module WithTrack(Module module, int trackId, Func<StationTrack, StationTrack> change) =>
        module.With(copy => copy.Tracks = [.. module.Tracks.Select(t => t.Id == trackId ? change(t).With(c => c.Id = trackId) : t)]);

    private EditCommand<ModuleFileContent> Change(string description, Func<Module, Module> change) =>
        new(description, content =>
        {
            var changed = change(content.Module);
            return ReferenceEquals(changed, content.Module) ? content : Measured(content with { Module = changed });
        });
}
