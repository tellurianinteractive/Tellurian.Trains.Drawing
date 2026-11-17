using Bunit;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class ModulePropertiesPaneTests
{
    private const int Prefix = 0, FremoNumber = 1, Name = 2, OwnerName = 3;

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

    private static void Enter(IRenderedComponent<DrawingPage> page, int field, string value) =>
        page.FindAll(".module-properties input")[field].Change(value);

    [TestMethod]
    public void IdentificationIsEditedInTheDraft()
    {
        using var context = new AppTestContext();
        var (page, id) = OpenNewModule(context);

        Enter(page, Prefix, "SFjä");
        Enter(page, FremoNumber, "12");
        Enter(page, OwnerName, "Stefan Fjällemark");

        // In the module editor, changes are autosaved to the draft a moment later.
        context.Time.Advance(Editing.Commands.AutosaveScheduler<Model.Files.ModuleFileContent>.DefaultDelay);
        page.WaitForAssertion(() => Assert.AreEqual("SFjä 012 Tellby", context.Workbench.Drafts[id].Name));
        var module = DrawingFileSerializer.DeserializeModule(context.Workbench.Drafts[id].Json).Module;
        Assert.AreEqual(12, module.FremoNumber);
        Assert.AreEqual(new Model.Modules.Owner { Name = "Stefan Fjällemark", Prefix = "SFjä" }, module.Owners.Single());
        Assert.IsTrue(context.Workbench.Drafts[id].HasUnsavedChanges);
        Assert.IsEmpty(page.FindAll(".module-problems"));
    }

    [TestMethod]
    public void ProblemsAreShownWhileFillingIn()
    {
        using var context = new AppTestContext();
        var (page, _) = OpenNewModule(context);

        Enter(page, FremoNumber, "12");
        page.WaitForAssertion(() => StringAssert.Contains(page.Find(".module-problems").TextContent, "Enter the owner's FREMO prefix with the FREMO number."));

        Enter(page, FremoNumber, "twelve");
        page.WaitForAssertion(() => StringAssert.Contains(page.Find(".module-problems").TextContent, "The FREMO number must be 1–9999."));

        Enter(page, FremoNumber, "");
        Enter(page, Name, "");
        page.WaitForAssertion(() => StringAssert.Contains(page.Find(".module-problems").TextContent, "Enter a name, or a FREMO number with the owner's prefix."));
    }
}
