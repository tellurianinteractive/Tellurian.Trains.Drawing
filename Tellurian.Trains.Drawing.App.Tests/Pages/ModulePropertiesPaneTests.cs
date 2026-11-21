using Bunit;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class ModulePropertiesPaneTests
{
    private const int Prefix = 0, Number = 1, Name = 2, Owners = 3, Package = 4;

    private static (IRenderedComponent<DrawingPage> Page, Guid DraftId) OpenNewModule(AppTestContext context)
    {
        var form = context.Render<NewModulePage>();
        form.Find("#name").Change("Tellby");
        form.Find("form").Submit();
        form.WaitForAssertion(() => Assert.HasCount(1, context.Workbench.Drafts));
        var id = context.Workbench.Drafts.Keys.Single();
        var page = context.Render<DrawingPage>(p => p.Add(x => x.Id, id));
        page.WaitForElement(".module-properties");
        return (page, id);
    }

    [TestMethod]
    public void TheFieldsArePrefixNumberNameOwnersAndPackage()
    {
        using var context = new AppTestContext();
        var (page, _) = OpenNewModule(context);
        CollectionAssert.AreEqual(
            new[] { "Prefix", "Number", "Name", "Owners", "Package" },
            page.FindAll(".module-properties label.property-label").Select(l => l.ChildNodes[0].TextContent.Trim()).ToArray());
    }

    private static void Enter(IRenderedComponent<DrawingPage> page, int field, string value) =>
        page.FindAll(".module-properties input")[field].Change(value);

    [TestMethod]
    public void IdentificationIsEditedInTheDraft()
    {
        using var context = new AppTestContext();
        var (page, id) = OpenNewModule(context);

        Enter(page, Prefix, "SFjä");
        Enter(page, Number, "12");
        Enter(page, Owners, " Stefan Fjällemark, Module Club ");

        // In the module editor, changes are autosaved to the draft a moment later.
        context.Time.Advance(Editing.Commands.AutosaveScheduler<ModuleFileContent>.DefaultDelay);
        page.WaitForAssertion(() => Assert.AreEqual("SFjä 0012", context.Workbench.Drafts[id].Name, "A module that is no operation location is named by its designation."));
        var module = DrawingFileSerializer.DeserializeModule(context.Workbench.Drafts[id].Json).Module;
        Assert.AreEqual(("SFjä", 12, "Stefan Fjällemark, Module Club"), (module.Prefix, module.Number, module.ModuleOwners));
        Assert.IsTrue(context.Workbench.Drafts[id].HasUnsavedChanges);
        Assert.IsEmpty(page.FindAll(".module-problems"));
    }

    [TestMethod]
    public void ThePackageIsTheBoxTheModuleIsTransportedIn()
    {
        using var context = new AppTestContext();
        var (page, id) = OpenNewModule(context);

        Enter(page, Package, " Box 3 ");
        context.Time.Advance(Editing.Commands.AutosaveScheduler<ModuleFileContent>.DefaultDelay);
        page.WaitForAssertion(() => Assert.AreEqual("Box 3", DrawingFileSerializer.DeserializeModule(context.Workbench.Drafts[id].Json).Module.PackageLabel));

        Enter(page, Package, "");
        context.Time.Advance(Editing.Commands.AutosaveScheduler<ModuleFileContent>.DefaultDelay);
        page.WaitForAssertion(() => Assert.IsNull(DrawingFileSerializer.DeserializeModule(context.Workbench.Drafts[id].Json).Module.PackageLabel));
    }

    [TestMethod]
    public void ProblemsAreShownWhileFillingIn()
    {
        using var context = new AppTestContext();
        var (page, _) = OpenNewModule(context);

        Enter(page, Number, "12");
        page.WaitForAssertion(() => StringAssert.Contains(page.Find(".module-problems").TextContent, "Enter the prefix with the number."));

        Enter(page, Number, "twelve");
        page.WaitForAssertion(() => StringAssert.Contains(page.Find(".module-problems").TextContent, "The number must be 1–9999."));

        Enter(page, Number, "");
        Enter(page, Name, "");
        page.WaitForAssertion(() => StringAssert.Contains(page.Find(".module-problems").TextContent, "Enter a name, or a number with the prefix."));
    }
}
