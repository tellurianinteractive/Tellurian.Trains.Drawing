using static Tellurian.Trains.Schedules.Model.Layouts.LayoutPartCopying;
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
    public void UsesTheModulesPrefix()
    {
        var module = TestModules.Straight().With(c => { c.Name = ""; c.Number = 12; c.ModuleOwners = "Club"; });
        CollectionAssert.AreEqual(new[] { new ModuleProblem(ModuleProblemCode.PrefixMissing, nameof(Module.Prefix)) }, ModuleValidation.Validate(module).ToArray());
        Assert.IsEmpty(ModuleValidation.Validate(module.With(c => c.Prefix = "SFjä")));
    }

    [TestMethod]
    [DataRow("Tellby", null, null, "Tellby")]
    [DataRow("", "SFjä", 12, "SFjä 0012")]
    [DataRow(" Tellby ", " SFjä ", 1234, "SFjä 1234", DisplayName = "With a prefix the designation alone, unless an operation location")]
    [DataRow("Tellby", null, 12, "Tellby 12", DisplayName = "Without prefix the number follows the name")]
    public void TitleHasTheDesignationAndTheName(string? name, string? prefix, int? number, string expected) =>
        Assert.AreEqual(expected, (TestModules.Straight().With(c => { c.Name = name ?? string.Empty; c.Prefix = prefix; c.Number = number; })).Title);

    [TestMethod]
    [DataRow("Tellby", null, "Short", "Tellby short", DisplayName = "Without a number the configuration follows the name")]
    [DataRow("Tellby", null, "  ", "Tellby", DisplayName = "A blank configuration is left out")]
    [DataRow("Tellby", 12, "Short", "SFjä 0012", DisplayName = "With a number the configuration is left out")]
    public void TitleHasTheConfigurationLabelOfAModuleWithoutANumber(string name, int? number, string label, string expected)
    {
        var module = TestModules.Straight().With(c => { c.Name = name; c.Prefix = "SFjä"; c.Number = number; c.ConfigurationLabel = label; });
        Assert.AreEqual(expected, module.Title);
        Assert.AreEqual(expected, module.ToString());
    }

    [TestMethod]
    public void TheDesignationIsWrittenWithASpace()
    {
        var module = TestModules.Straight().With(c => { c.Prefix = "SFjä"; c.Number = 12; });
        Assert.AreEqual("SFjä 0012", module.Designation);
        Assert.AreEqual("12", (module.With(c => c.Prefix = null)).Designation);
        Assert.IsNull((module.With(c => c.Number = null)).Designation);
    }

    [TestMethod]
    public void PrefixNumberAndNameAreChangedTrimmed()
    {
        var changed = TestModules.Straight().WithPrefix(" SFjä ").WithNumber(12).WithName("");
        Assert.AreEqual(("SFjä", 12, ""), (changed.Prefix, changed.Number, changed.Name));
        Assert.AreEqual("SFjä 0012", changed.Title);
        Assert.IsEmpty(ModuleValidation.Validate(changed));
    }

    [TestMethod]
    public void BlankPrefixAndOwnersAreStoredAsMissing()
    {
        var module = TestModules.Straight().With(c => { c.Prefix = "SFjä"; c.ModuleOwners = "S. F., Club"; });
        Assert.AreEqual("S. F., Club", module.WithModuleOwners(" S. F., Club ").ModuleOwners);
        var changed = module.WithPrefix(" ").WithModuleOwners(null);
        Assert.IsNull(changed.Prefix);
        Assert.IsNull(changed.ModuleOwners);
    }

    [TestMethod]
    public void TheScaleIsThatOfTheStandard()
    {
        var module = TestModules.Straight();
        var standard = TestCatalogue.Instance.FindStandard(TestCatalogue.FremoId)!;
        Assert.AreEqual(standard.Id, module.StandardId);
        Assert.AreEqual(standard.NameOnly, module.Standard);
        Assert.AreEqual((standard.ShortName, standard.ScaleName), (module.Standard!.ShortName, module.Scale?.ShortName));
        Assert.IsNull((module.With(c => c.Standard = null)).Scale);
    }
}
