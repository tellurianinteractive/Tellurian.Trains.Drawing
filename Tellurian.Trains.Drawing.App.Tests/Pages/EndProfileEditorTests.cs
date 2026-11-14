using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Tellurian.Trains.Drawing.App.Catalogue;
using Tellurian.Trains.Drawing.App.Documents;
using Tellurian.Trains.Drawing.App.Editors;
using Tellurian.Trains.Drawing.App.Pages;
using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Tests.Pages;

[TestClass]
public sealed class EndProfileEditorTests
{
    [TestMethod]
    public void NewEndProfileFromTheTemplateOpensInTheEditor()
    {
        using var context = new AppTestContext();
        var page = context.Render<NewEndProfilePage>();

        page.Find("#designation").Change("MY-PROFILE");
        page.Find("#track-count").Change("2");
        page.Find("form").Submit();

        var draft = context.Workbench.Drafts.Values.Single();
        var profile = DrawingFileSerializer.DeserializeEndProfile(draft.Json);
        Assert.AreEqual(DrawingFileKind.EndProfile, draft.Kind);
        Assert.AreEqual("MY-PROFILE", profile.Designation);
        Assert.HasCount(2, profile.Tracks);
        Assert.IsTrue(profile.HasScrewHoles, "Copied from the standard's default profile.");
        Assert.EndsWith($"drawing/{draft.Id}", context.Services.GetRequiredService<NavigationManager>().Uri);
    }

    [TestMethod]
    public void TemplatePlateTopIsAtTheReferenceProfileCentreNotItsEmbankment()
    {
        using var context = new AppTestContext();
        var page = context.Render<NewEndProfilePage>();
        var top = double.Parse(page.Find("#plate-top").GetAttribute("value")!, System.Globalization.CultureInfo.InvariantCulture);
        Assert.IsTrue(top >= 0, "The top edge is at or below rail top.");
    }

    [TestMethod]
    public void DrawingPageShowsTheEditorWithDerivedProperties()
    {
        using var context = new AppTestContext();
        var profile = BuiltInCatalogue.Instance.EndProfiles[0].Copy("EDIT-ME");
        var draft = context.Services.GetRequiredService<DrawingDocuments>().CreateAsync(DrawingFileSerializer.Serialize(profile)).Result;

        var page = context.Render<DrawingPage>(p => p.Add(x => x.Id, draft.Id));

        page.WaitForElement("#symmetry");
        Assert.AreEqual(profile.ScrewHoles.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), page.Find("#screw-hole-count").TextContent);
        Assert.IsNotNull(page.Find("#save-to-catalogue"));
        Assert.IsNotNull(page.FindComponent<EndProfileEditor>());
    }

    [TestMethod]
    public void MirrorScrewHolesIsUndoable()
    {
        using var context = new AppTestContext();
        var source = BuiltInCatalogue.Instance.EndProfiles[0];
        var profile = source.Copy("ONE-SIDED") with { ScrewHoles = [.. source.ScrewHoles.Where(h => h.Center.X < -1)] };
        var draft = context.Services.GetRequiredService<DrawingDocuments>().CreateAsync(DrawingFileSerializer.Serialize(profile)).Result;
        var page = context.Render<DrawingPage>(p => p.Add(x => x.Id, draft.Id));
        page.WaitForElement("#mirror-holes");
        var before = page.Find("#screw-hole-count").TextContent;

        page.Find("#mirror-holes").Click();
        Assert.AreNotEqual(before, page.Find("#screw-hole-count").TextContent);
        page.Find("#undo").Click();
        Assert.AreEqual(before, page.Find("#screw-hole-count").TextContent);
    }

    [TestMethod]
    public async Task SaveToCatalogueStoresTheProfile()
    {
        using var context = new AppTestContext();
        var profile = BuiltInCatalogue.Instance.EndProfiles[0].Copy("TO-CATALOGUE");
        var draft = await context.Services.GetRequiredService<DrawingDocuments>().CreateAsync(DrawingFileSerializer.Serialize(profile));
        var page = context.Render<DrawingPage>(p => p.Add(x => x.Id, draft.Id));
        page.WaitForElement("#save-to-catalogue").Click();

        var user = await context.Services.GetRequiredService<CatalogueService>().GetUserAsync();
        Assert.AreEqual("TO-CATALOGUE", user.FindEndProfile(profile.Id)?.Designation);
    }

    [TestMethod]
    public async Task EditingACatalogueProfileReusesItsDraft()
    {
        using var context = new AppTestContext();
        var documents = context.Services.GetRequiredService<DrawingDocuments>();
        var json = DrawingFileSerializer.Serialize(BuiltInCatalogue.Instance.EndProfiles[0].Copy("MINE"));

        var first = await documents.EditAsync(json);
        Assert.IsFalse(first.HasUnsavedChanges);
        var changed = await documents.UpdateAsync(first.Id, json);
        var second = await documents.EditAsync(json);

        Assert.AreEqual(first.Id, second.Id);
        Assert.IsTrue(second.HasUnsavedChanges, "The existing draft with its changes, not a new one.");
        Assert.AreEqual(changed.Modified, second.Modified);
    }

    [TestMethod]
    public async Task UpdateByIdKeepsTheFileNameSetByASave()
    {
        using var context = new AppTestContext();
        var documents = context.Services.GetRequiredService<DrawingDocuments>();
        var json = DrawingFileSerializer.Serialize(BuiltInCatalogue.Instance.EndProfiles[0].Copy("SAVED"));
        var stale = await documents.CreateAsync(json);
        context.Files.SaveAsName = "saved.tdendprofile";
        await documents.SaveAsync(stale.Id, saveAs: true);

        var updated = await documents.UpdateAsync(stale.Id, json);

        Assert.AreEqual("saved.tdendprofile", updated.FileName);
    }
}
