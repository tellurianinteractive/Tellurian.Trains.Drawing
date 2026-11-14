namespace Tellurian.Trains.Drawing.Model;

/// <summary>What a dimension measures (EP-10).</summary>
public enum DimensionKind
{
    /// <summary>The distance along the line between the points.</summary>
    Aligned,

    /// <summary>The difference in X.</summary>
    Horizontal,

    /// <summary>The difference in Y.</summary>
    Vertical,
}
