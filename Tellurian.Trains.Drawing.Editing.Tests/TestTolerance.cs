global using Tellurian.Trains.Drawing.Geometry;
global using static Tellurian.Trains.Drawing.Editing.Tests.TestTolerance;

namespace Tellurian.Trains.Drawing.Editing.Tests;

/// <summary>Tolerances for asserting computed values in tests.</summary>
internal static class TestTolerance
{
    /// <summary>For values that are exact up to floating-point rounding.</summary>
    public const double Precision = 1e-9;
}
