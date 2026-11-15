using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.AutoDraw;

/// <summary>
/// The quick-create templates (MD-3): a straight, a curved and a straight + curve module with two exits,
/// "A" and "B", of the same end profile. Templates are drawn with auto-draw (MD-20).
/// </summary>
public sealed record ModuleTemplate
{
    public const string FirstExit = "A";
    public const string SecondExit = "B";

    public required ModuleTemplateKind Kind { get; init; }
    public required string Name { get; init; }

    /// <summary>The standard the module follows.</summary>
    public required ModuleStandard Standard { get; init; }

    /// <summary>The end profile of both exits; it gives the number of through tracks.</summary>
    public required EndProfile EndProfile { get; init; }

    /// <summary>The length of a straight module, or of the straight part of a straight + curve module, mm.</summary>
    public double Straight { get; init; }

    public double Radius { get; init; }

    /// <summary>The angle of the curve, degrees.</summary>
    public double Angle { get; init; }

    /// <summary>The module's width; null uses the end plate's width.</summary>
    public double? Width { get; init; }

    public CurveDirection CurveDirection { get; init; }
    public StraightPosition StraightPosition { get; init; }

    /// <summary>The auto-draw parameters for this template.</summary>
    public AutoDrawParameters ToParameters()
    {
        var exit = new AutoDrawExit { Label = FirstExit, Direction = ExitDirection.West, EndProfile = EndProfile.Designation, EndProfileId = EndProfile.Id };
        var hasStraight = Kind is ModuleTemplateKind.Straight or ModuleTemplateKind.StraightAndCurve;
        var hasCurve = Kind is ModuleTemplateKind.Curve or ModuleTemplateKind.StraightAndCurve;
        return new()
        {
            Name = Name,
            Standard = Standard.ShortName,
            Straight = hasStraight ? Straight : null,
            Radius = hasCurve ? Radius : null,
            Angle = hasCurve ? Angle : null,
            Width = Width,
            NumberOfThroughTracks = EndProfile.Tracks.Count,
            Exits = [exit, exit with { Label = SecondExit, Direction = ExitDirection.East }],
            CurveDirection = CurveDirection,
            StraightPosition = StraightPosition,
        };
    }

    /// <summary>Draws the module from this template with <paramref name="catalogue"/>.</summary>
    public AutoDrawResult Create(DrawingCatalogue catalogue) => ModuleAutoDraw.Create(ToParameters(), catalogue);
}
