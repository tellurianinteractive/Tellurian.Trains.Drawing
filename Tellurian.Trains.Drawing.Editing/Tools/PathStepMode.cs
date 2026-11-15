namespace Tellurian.Trains.Drawing.Editing.Tools;

/// <summary>What the next click of the path tool adds.</summary>
public enum PathStepMode
{
    Line,

    /// <summary>An arc that continues tangentially from the previous step to the clicked point.</summary>
    TangentArc,
}
