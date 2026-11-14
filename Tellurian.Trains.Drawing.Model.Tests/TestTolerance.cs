global using Tellurian.Trains.Drawing.Geometry;
global using static Tellurian.Trains.Drawing.Model.Tests.TestTolerance;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>Tolerances for asserting computed values in tests.</summary>
internal static class TestTolerance
{
    /// <summary>For values that are exact up to floating-point rounding.</summary>
    public const double Precision = 1e-9;

    /// <summary>Values read from a drawing dimensioned in whole millimetres.</summary>
    public const double DrawingRounding = 0.5;

    /// <summary>The direction change a drawing's rounded values may cause where an arc meets a fillet, degrees.</summary>
    public const double ArcAngleRounding = 0.1;

    /// <summary>Relative difference allowed between areas computed from separately tessellated outlines.</summary>
    public const double RelativeArea = 1e-4;
}
