using Tellurian.Trains.Drawing.Model.Catalogues;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using References = Tellurian.Trains.Schedules.Model.Layouts.ExternalReferences;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class StandardMergeTests
{
    private static readonly Guid RegistryId = Guid.Parse("68532471-8a48-5f33-a674-03775cb74904");

    /// <summary>The user's FREMO standard, with values the Registry doesn't send, and an end profile and an add-on of it.</summary>
    private static Catalogue User => TestCatalogue.Instance with
    {
        EndProfiles = [.. TestCatalogue.Instance.EndProfiles.Select(p => p.With(e => e.Standards = [new(TestCatalogue.FremoId, "FREMO/H0-RE")]))],
        AddOns = [new() { Id = Guid.NewGuid(), ShortName = "FREMO-EXTRA", BaseStandardId = TestCatalogue.FremoId }],
    };

    /// <summary>The Registry's FREMO standard: another id, its own values, its reference, and none of the user's defaults.</summary>
    private static ModuleStandard Registry => new()
    {
        Id = RegistryId,
        ShortName = "fremo/h0-re",
        Scale = new() { ShortName = "H0", Denominator = 87 },
        TrackSystem = "CODE 75-100",
        ExternalReferences = References.WithModuleRegistryId(null, References.StandardId, 2),
    };

    private static Catalogue Imported(params ModuleStandard[] standards) => Catalogue.Empty with { Standards = standards };

    [TestMethod]
    public void TheRegistrysStandardReplacesTheUsersWithTheSameName()
    {
        var applied = EndProfileImport.Apply(User, Imported(Registry), []);

        var fremo = applied.Standards.Where(s => s.ShortName.Equals("FREMO/H0-RE", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.HasCount(1, fremo, "Never two standards with the same name.");
        var merged = fremo[0];
        Assert.AreEqual((RegistryId, "fremo/h0-re", "CODE 75-100"), (merged.Id!.Value, merged.ShortName, merged.TrackSystem), "The Registry's values take precedence.");
        Assert.AreEqual(2, References.ModuleRegistryId(merged.ExternalReferences, References.StandardId));
        Assert.AreEqual(TestCatalogue.FremoSingle.Id, merged.DefaultSingleTrackEndProfileId, "What the Registry doesn't give is kept from the user's.");
        Assert.IsNull(applied.FindStandard(TestCatalogue.FremoId));
    }

    [TestMethod]
    public void EndProfilesAndAddOnsOfTheReplacedStandardFollow()
    {
        var applied = EndProfileImport.Apply(User, Imported(Registry), []);

        Assert.IsTrue(applied.EndProfiles.All(p => p.Standards.Single().Id == RegistryId));
        Assert.AreEqual("fremo/h0-re", applied.EndProfiles[0].Standards[0].Name);
        Assert.AreEqual(RegistryId, applied.AddOns.Single().BaseStandardId);
        Assert.IsNotEmpty(applied.EndProfilesOf(RegistryId));
    }

    [TestMethod]
    public void TwoStandardsWithTheSameNameBecomeOne()
    {
        // As after a fetch in 0.11.0, which added the Registry's beside the user's.
        var user = User with { Standards = [.. User.Standards, Registry] };

        var applied = EndProfileImport.Apply(user, Imported(Registry.With(s => s.Wheelset = "RP25")), []);

        var merged = applied.Standards.Single(s => s.ShortName.Equals("FREMO/H0-RE", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual((RegistryId, "RP25"), (merged.Id!.Value, merged.Wheelset));
        Assert.IsTrue(applied.EndProfiles.All(p => p.Standards.Single().Id == RegistryId));
    }

    [TestMethod]
    public void TheCountriesAreTheRegistrysAlsoWhenItHasNone()
    {
        var user = User with { Standards = [.. User.Standards.Select(s => s.Id == TestCatalogue.FremoId ? s.With(e => e.Countries = ["SE"]) : s)] };

        var applied = EndProfileImport.Apply(user, Imported(Registry), []);

        Assert.IsNull(applied.FindStandard(RegistryId)!.Countries, "None from the Registry means everywhere.");
    }

    [TestMethod]
    public void AStandardWithTheSameIdIsReplacedAndFilledIn()
    {
        var update = Registry.With(s => { s.Id = TestCatalogue.FremoId; s.ShortName = "FREMO/H0-RE"; });

        var applied = EndProfileImport.Apply(User, Imported(update), []);

        var merged = applied.FindStandard(TestCatalogue.FremoId)!;
        Assert.AreEqual(("CODE 75-100", TestCatalogue.FremoSingle.Id), (merged.TrackSystem, merged.DefaultSingleTrackEndProfileId));
        Assert.AreEqual(User.Standards.Count, applied.Standards.Count);
    }

    [TestMethod]
    public void AnExtensionIsOfferedTheEndProfilesOfTheStandardItExtends()
    {
        var extension = new ModuleStandard { Id = Guid.NewGuid(), ShortName = "FREMO/E", Scale = new() { ShortName = "H0" }, ExtendsStandardId = RegistryId };

        var applied = EndProfileImport.Apply(User, Imported(Registry, extension), []);

        Assert.IsNotEmpty(applied.EndProfilesOf(extension.Id!.Value));
        CollectionAssert.AreEquivalent(applied.EndProfilesOf(RegistryId).ToList(), applied.EndProfilesOf(extension.Id!.Value).ToList());
    }

    [TestMethod]
    public void AUsersStandardThatExtendsAReplacedOneFollows()
    {
        var own = new ModuleStandard { Id = Guid.NewGuid(), ShortName = "MY-FREMO", Scale = new() { ShortName = "H0" }, ExtendsStandardId = TestCatalogue.FremoId };

        var applied = EndProfileImport.Apply(User with { Standards = [.. User.Standards, own] }, Imported(Registry), []);

        Assert.AreEqual(RegistryId, applied.FindStandard(own.Id!.Value)!.ExtendsStandardId);
    }

    [TestMethod]
    public void AMergedEndProfileGetsTheMergedStandard()
    {
        var drawn = TestCatalogue.FremoSingle.With(p => p.Standards = [new(TestCatalogue.FremoId, "FREMO/H0-RE")]);
        var shapeless = new EndProfile
        {
            Id = Guid.NewGuid(), Designation = drawn.Designation, Scale = drawn.Scale, Standards = [new(RegistryId, "FREMO/H0-RE")],
            ExternalReferences = References.WithModuleRegistryId(null, References.EndProfileId, 42),
        };
        var user = User.With(drawn);

        var applied = EndProfileImport.Apply(user, Imported(Registry), EndProfileImport.Compare([shapeless], user));

        var merged = applied.FindEndProfile(shapeless.Id)!;
        Assert.IsTrue(merged.HasOutline());
        Assert.AreEqual(RegistryId, merged.Standards.Single().Id);
    }
}
