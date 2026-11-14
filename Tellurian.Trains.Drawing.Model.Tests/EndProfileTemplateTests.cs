using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class EndProfileTemplateTests
{
    /// <summary>A low plate (top at rail top, 104 mm deep) with the holes of a 195 mm plate.</summary>
    private static EndProfile LowPlate(params Point[] holes) => EndProfileTemplate.Create(new()
    {
        Designation = "B96-LOW",
        Scale = "H0",
        PlateWidth = 500,
        PlateTop = 0,
        PlateBottom = -104,
        Gauge = 16.5,
        ScrewHoles = holes,
        ScrewHoleDiameter = 8,
    });

    [TestMethod]
    public void HolesOutsideTheLowerPlateAreLeftOut()
    {
        var profile = LowPlate(new(-200, -40), new(200, -40), new(0, -60), new(-200, -150), new(200, -150));
        CollectionAssert.AreEquivalent(new[] { new Point(-200, -40), new Point(200, -40), new Point(0, -60) }, profile.ScrewHoles.Select(h => h.Center).ToArray());
    }

    [TestMethod]
    [DataRow(-14.0, true)]
    [DataRow(-13.9, false)]
    [DataRow(-90.0, true)]
    [DataRow(-90.1, false)]
    public void RimMustBeTenMillimetresFromTheTopAndBottomEdges(double y, bool kept) =>
        Assert.AreEqual(kept ? 1 : 0, LowPlate(new Point(0, y)).ScrewHoles.Count, $"y = {y}");

    [TestMethod]
    [DataRow(236.0, true)]
    [DataRow(236.1, false)]
    [DataRow(-236.1, false)]
    public void RimMustBeTenMillimetresFromTheSides(double x, bool kept) =>
        Assert.AreEqual(kept ? 1 : 0, LowPlate(new Point(x, -50)).ScrewHoles.Count, $"x = {x}");
}
