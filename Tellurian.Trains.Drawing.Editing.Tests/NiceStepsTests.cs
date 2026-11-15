using Tellurian.Trains.Drawing.Editing.Viewing;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class NiceStepsTests
{
    [TestMethod]
    [DataRow(1.0, 1.0), DataRow(1.2, 2.0), DataRow(3.0, 5.0), DataRow(7.0, 10.0), DataRow(0.03, 0.05), DataRow(240.0, 500.0), DataRow(1000.0, 1000.0)]
    public void AtLeast(double minimum, double expected) => Assert.AreEqual(expected, NiceSteps.AtLeast(minimum), expected * Precision);

    [TestMethod]
    [DataRow(1.0, 1.0), DataRow(1.9, 1.0), DataRow(4.9, 2.0), DataRow(9.9, 5.0), DataRow(0.07, 0.05), DataRow(600.0, 500.0)]
    public void AtMost(double maximum, double expected) => Assert.AreEqual(expected, NiceSteps.AtMost(maximum), expected * Precision);

    [TestMethod]
    public void GridLinesAreAtLeastTheMinimumApart()
    {
        foreach (var scale in new[] { 0.01, 0.13, 1, 7.5, 50 })
        {
            var pixels = NiceSteps.GridSpacing(scale) * scale;
            Assert.IsTrue(pixels >= NiceSteps.MinimumGridPixels - Precision && pixels < NiceSteps.MinimumGridPixels * 2.5 + Precision, $"scale {scale}: {pixels} px");
        }
    }

    [TestMethod]
    public void ScaleBarFitsItsSpace()
    {
        foreach (var scale in new[] { 0.01, 0.13, 1, 7.5, 50 })
        {
            var pixels = NiceSteps.ScaleBarLength(scale) * scale;
            Assert.IsTrue(pixels <= NiceSteps.ScaleBarPixels + Precision && pixels > NiceSteps.ScaleBarPixels / 2.5 - Precision, $"scale {scale}: {pixels} px");
        }
    }
}
