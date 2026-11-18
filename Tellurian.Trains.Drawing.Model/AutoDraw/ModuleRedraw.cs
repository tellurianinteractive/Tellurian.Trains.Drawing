using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.AutoDraw;

/// <summary>
/// Changes to a drawn module's ends and shape: another end profile at an exit, an end plate turned the other side out,
/// or a curve turning the other way. A new end profile or curve direction redraws the module from the parameters it was
/// drawn from (<see cref="ModuleVariant.AutoDraw"/>); what was done to it since, its identification and its section
/// joints, is kept. Joints keep their place relative to the main path, which runs along the centre of the end plates,
/// and are mirrored with a flipped curve. All of it works on the default variant.
/// </summary>
public static class ModuleRedraw
{
    /// <summary>Whether the module can be redrawn: it was drawn from parameters.</summary>
    public static bool CanRedraw(ModuleFileContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return content.Module.DefaultVariant.AutoDraw is not null;
    }

    /// <summary>Whether the module curves, so that its curve can be flipped.</summary>
    public static bool HasCurve(ModuleFileContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return content.Module.DefaultVariant.AutoDraw?.Parameters is { } p && (p.Angle is > 0 || p.Radius is > 0);
    }

    /// <summary>Whether the plate at an exit with <paramref name="profile"/> can be turned: the profile is asymmetric.</summary>
    public static bool CanFlip(EndProfile? profile) => profile is not null && !EndProfileAnalysis.Analyze(profile).IsSymmetric;

    /// <summary>
    /// Whether redrawing moves the bends of section joints relative to the tracks: some joint is bent, and one of the
    /// end profiles, now or after the change, has its tracks off the centre of the plate. With centred tracks the bends
    /// keep their place.
    /// </summary>
    /// <param name="newProfile">An end profile the module is about to get, if any.</param>
    public static bool BendsWillChange(ModuleFileContent content, EndProfile? newProfile = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        var variant = content.Module.DefaultVariant;
        if (!variant.InternalJoints.Any(j => j.Bends is { Count: > 0 })) return false;
        var profiles = variant.Exits.Select(e => content.FindEndProfile(e.EndProfile.Id)).Append(newProfile).OfType<EndProfile>();
        return profiles.Any(p => EndProfileAnalysis.AsymmetricElements(p).HasFlag(FitDifferences.TrackPositions));
    }

    /// <summary>
    /// The module with <paramref name="profile"/> at exit <paramref name="exitLabel"/>, redrawn; null if it can't be
    /// redrawn or doesn't have that exit. The exit's plate gets the orientation the drawing gives it; the others keep theirs.
    /// </summary>
    public static ModuleFileContent? WithEndProfile(ModuleFileContent content, string exitLabel, EndProfile profile, DrawingCatalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(catalogue);
        if (content.Module.DefaultVariant.AutoDraw?.Parameters is not { } parameters || !parameters.Exits.Any(e => e.Label == exitLabel)) return null;
        var changed = parameters with
        {
            Exits = [.. parameters.Exits.Select(e => e.Label == exitLabel ? e with { EndProfile = profile.Designation, EndProfileId = profile.Id } : e)],
        };
        return Redraw(content, changed, catalogue.With(profile), mirrored: false, keepOrientation: label => label != exitLabel);
    }

    /// <summary>The module with its curve turning the other way, redrawn; null if it can't be redrawn or has no curve.</summary>
    public static ModuleFileContent? WithCurveFlipped(ModuleFileContent content, DrawingCatalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(catalogue);
        if (!HasCurve(content) || content.Module.DefaultVariant.AutoDraw?.Parameters is not { } parameters) return null;
        var flipped = parameters with { CurveDirection = parameters.CurveDirection == CurveDirection.Left ? CurveDirection.Right : CurveDirection.Left };
        return Redraw(content, flipped, catalogue, mirrored: true, keepOrientation: _ => true);
    }

    /// <summary>
    /// The module with the plate at exit <paramref name="exitLabel"/> turned the other side out, and whether it turns an
    /// asymmetric end profile over (<see cref="Module.IsEndProfileSwitcher"/>) following; null without that exit.
    /// </summary>
    public static ModuleFileContent? WithEndFlipped(ModuleFileContent content, string exitLabel)
    {
        ArgumentNullException.ThrowIfNull(content);
        var variant = content.Module.DefaultVariant;
        if (variant.Exit(exitLabel) is null) return null;
        var exits = variant.Exits.Select(e => e.Label == exitLabel ? e with { Orientation = Flipped(e.Orientation) } : e).ToList();
        return WithVariant(content, variant with { Exits = exits }, content.EndProfiles);
    }

    /// <summary>
    /// Whether a module with these exits turns an asymmetric end profile over: it has two exits, both asymmetric, with
    /// their plates the same side out. A plate at the far end faces the other way, so the same orientation puts the
    /// asymmetric feature on the other side of the track.
    /// </summary>
    public static bool IsEndProfileSwitcher(IReadOnlyList<ModuleExit> exits, Func<Guid, EndProfile?> endProfiles)
    {
        ArgumentNullException.ThrowIfNull(exits);
        ArgumentNullException.ThrowIfNull(endProfiles);
        return exits is [var first, var second]
            && CanFlip(endProfiles(first.EndProfile.Id)) && CanFlip(endProfiles(second.EndProfile.Id))
            && first.Orientation == second.Orientation;
    }

