using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Editing.Viewing;

/// <summary>
/// The visible part of a drawing (AP-4). World coordinates are millimetres with Y up; screen coordinates are
/// CSS pixels with Y down, origin at the top left of the canvas. <see cref="Scale"/> is pixels per millimetre.
/// </summary>
public sealed record Viewport
{
    public const double MinimumScale = 0.002;
    public const double MaximumScale = 200;

    /// <summary>The world point at the centre of the canvas.</summary>
    public required Point Center { get; init; }

    /// <summary>Pixels per millimetre.</summary>
    public required double Scale { get; init; }

    /// <summary>The canvas width in pixels.</summary>
    public required double Width { get; init; }

    /// <summary>The canvas height in pixels.</summary>
    public required double Height { get; init; }

    /// <summary>A viewport of the given size that shows <paramref name="area"/> with a margin in pixels.</summary>
    public static Viewport Fit(BoundingBox area, double width, double height, double margin = 20)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        var usableWidth = Math.Max(width - 2 * margin, 1);
        var usableHeight = Math.Max(height - 2 * margin, 1);
        var scale = area.IsEmpty || (area.Width < Tolerance.Length && area.Height < Tolerance.Length)
            ? 1
            : Math.Min(usableWidth / Math.Max(area.Width, Tolerance.Length), usableHeight / Math.Max(area.Height, Tolerance.Length));
        return new() { Center = area.IsEmpty ? Point.Origin : area.Center, Scale = Clamp(scale), Width = width, Height = height };
    }

    /// <summary>The part of the world that is visible.</summary>
    public BoundingBox VisibleWorld
    {
        get
        {
            var halfWidth = Width / 2 / Scale;
            var halfHeight = Height / 2 / Scale;
            return new(new(Center.X - halfWidth, Center.Y - halfHeight), new(Center.X + halfWidth, Center.Y + halfHeight));
        }
    }

    /// <summary>
    /// The SVG <c>viewBox</c>, for content drawn in world coordinates inside a <c>scale(1,-1)</c> group,
    /// so that world Y points up on screen.
    /// </summary>
    public ViewBox ViewBox
    {
        get
        {
            var visible = VisibleWorld;
            return new(visible.Min.X, -visible.Max.Y, visible.Width, visible.Height);
        }
    }

    public Point ScreenToWorld(Point screen) =>
        new(Center.X + (screen.X - Width / 2) / Scale, Center.Y - (screen.Y - Height / 2) / Scale);

    public Point WorldToScreen(Point world) =>
        new(Width / 2 + (world.X - Center.X) * Scale, Height / 2 - (world.Y - Center.Y) * Scale);

    /// <summary>A length in pixels as millimetres, e.g. a snap tolerance.</summary>
    public double ToWorld(double pixels) => pixels / Scale;

    /// <summary>Zooms by <paramref name="factor"/> keeping the world point under <paramref name="screen"/> in place (wheel and pinch zoom).</summary>
    public Viewport ZoomAt(Point screen, double factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor);
        var anchor = ScreenToWorld(screen);
        var zoomed = this with { Scale = Clamp(Scale * factor) };
        var moved = zoomed.ScreenToWorld(screen);
        return zoomed with { Center = zoomed.Center + (anchor - moved) };
    }

    /// <summary>Moves the view by a drag of <paramref name="dx"/>, <paramref name="dy"/> pixels; the drawing follows the pointer.</summary>
    public Viewport PanBy(double dx, double dy) =>
        this with { Center = new(Center.X - dx / Scale, Center.Y + dy / Scale) };

    /// <summary>The same view on a canvas of another size, e.g. after the window was resized.</summary>
    public Viewport Resized(double width, double height) => this with { Width = width, Height = height };

    /// <summary>Shows <paramref name="area"/>, e.g. zoom to fit or zoom to selection.</summary>
    public Viewport ZoomTo(BoundingBox area, double margin = 20) => Fit(area, Width, Height, margin);

    private static double Clamp(double scale) => Math.Clamp(scale, MinimumScale, MaximumScale);
}
