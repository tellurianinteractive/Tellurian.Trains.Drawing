namespace Tellurian.Trains.Drawing.Model.AutoDraw;

/// <summary>Where the straight part of a straight + curve module is, seen from the first exit.</summary>
public enum StraightPosition
{
    Before,
    After,

    /// <summary>Half before and half after the curve.</summary>
    Split,
}
