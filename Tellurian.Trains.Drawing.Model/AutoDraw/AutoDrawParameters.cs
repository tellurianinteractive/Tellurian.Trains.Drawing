namespace Tellurian.Trains.Drawing.Model.AutoDraw;

/// <summary>
/// The data a module is auto-drawn from (ModuleDrawing §8.1). The properties map one-to-one to
/// Module Registry data, except the ones marked as new. Lengths in mm, angles in degrees.
/// </summary>
public sealed record AutoDrawParameters : ExtensibleRecord
{
    /// <summary>The module id to use; a new one is created if null.</summary>
    public Guid? ModuleId { get; init; }

    /// <summary>Registry <c>FullName</c>; may be empty for a module identified by its FREMO designation.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The owner's number of the module (Registry <c>FremoNumber</c>).</summary>
    public int? Number { get; init; }

    /// <summary>The letters the owner marks modules with, e.g. <c>SFjä</c>; with the number it forms the designation.</summary>
    public string? Prefix { get; init; }

    /// <summary>The names of all owners in one text, separated by commas.</summary>
    public string? ModuleOwners { get; init; }

    /// <summary>Tells apart the ways one module can be set up, e.g. "Short" (Registry <c>ConfigurationLabel</c>).</summary>
    public string? ConfigurationLabel { get; init; }

    /// <summary>The standard's short name, e.g. "FREMO/H0-RE" or "MMM-2R".</summary>
    public required string Standard { get; init; }

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

    /// <summary>
    /// The module turns an asymmetric end profile over (<i>Endewender</i>, Registry flag), so an asymmetric profile
    /// isn't mirrored at the far exits (ModuleDrawing §4.3).
    /// </summary>
    public bool IsEndProfileSwitcher { get; init; }

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
