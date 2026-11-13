using Bunit;
using Tellurian.Trains.Drawing.App.Pages;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class HomeTests
{
    [TestMethod]
    public void ShowsAppNameAndVersion()
    {
        using var context = new BunitContext();

        var home = context.Render<Home>();

        Assert.AreEqual("Tellurian Trains Drawing", home.Find("h1").TextContent);
        StringAssert.StartsWith(home.Find(".version").TextContent, "Version 0.1.0");
    }
}
