using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.AutoDraw;

/// <summary>The result of <see cref="ModuleAutoDraw.Create"/>.</summary>
public sealed record AutoDrawResult
{
    public required AutoDrawStatus Status { get; init; }

    /// <summary>The drawn module; null when <see cref="Status"/> is <see cref="AutoDrawStatus.NeedsManualDrawing"/>.</summary>
    public Module? Module { get; init; }

    /// <summary>Every inferred value and every problem.</summary>
    public required IReadOnlyList<AutoDrawNote> Notes { get; init; }

    public bool IsDrawn => Module is not null;
}
