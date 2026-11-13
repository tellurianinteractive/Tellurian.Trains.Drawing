namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>The part of a module that people can pass under (MD-19, Registry flag <c>IsDuckunder</c>).</summary>
public sealed record DuckUnderArea
{
    /// <summary>A closed path in module coordinates; by default the whole outline.</summary>
    public required PathDrawing Area { get; init; }

    /// <summary>Clear height under the baseboard, mm, if known.</summary>
    public double? ClearHeight { get; init; }
}
