using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// The module's name written on its surface (MD-23): the FREMO designation, e.g. <c>SFjä 7900</c>, or else the name,
/// with the first owner's full name in smaller text under it. It runs along the track, centred between the track ends,
/// and is placed on the side of the track that is above it as the text is read on screen. So when a module is turned
/// on screen, e.g. placed in a hall plan, the text still reads upright and stays above the track.
/// The text is as big as fits between the tracks and the module's side and within the module's length, up to a
/// maximum, so that it can be read in a hall plan without zooming in much.
/// </summary>
public static class ModuleNameLabel
{
    /// <summary>The largest title height, mm.</summary>
    public const double MaxTitleSize = 150;

    /// <summary>The smallest title height, mm; used also when even that doesn't fit.</summary>
    public const double MinTitleSize = 15;

    /// <summary>The height of the owner's name relative to the title.</summary>
    public const double OwnerPerTitleSize = 0.6;

    /// <summary>The space between the tracks, the text and the module's side, and between the lines, relative to the title size.</summary>
    public const double MarginPerTitleSize = 0.25;

    /// <summary>The width of an average character relative to the text height, to estimate how wide a text is.</summary>
    public const double CharacterWidthPerSize = 0.6;

    /// <summary>How much of the track length the text may take up.</summary>
    public const double LengthShare = 0.9;

    private const int FitIterations = 4;

    /// <summary>The lines to write, or null when the module has neither a FREMO designation nor a name.</summary>
    public static ModuleNameText? Text(Module module)
    {
        ArgumentNullException.ThrowIfNull(module);
        var title = module.FremoDesignation ?? (string.IsNullOrWhiteSpace(module.Name) ? null : module.Name.Trim());
        if (title is null) return null;
        var owner = module.Owners.Count > 0 && !string.IsNullOrWhiteSpace(module.Owners[0].Name) ? module.Owners[0].Name.Trim() : null;
        return new(title, owner);
    }

    /// <summary>Where and how big to write <paramref name="text"/> on <paramref name="variant"/>, in module coordinates.</summary>
    /// <param name="screenRotation">How much the module is turned on screen, counter-clockwise; zero in a module view.</param>
    /// <param name="trackHalfWidth">Half the width of a track, e.g. half the sleeper length, mm.</param>
    /// <param name="maxTitleSize">The largest title height, mm.</param>
    public static ModuleNamePlacement Place(ModuleVariant variant, ModuleNameText text, Angle screenRotation, double trackHalfWidth, double maxTitleSize = MaxTitleSize)
    {
        ArgumentNullException.ThrowIfNull(variant);
        ArgumentNullException.ThrowIfNull(text);
        var (middle, length) = Middle(variant);
        var rotation = UprightOnScreen(middle.Heading + screenRotation) - screenRotation;
        var up = Vector.FromAngle(rotation).LeftNormal;
        var side = up.Dot(middle.Left) >= 0 ? 1.0 : -1.0;
        var hasOwner = text.Owner is not null;
        var lines = 1 + (hasOwner ? MarginPerTitleSize + OwnerPerTitleSize : 0);
        var charactersPerSize = Math.Max(text.Title.Length, hasOwner ? text.Owner!.Length * OwnerPerTitleSize : 0) * CharacterWidthPerSize;
        var room = SideDistance(variant, middle, side);

        var size = Math.Max(maxTitleSize, MinTitleSize);
        var edge = 0.0;
        for (var i = 0; i < FitIterations; i++)
        {
            edge = FreeDistance(variant, middle, side, size * charactersPerSize / 2, trackHalfWidth, size * (lines + 2 * MarginPerTitleSize));
            var fitsAcross = (room - edge) / (lines + 2 * MarginPerTitleSize);
            var fitsAlong = length > 0 && charactersPerSize > 0 ? LengthShare * length / charactersPerSize : double.PositiveInfinity;
            var fitted = Math.Clamp(Math.Min(fitsAcross, fitsAlong), MinTitleSize, Math.Max(maxTitleSize, MinTitleSize));
            if (Math.Abs(fitted - size) < Tolerance.Length) break;
            size = fitted;
        }

        var margin = size * MarginPerTitleSize;
        var ownerSize = size * OwnerPerTitleSize;
        var bottom = edge + margin;
        var subtitle = hasOwner ? middle.Position + up * (bottom + ownerSize / 2) : (Point?)null;
        var title = middle.Position + up * (bottom + size * lines - size / 2);
        return new(title, subtitle, rotation, size, hasOwner ? ownerSize : null);
    }

