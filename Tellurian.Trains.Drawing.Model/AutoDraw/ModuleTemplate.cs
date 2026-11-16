using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.AutoDraw;

/// <summary>
/// The quick-create templates (MD-3): a straight, a curved and a straight + curve module with two exits,
/// "A" and "B". Each exit can have its own end profile, also of another standard of the same scale, e.g. FREMO at
/// one end and MMM at the other. Templates are drawn with auto-draw (MD-20).
/// </summary>
public sealed record ModuleTemplate
{
    public const string FirstExit = "A";
    public const string SecondExit = "B";

    public required ModuleTemplateKind Kind { get; init; }

    /// <summary>The module's name; may be empty when it has a FREMO number (<see cref="Modules.ModuleValidation"/>).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The owner's FREMO module number, 1–9999.</summary>
    public int? FremoNumber { get; init; }

    /// <summary>The owner, with the full name and the FREMO prefix as far as they are known.</summary>
    public Owner? Owner { get; init; }

    /// <summary>The standard the module follows.</summary>
    public required ModuleStandard Standard { get; init; }

    /// <summary>The end profile of exit A, and of exit B unless <see cref="SecondEndProfile"/> is given.</summary>
    public required EndProfile EndProfile { get; init; }

    /// <summary>The end profile of exit B; null uses <see cref="EndProfile"/>.</summary>
    public EndProfile? SecondEndProfile { get; init; }

    /// <summary>The module turns an asymmetric end profile over (<i>Endewender</i>).</summary>
    public bool IsEndProfileSwitcher { get; init; }

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
        var second = SecondEndProfile ?? EndProfile;
        var exit = new AutoDrawExit { Label = FirstExit, Direction = ExitDirection.West, EndProfile = EndProfile.Designation, EndProfileId = EndProfile.Id };
        var hasStraight = Kind is ModuleTemplateKind.Straight or ModuleTemplateKind.StraightAndCurve;
        var hasCurve = Kind is ModuleTemplateKind.Curve or ModuleTemplateKind.StraightAndCurve;
        return new()
        {
            Name = Name,
            FremoNumber = FremoNumber,
            Owners = Owner is null ? [] : [Owner],
            Standard = Standard.ShortName,
            Straight = hasStraight ? Straight : null,
            Radius = hasCurve ? Radius : null,
            Angle = hasCurve ? Angle : null,
            Width = Width,
            NumberOfThroughTracks = Math.Max(EndProfile.Tracks.Count, second.Tracks.Count),
            Exits = [exit, exit with { Label = SecondExit, Direction = ExitDirection.East, EndProfile = second.Designation, EndProfileId = second.Id }],
            IsEndProfileSwitcher = IsEndProfileSwitcher,
            CurveDirection = CurveDirection,
            StraightPosition = StraightPosition,
        };
    }

    /// <summary>Draws the module from this template with <paramref name="catalogue"/>.</summary>
    public AutoDrawResult Create(DrawingCatalogue catalogue) => ModuleAutoDraw.Create(ToParameters(), catalogue);
}
