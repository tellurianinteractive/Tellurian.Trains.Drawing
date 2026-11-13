namespace Tellurian.Trains.Drawing.Model.AutoDraw;

/// <summary>
/// The data a module is auto-drawn from (ModuleDrawing §8.1). The properties map one-to-one to
/// Module Registry data, except the ones marked as new. Lengths in mm, angles in degrees.
/// </summary>
public sealed record AutoDrawParameters
{
    /// <summary>The module id to use; a new one is created if null.</summary>
    public Guid? ModuleId { get; init; }

    /// <summary>Registry <c>FullName</c>.</summary>
    public required string Name { get; init; }

    /// <summary>The standard's short name, e.g. "FREMO/H0-RE" or "MMM-2R".</summary>
    public required string Standard { get; init; }

    /// <summary>Registry <c>ConfigurationLabel</c>; the variant name.</summary>
    public string? ConfigurationLabel { get; init; }

    public int? RegistryModuleId { get; init; }

    public double? Straight { get; init; }
    public double? Radius { get; init; }
    public double? Angle { get; init; }

    /// <summary>Registry <c>Length</c>; only kept for comparison (MD-10).</summary>
    public double? Length { get; init; }

    /// <summary>The widest part of the module.</summary>
    public double? Width { get; init; }

    public int? NumberOfThroughTracks { get; init; }
    public int? NumberOfSections { get; init; }
    public IReadOnlyList<AutoDrawExit> Exits { get; init; } = [];

    public bool IsEndOfLine { get; init; }
    public bool IsDuckunder { get; init; }
    public bool IsTurntable { get; init; }

    /// <summary>The module is the operation place of a station (Registry <c>Station.PrimaryModule</c>).</summary>
    public bool IsOperationPlace { get; init; }

    /// <summary>New, not in the Registry.</summary>
    public CurveDirection CurveDirection { get; init; }

    /// <summary>New, not in the Registry.</summary>
    public StraightPosition StraightPosition { get; init; }

    /// <summary>New, not in the Registry.</summary>
    public WideningSide WideningSide { get; init; }

    /// <summary>New, not in the Registry. The angle at which a widened body runs out from the end plate.</summary>
    public double TaperAngle { get; init; } = 45;
}
