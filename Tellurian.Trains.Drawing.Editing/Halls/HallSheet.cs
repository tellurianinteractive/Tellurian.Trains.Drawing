using Tellurian.Trains.Drawing.Editing.EndProfiles;
using Tellurian.Trains.Drawing.Editing.Viewing;
using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.Halls;

namespace Tellurian.Trains.Drawing.Editing.Halls;

/// <summary>
/// A hall laid out for printing, in hall millimetres: the area to print, the text height for labels, the title
/// (the hall's name, with the address under it) and a scale bar, all below the hall, and lines of information, e.g.
/// the size and floor areas, large in the middle of the hall.
/// </summary>
public sealed record HallSheetLayout(BoundingBox Bounds, double TextSize, LabelPlacement Title, LabelPlacement? Subtitle, Point ScaleBarStart, double ScaleBarLength,
    IReadOnlyList<LabelPlacement> Information)
{
    /// <summary>The SVG <c>viewBox</c> of <see cref="Bounds"/>, for content inside a <c>scale(1,-1)</c> group.</summary>
    public ViewBox ViewBox => new(Bounds.Min.X, -Bounds.Max.Y, Bounds.Width, Bounds.Height);
}

/// <summary>
/// A hall on a landscape page, fitted to it whatever the editor's zoom (the same page as <see cref="EndProfileSheet"/>):
/// texts are sized for paper, and the title and a scale bar go below the hall.
/// </summary>
public static class HallSheet
{
    /// <summary>The height of labels on paper, mm.</summary>
    public const double PaperTextSize = 3;

    /// <summary>The height of the title on paper, mm.</summary>
    public const double PaperTitleSize = 6;

    /// <summary>The height of the information in the middle of the hall on paper, mm, unless it must be smaller to fit.</summary>
    public const double PaperInformationSize = 8;

    /// <summary>How much of the hall's width and height the information may take.</summary>
    private const double InformationShare = 0.8;

    /// <summary>The distance between lines of the information, relative to its text height.</summary>
    private const double LineSpacing = 1.5;

    /// <summary>The room under the hall for the title, the address and the scale bar, on paper, mm.</summary>
    private const double PaperRoomBelow = 22;

    /// <summary>The space around everything printed, on paper, mm.</summary>
    private const double PaperMargin = 2;

    /// <summary>The scale bar is about this share of the hall's width, rounded to a round length.</summary>
    private const double ScaleBarShare = 0.2;

    /// <summary>The round lengths a scale bar can have, mm.</summary>
    private static readonly double[] ScaleBarLengths = [500, 1000, 2000, 5000, 10000, 20000, 50000];

    /// <summary>
    /// Lays out <paramref name="hall"/> for a page, with <paramref name="information"/> (lines written by the host, in
    /// its language) centred in the hall.
    /// </summary>
    public static HallSheetLayout Create(Hall hall, IReadOnlyList<string>? information = null)
    {
        ArgumentNullException.ThrowIfNull(hall);
        var drawing = DrawingBounds(hall);
        var scale = Math.Min(EndProfileSheet.PageWidth / drawing.Width, (EndProfileSheet.PageHeight - PaperRoomBelow) / drawing.Height);
        var textSize = PaperTextSize / scale;
        var titleSize = PaperTitleSize / scale;
        var title = new LabelPlacement(hall.Name, new(drawing.Center.X, drawing.Min.Y - titleSize * 1.3), titleSize);
        var subtitle = string.IsNullOrWhiteSpace(hall.Address) ? null : new LabelPlacement(hall.Address, new(drawing.Center.X, title.Center.Y - titleSize * 1.2), textSize * 1.2);
        var barLength = ScaleBarLengths.LastOrDefault(l => l <= drawing.Width * ScaleBarShare, ScaleBarLengths[0]);
        var barStart = new Point(drawing.Min.X, title.Center.Y);
        var bounds = drawing.Union(title.Bounds).Including(barStart - new Vector(0, textSize * 2)).Including(barStart + new Vector(barLength, 0));
        if (subtitle is not null) bounds = bounds.Union(subtitle.Bounds);
        return new(bounds.Inflated(PaperMargin / scale), textSize, title, subtitle, barStart, barLength, Information(hall, information ?? [], PaperInformationSize / scale));
    }

    /// <summary>
    /// The lines centred on the middle of the hall's walls, one under the other, at <paramref name="size"/> unless the
    /// widest line or all of them together need a smaller size to fit well inside the walls' bounds.
    /// </summary>
    private static List<LabelPlacement> Information(Hall hall, IReadOnlyList<string> lines, double size)
    {
        if (lines.Count == 0) return [];
        var walls = hall.Outline.ToPath().Bounds;
        var widest = Math.Max(lines.Max(l => l.Length), 1);
        size = Math.Min(size, Math.Min(walls.Width * InformationShare / (widest * EndProfileLabel.CharacterWidth), walls.Height * InformationShare / (lines.Count * LineSpacing)));
        var step = size * LineSpacing;
        var top = walls.Center.Y + (lines.Count - 1) * step / 2;
        return [.. lines.Select((line, i) => new LabelPlacement(line, new(walls.Center.X, top - i * step), size))];
    }

    /// <summary>The walls, and the areas kept free in front of doors, which may reach outside the walls' bounds.</summary>
    public static BoundingBox DrawingBounds(Hall hall)
    {
        ArgumentNullException.ThrowIfNull(hall);
        return hall.Doors.Aggregate(hall.Outline.ToPath().Bounds, (box, door) => box.Union(door.KeepFreeZone().ToPath().Bounds));
    }
}