    private static ProfileOrientation Flipped(ProfileOrientation orientation) =>
        orientation == ProfileOrientation.Normal ? ProfileOrientation.Mirrored : ProfileOrientation.Normal;

    private static ModuleFileContent? Redraw(ModuleFileContent content, AutoDrawParameters parameters, DrawingCatalogue catalogue, bool mirrored, Func<string, bool> keepOrientation)
    {
        var old = content.Module.DefaultVariant;
        if (ModuleAutoDraw.Create(parameters with { ModuleId = content.Module.Id }, catalogue).Module is not { } drawn) return null;
        EndProfile? Lookup(Guid id) => catalogue.FindEndProfile(id) ?? content.FindEndProfile(id);
        var redrawn = drawn.DefaultVariant;
        var exits = redrawn.Exits
            .Select(e => keepOrientation(e.Label) && old.Exit(e.Label) is { } before ? e with { Orientation = before.Orientation } : e)
            .ToList();
        var variant = old with
        {
            Exits = exits,
            MainPath = redrawn.MainPath,
            Sides = redrawn.Sides,
            Sections = redrawn.Sections,
            InternalJoints = redrawn.InternalJoints,
            Tracks = redrawn.Tracks,
            OperatorSpaces = redrawn.OperatorSpaces,
            PublicSides = redrawn.PublicSides,
            DuckUnderAreas = redrawn.DuckUnderAreas,
            Labels = redrawn.Labels,
            RegistryMeasures = redrawn.RegistryMeasures,
            AutoDraw = redrawn.AutoDraw,
        };
        variant = WithJointsFrom(old, variant, mirrored, Lookup);
        var profiles = variant.Exits.Select(e => e.EndProfile.Id).Distinct().Select(Lookup).OfType<EndProfile>().ToList();
        return WithVariant(content, variant, profiles);
    }

    /// <summary>
    /// <paramref name="variant"/> with the joints of <paramref name="old"/> moved onto it: each point keeps its distance
    /// along the main path and its offset from it, mirrored when <paramref name="mirrored"/>, and the ends are put on
    /// the new outline. A joint that no longer cuts the module is drawn straight across the track where it crossed it,
    /// or left out if even that doesn't fit.
    /// </summary>
    private static ModuleVariant WithJointsFrom(ModuleVariant old, ModuleVariant variant, bool mirrored, Func<Guid, EndProfile?> endProfiles)
    {
        if (old.InternalJoints.Count == 0 || old.MainPath?.ToPath() is not { } oldMain || variant.MainPath?.ToPath() is not { } newMain) return variant;
        SegmentPath? outline;
        try
        {
            outline = ModuleGeometry.Outline(variant, endProfiles);
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            outline = null;
        }
        if (outline is null) return variant;
        var accepted = new List<InternalJoint>();
        var result = ModuleSections.WithJoints(variant, outline, []) ?? variant;
        foreach (var joint in old.InternalJoints)
        {
            var points = joint.Points.Select(p => Moved(p, oldMain, newMain, mirrored)).ToList();
            points[0] = ModuleSections.OnEdge(outline, points[0]);
            points[^1] = ModuleSections.OnEdge(outline, points[^1]);
            var moved = joint with { Start = points[0], End = points[^1], Bends = points.Count > 2 ? [.. points.Skip(1).Take(points.Count - 2)] : null };
            InternalJoint? straight = ModuleSections.AcrossAt(outline, variant.MainPath!, ModuleSections.DistanceAlongTrack(joint, old.MainPath!));
            foreach (var candidate in new[] { moved, straight }.OfType<InternalJoint>())
            {
                if (ModuleSections.WithJoints(variant, outline, [.. accepted, candidate]) is not { } cut) continue;
                accepted.Add(candidate);
                result = cut;
                break;
            }
        }
        return result;
    }

    private static Point Moved(Point point, SegmentPath oldMain, SegmentPath newMain, bool mirrored)
    {
        var projection = oldMain.Project(point);
        var offset = (point - projection.Pose.Position).Dot(projection.Pose.Left);
        var pose = newMain.PoseAt(Math.Clamp(projection.Distance, 0, newMain.Length));
        return pose.Position + pose.Left * (mirrored ? -offset : offset);
    }

    private static ModuleFileContent WithVariant(ModuleFileContent content, ModuleVariant variant, IReadOnlyList<EndProfile> profiles)
    {
        EndProfile? Lookup(Guid id) => profiles.FirstOrDefault(p => p.Id == id) ?? content.FindEndProfile(id);
        var isSwitcher = IsEndProfileSwitcher(variant.Exits, Lookup);
        if (variant.AutoDraw is { } record) variant = variant with { AutoDraw = record with { Parameters = record.Parameters with { IsEndProfileSwitcher = isSwitcher } } };
        var module = content.Module with
        {
            IsEndProfileSwitcher = isSwitcher,
            Variants = [.. content.Module.Variants.Select(v => v.Id == variant.Id ? variant : v)],
        };
        try
        {
            return ModuleFileContent.Create(module, Lookup);
        }
        catch (KeyNotFoundException)
        {
            return content with { Module = module };
        }
    }
}
