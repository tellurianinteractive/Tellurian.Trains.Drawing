using Tellurian.Trains.Drawing.Model.Catalogue;

namespace Tellurian.Trains.Drawing.Model.AutoDraw;

/// <summary>
/// Draws a module from the data the Module Registry has (spec MD-20, ModuleDrawing §8).
/// UI-independent, so the Registry can run it on its server and store the module JSON.
/// </summary>
public static class ModuleAutoDraw
{
    /// <summary>Recorded on every auto-drawn variant.</summary>
    public const string GeneratorVersion = "1.0";

    /// <summary>The Registry end profile designation for joints between sections.</summary>
    public const string InternalEndProfile = "Internal";

    /// <summary>Distance from a buffer stop to the back edge of an end-of-line module, mm.</summary>
    public const double BufferStopMargin = 50;

    /// <summary>Gap between provisionally placed junction exits, beyond their plate width, mm.</summary>
    public const double ProvisionalExitGap = 100;

    /// <summary>Reverse-curve radius when the standard has none, mm.</summary>
    public const double FallbackReverseCurveRadius = 1000;

    public static AutoDrawResult Create(AutoDrawParameters parameters, DrawingCatalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(catalogue);
        return new AutoDrawGenerator(parameters, catalogue).Run();
    }
}
