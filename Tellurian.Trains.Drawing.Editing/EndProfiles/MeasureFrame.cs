using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Editing.EndProfiles;

/// <summary>
/// Translates between stored end-view coordinates (rail top and centre line) and the coordinates the user
/// measures in. Only what is shown and typed changes; drawings are always stored relative to rail top.
/// </summary>
public sealed record MeasureFrame(MeasureOrigin Kind, Point Origin)
{
    public static MeasureFrame RailTopCentre { get; } = new(MeasureOrigin.RailTopCentre, Point.Origin);

    /// <summary>The frame of <paramref name="kind"/> for <paramref name="profile"/>; a profile without a plate uses rail top.</summary>
    public static MeasureFrame For(EndProfile profile, MeasureOrigin kind) =>
        kind == MeasureOrigin.PlateLowerLeft && PlateShape.From(profile) is { } plate
            ? new(kind, new(plate.Left, plate.Bottom))
            : RailTopCentre;

    public Point ToDisplay(Point point) => new(point.X - Origin.X, point.Y - Origin.Y);
    public Point FromDisplay(Point point) => new(point.X + Origin.X, point.Y + Origin.Y);
    public double ToDisplayX(double x) => x - Origin.X;
    public double FromDisplayX(double x) => x + Origin.X;
    public double ToDisplayY(double y) => y - Origin.Y;
    public double FromDisplayY(double y) => y + Origin.Y;
}