    /// <summary>The middle of the main path, or of the longest track when there is none, and the length of that path.</summary>
    private static (Pose Middle, double Length) Middle(ModuleVariant variant)
    {
        var path = variant.MainPath?.ToPath()
            ?? variant.Tracks.SelectMany(t => t.Routes()).OrderByDescending(r => r.Length).FirstOrDefault();
        if (path is not null) return (path.PoseAt(path.Length / 2), path.Length);
        var exit = variant.Exits.Count > 0 ? variant.Exits[0].Pose : Pose.Origin;
        return (exit.Reversed(), 0);
    }

    /// <summary>The direction turned half a turn if needed, so that it is in (-90°, 90°] on screen and the text reads upright.</summary>
    private static Angle UprightOnScreen(Angle heading)
    {
        var degrees = heading.Signed.Degrees;
        return new(degrees > Angle.Quarter.Degrees ? degrees - Angle.Half.Degrees : degrees <= -Angle.Quarter.Degrees ? degrees + Angle.Half.Degrees : degrees);
    }

    /// <summary>The distance from the middle, on <paramref name="side"/> of the track, to the nearest side of the module; infinite without sides.</summary>
    private static double SideDistance(ModuleVariant variant, Pose middle, double side)
    {
        var nearest = double.PositiveInfinity;
        foreach (var path in variant.Sides.Select(s => s.Path.ToPath()))
        {
            var points = path.Tessellate().Points;
            for (var i = 1; i < points.Count; i++)
            {
                if (Across(points[i - 1], points[i], middle, side, 0) is { } range && range.Min > 0) nearest = Math.Min(nearest, range.Min);
            }
        }
        return nearest;
    }

    /// <summary>
    /// The distance from the middle, on <paramref name="side"/> of the track, to the first gap between tracks that is
    /// <paramref name="needed"/> wide, looking only at the tracks within <paramref name="halfWidth"/> along the track.
    /// </summary>
    private static double FreeDistance(ModuleVariant variant, Pose middle, double side, double halfWidth, double trackHalfWidth, double needed)
    {
        var occupied = new List<(double From, double To)>();
        foreach (var route in variant.Tracks.SelectMany(t => t.Routes()))
        {
            var points = route.Tessellate().Points;
            for (var i = 1; i < points.Count; i++)
            {
                if (Across(points[i - 1], points[i], middle, side, halfWidth) is { } range)
                    occupied.Add((range.Min - trackHalfWidth, range.Max + trackHalfWidth));
            }
        }
        var edge = 0.0;
        foreach (var (from, to) in occupied.Where(o => o.To > 0).OrderBy(o => o.From))
        {
            if (from - edge >= needed) break;
            edge = Math.Max(edge, to);
        }
        return edge;
    }

    /// <summary>The lateral range, on <paramref name="side"/>, of the part of the edge within <paramref name="halfWidth"/> along the track.</summary>
    private static (double Min, double Max)? Across(Point start, Point end, Pose middle, double side, double halfWidth)
    {
        var forward = middle.Forward;
        var left = middle.Left * side;
        var (a0, l0) = ((start - middle.Position).Dot(forward), (start - middle.Position).Dot(left));
        var (a1, l1) = ((end - middle.Position).Dot(forward), (end - middle.Position).Dot(left));
        if (Math.Max(a0, a1) < -halfWidth || Math.Min(a0, a1) > halfWidth) return null;
        if (Math.Abs(a1 - a0) < Tolerance.Length) return (Math.Min(l0, l1), Math.Max(l0, l1));
        double At(double a) => l0 + (l1 - l0) * (Math.Clamp(a, Math.Min(a0, a1), Math.Max(a0, a1)) - a0) / (a1 - a0);
        var (from, to) = (At(-halfWidth), At(halfWidth));
        return (Math.Min(from, to), Math.Max(from, to));
    }
}

/// <summary>The lines of a module's name label: the FREMO designation or the name, and the owner's full name.</summary>
public sealed record ModuleNameText(string Title, string? Owner);

/// <summary>
/// Where and how big a module's name label is, in module coordinates: the centres of the title and of the owner's
/// name under it, the direction the text reads in, and the text heights in mm.
/// </summary>
public readonly record struct ModuleNamePlacement(Point Title, Point? Subtitle, Angle Rotation, double TitleSize, double? SubtitleSize);
