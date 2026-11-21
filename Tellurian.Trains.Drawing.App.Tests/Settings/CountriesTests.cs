using Tellurian.Trains.Drawing.App.Settings;
using Tellurian.Trains.Drawing.App.Tests.Fakes;

namespace Tellurian.Trains.Drawing.App.Tests.Settings;

[TestClass]
public sealed class CountriesTests
{
    [TestMethod]
    public void TheRegistryCountriesAreEmbeddedByName()
    {
        Assert.HasCount(14, Countries.All);
        CollectionAssert.AreEqual(Countries.All.OrderBy(c => c.EnglishName, StringComparer.CurrentCultureIgnoreCase).ToArray(), Countries.All.ToArray());
        var sweden = Countries.Find(1)!;
        Assert.AreEqual(("Sweden", "SE"), (sweden.EnglishName, sweden.Code));
    }

    [TestMethod]
    public void CodesAreIsoCodesAlsoForTheUnitedKingdom()
    {
        Assert.AreEqual("GB", Countries.All.Single(c => c.EnglishName == "United Kingdom").Code);
        Assert.IsTrue(Countries.All.All(c => CatalogueValidation.IsCountryCode(c.Code)));
        Assert.AreEqual(Countries.All.Count, Countries.All.Select(c => c.Code).Distinct().Count());
    }

    [TestMethod]
    public async Task SettingsSurviveARestartAndAskTheBrowserToKeepThem()
    {
        var workbench = new InMemoryWorkbench();
        await new SettingsService(workbench).SaveAsync(new() { UserName = "Anna", CountryId = 1, ApiKey = "key" });

        var afterRestart = await new SettingsService(workbench).GetAsync();

        Assert.AreEqual(new UserSettings { UserName = "Anna", CountryId = 1, ApiKey = "key" }, afterRestart);
        Assert.AreEqual(1, workbench.PersistenceRequests);
    }

    [TestMethod]
    public async Task TheSettingsGiveTheCountryCode()
    {
        var settings = new SettingsService(new InMemoryWorkbench());
        Assert.IsNull(await settings.CountryCodeAsync());

        await settings.SaveAsync(new() { CountryId = 1 });

        Assert.AreEqual("SE", await settings.CountryCodeAsync());
    }
}
