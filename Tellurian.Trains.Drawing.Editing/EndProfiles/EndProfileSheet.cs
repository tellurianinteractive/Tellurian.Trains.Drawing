using Tellurian.Trains.Drawing.Editing.Viewing;
using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Editing.EndProfiles;

/// <summary>A drawing laid out for a page: its dimensions, title, text height and the area to print, in end-view millimetres.</summary>
public sealed record SheetLayout(IReadOnlyList<Dimension> Dimensions, LabelPlacement? Title, double TextSize, BoundingBox Bounds)
{
    /// <summary>The SVG <c>viewBox</c> of <see cref="Bounds"/>, for content inside a <c>scale(1,-1)</c> group.</summary>
    public ViewBox ViewBox => new(Bounds.Min.X, -Bounds.Max.Y, Bounds.Width, Bounds.Height);
}

/// <summary>
/// An end profile drawing on a page, independent of the editor's zoom: dimension text and distances are sized for
/// paper, and the printed area is just the drawing, its dimensions and its title.
/// </summary>
public static class EndProfileSheet
{
    /// <summary>The printable area of a landscape A4 page with 10 mm margins, mm.</summary>
    public const double PageWidth = 277, PageHeight = 190;

    /// <summary>The height of dimension text on paper, mm.</summary>
    public const double PaperTextSize = 3;

    /// <summary>The distance from the drawing to its dimension lines on paper, mm.</summary>
    public const double PaperGap = 8;

    /// <summary>The space around everything printed, on paper, mm.</summary>
    public const double PaperMargin = 2;

    /// <summary>The room a dimension's text takes beyond its line, relative to the dimension text height.</summary>
    public const double DimensionTextRoom = 1.6;

    /// <summary>
    /// The room the dimensions and title need around the drawing, relative to its size; it sets the first guess at
    /// the printing scale, which sizes them.
    /// </summary>
    private const double WidthRoom = 1.3, HeightRoom = 1.8;

    /// <summary>The area drawn when a profile has no drawing yet, mm.</summary>
    private const double EmptySize = 100;

    public static SheetLayout Create(EndProfile profile, MeasureFrame frame, bool details)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var drawing = DrawingBounds(profile);
        var scale = Math.Min(PageWidth / (drawing.Width * WidthRoom), PageHeight / (drawing.Height * HeightRoom));
        var textSize = PaperTextSize / scale;
        var dimensions = EndProfileDimensions.Create(profile, frame, PaperGap / scale, details);
        var title = EndProfileLabel.Place(profile, dimensions, textSize);
        return new(dimensions, title, textSize, Bounds(profile, dimensions, title, textSize).Inflated(PaperMargin / scale));
    }

    /// <summary>The area of the drawing, its dimensions with their text, and the title.</summary>
    public static BoundingBox Bounds(EndProfile profile, IEnumerable<Dimension> dimensions, LabelPlacement? title, double textSize)
    {
        ArgumentNullException.ThrowIfNull(dimensions);
        var bounds = DrawingBounds(profile);
        foreach (var dimension in dimensions)
        {
            var (start, end) = dimension.Line();
            bounds = bounds.Union(BoundingBox.FromPoints([start, end]).Inflated(textSize * DimensionTextRoom)).Including(dimension.Start).Including(dimension.End);
        }
        return title is null ? bounds : bounds.Union(title.Bounds);
    }

    /// <summary>The plate, ground line, rail top, tracks, holes and dowels.</summary>
    public static BoundingBox DrawingBounds(EndProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var box = BoundingBox.Empty.Including(Point.Origin);
        if (profile.Outline is { } outline) box = box.Union(outline.ToPath().Bounds);
        if (profile.GroundLine is { } ground) box = box.Union(ground.ToPath().Bounds);
        foreach (var track in profile.Tracks) box = box.Union(EndProfileScene.TrackBounds(track));
        foreach (var circle in profile.ScrewHoles.Concat(profile.Dowels))
            box = box.Union(BoundingBox.FromPoints([circle.Center]).Inflated(circle.Diameter / 2));
        return box.Width < Tolerance.Length || box.Height < Tolerance.Length ? box.Inflated(EmptySize / 2) : box;
    }
}
