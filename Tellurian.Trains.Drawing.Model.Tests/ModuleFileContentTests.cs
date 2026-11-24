using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class ModuleFileContentTests
{
    private static ModuleFileContent StraightModule() => ModuleFileContent.Create(TestModules.Straight(), TestCatalogue.Lookup);

    [TestMethod]
    public void EmbeddedEndProfilesThatChangedInTheCatalogueAreReplaced()
    {
        var content = StraightModule();
        var embedded = content.EndProfiles[0];
        var changed = embedded.With(edited => { edited.ShortName = "NEW"; });

        var (updatedContent, updated) = content.WithCurrentEndProfiles(id => id == changed.Id ? changed : TestCatalogue.Lookup(id));

        Assert.AreEqual("NEW", updatedContent.FindEndProfile(embedded.Id)?.ShortName);
        Assert.AreEqual(changed.Id, updated.Single().Id);
        Assert.AreSame(content.Module, updatedContent.Module, "The module itself is unchanged.");
    }

    [TestMethod]
    public void NothingIsReplacedWhenTheCatalogueHasTheSameEndProfiles()
    {
        var content = StraightModule();

        var (same, updated) = content.WithCurrentEndProfiles(id => TestCatalogue.Lookup(id) is { } profile ? profile.With(edited => {  }) : null);

        Assert.IsEmpty(updated);
        Assert.AreSame(content, same);
    }

    [TestMethod]
    public void EmbeddedEndProfilesMissingFromTheCatalogueAreKept()
    {
        var content = StraightModule();

        var (same, updated) = content.WithCurrentEndProfiles(_ => (EndProfile?)null);

        Assert.IsEmpty(updated);
        Assert.HasCount(content.EndProfiles.Count, same.EndProfiles);
    }
}
