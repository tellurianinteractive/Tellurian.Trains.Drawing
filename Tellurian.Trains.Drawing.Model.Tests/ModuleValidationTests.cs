using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.Tests;

[TestClass]
public sealed class ModuleValidationTests
{
    private static ModuleProblemCode[] Codes(string? name, string? prefix, int? number) =>
        [.. ModuleValidation.Validate(name, prefix, number).Select(p => p.Code)];

    [TestMethod]
    [DataRow("Tellby", null, null, DisplayName = "Name only")]
    [DataRow("", "SFjä", 12, DisplayName = "FREMO designation only")]
    [DataRow(null, "SFjä", 9999, DisplayName = "Highest FREMO number")]
    [DataRow("Tellby", "SFjä", 1, DisplayName = "Both")]
    [DataRow("Tellby", "SFjä", null, DisplayName = "Prefix without number")]
    public void IsValid(string? name, string? prefix, int? number) => Assert.IsEmpty(Codes(name, prefix, number));

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    public void WithoutFremoNumberANameIsNeeded(string? name) =>
        CollectionAssert.AreEqual(new[] { ModuleProblemCode.NameMissing }, Codes(name, "SFjä", null));

    [TestMethod]
    [DataRow(null)]
    [DataRow(" ")]
    public void FremoNumberNeedsAPrefix(string? prefix) =>
        CollectionAssert.AreEqual(new[] { ModuleProblemCode.PrefixMissing }, Codes("Tellby", prefix, 12));

    [TestMethod]
    [DataRow(0)]
    [DataRow(10000)]
    public void FremoNumberMustBeInRange(int number) =>
        CollectionAssert.AreEqual(new[] { ModuleProblemCode.FremoNumberOutOfRange }, Codes(null, "SFjä", number));

    [TestMethod]
    public void UsesTheFirstOwnersPrefix()
    {
        var module = TestModules.Straight() with { Name = "", FremoNumber = 12, Owners = [new() { Name = "Club" }, new() { Prefix = "SFjä" }] };
        CollectionAssert.AreEqual(new[] { new ModuleProblem(ModuleProblemCode.PrefixMissing, nameof(Owner.Prefix)) }, ModuleValidation.Validate(module).ToArray());
    }

    [TestMethod]
    [DataRow("Tellby", null, null, "Tellby")]
    [DataRow("", "SFjä", 12, "SFjä 012")]
    [DataRow(" Tellby ", " SFjä ", 1234, "SFjä 1234 Tellby")]
    [DataRow("Tellby", null, 12, "Tellby", DisplayName = "No designation without prefix")]
    public void TitleHasTheFremoDesignationAndTheName(string? name, string? prefix, int? number, string expected) =>
        Assert.AreEqual(expected, Module.TitleOf(name, prefix, number));

    [TestMethod]
    public void OwnerIsCreatedWhenAPrefixIsGiven()
    {
        var module = TestModules.Straight() with { Owners = [] };
        var changed = module.WithOwnerPrefix(" SFjä ").WithFremoNumber(12).WithName("");
        Assert.AreEqual(new Owner { Prefix = "SFjä" }, changed.Owners.Single());
        Assert.AreEqual("SFjä 012", changed.Title);
        Assert.IsEmpty(ModuleValidation.Validate(changed));
    }

    [TestMethod]
    public void OwnerLeftWithNothingIsRemoved()
    {
        var module = TestModules.Straight() with { Owners = [new() { Prefix = "SFjä", Name = "S. F." }, new() { Name = "Club" }] };
        var changed = module.WithOwnerPrefix(" ").WithOwnerName(null);
        Assert.AreEqual("Club", changed.Owners.Single().Name);
    }

    [TestMethod]
    public void OtherOwnerPropertiesAreKept()
    {
        var module = TestModules.Straight() with { Owners = [new() { Name = "A", Share = 0.5, RegistryPersonId = 17 }] };
        Assert.AreEqual(new Owner { Name = "B", Share = 0.5, RegistryPersonId = 17 }, module.WithOwnerName("B").Owners.Single());
    }
}
