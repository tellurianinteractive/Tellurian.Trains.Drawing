using System.Globalization;

namespace Tellurian.Trains.Drawing.App.Tests;

/// <summary>UI tests assert English texts, whatever the culture of the machine that runs them.</summary>
[TestClass]
public static class TestCulture
{
    [AssemblyInitialize]
    public static void UseEnglish(TestContext _)
    {
        var english = CultureInfo.GetCultureInfo("en");
        CultureInfo.DefaultThreadCurrentCulture = english;
        CultureInfo.DefaultThreadCurrentUICulture = english;
        CultureInfo.CurrentCulture = english;
        CultureInfo.CurrentUICulture = english;
    }
}
