using Tellurian.Trains.Drawing.App.Catalogue;
using Tellurian.Trains.Drawing.App.Tests.Fakes;
using Tellurian.Trains.Drawing.Model.Catalogue;

namespace Tellurian.Trains.Drawing.App.Tests.Catalogue;

[TestClass]
public sealed class CatalogueServiceTests
{
    [TestMethod]
    public async Task WithoutUserEntriesTheCatalogueIsTheBuiltInOne()
    {
        var catalogue = await new CatalogueService(new InMemoryWorkbench()).GetAsync();
        Assert.HasCount(BuiltInCatalogue.Instance.Standards.Count, catalogue.Standards);
        Assert.HasCount(BuiltInCatalogue.Instance.EndProfiles.Count, catalogue.EndProfiles);
    }

    [TestMethod]
    public async Task UserEntriesAreStoredAndReplaceBuiltInOnes()
    {
        var workbench = new InMemoryWorkbench();
        var builtIn = BuiltInCatalogue.Instance.Standards[0];
        var user = DrawingCatalogue.Empty with { Standards = [builtIn with { MinimumClearance = 999 }] };
        await new CatalogueService(workbench).SaveUserAsync(user);

        var catalogue = await new CatalogueService(workbench).GetAsync();

        Assert.AreEqual(999, catalogue.FindStandard(builtIn.Id)!.MinimumClearance);
        Assert.HasCount(BuiltInCatalogue.Instance.Standards.Count, catalogue.Standards);
    }
}
