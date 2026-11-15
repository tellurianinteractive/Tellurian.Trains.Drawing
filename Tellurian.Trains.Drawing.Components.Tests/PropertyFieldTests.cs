using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Tellurian.Trains.Drawing.Components.Properties;
using Tellurian.Trains.Drawing.Editing.Input;

namespace Tellurian.Trains.Drawing.Components.Tests;

[TestClass]
public sealed class PropertyFieldTests
{
    private static void InCulture(string name, Action test)
    {
        var (culture, uiCulture) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
            test();
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (culture, uiCulture);
        }
    }

    private static IRenderedComponent<NumberField> Field(BunitContext context, double? value, Action<double> changed, double? min = null) =>
        context.Render<NumberField>(p => p
            .Add(f => f.Label, "X")
            .Add(f => f.Value, value)
            .Add(f => f.Min, min)
            .Add(f => f.ValueChanged, EventCallback.Factory.Create<double>(new object(), changed)));

    [TestMethod]
    public void ShowsTheValueInTheCultureWithItsUnit() => InCulture("sv-SE", () =>
    {
        using var context = new BunitContext();
        var field = Field(context, 1200.5, _ => { });
        Assert.AreEqual("1200,5", field.Find("input").GetAttribute("value"));
        Assert.AreEqual("mm", field.Find(".number-field-unit").TextContent);
        Assert.AreEqual("X", field.Find("label").TextContent);
    });

    [TestMethod]
    public void ChangeCommitsTheParsedValue() => InCulture("sv-SE", () =>
    {
        using var context = new BunitContext();
        double? committed = null;
        var field = Field(context, 1200, v => committed = v);

        field.Find("input").Change("1,5 m");

        Assert.AreEqual(1500, committed);
        Assert.AreEqual("1500", field.Find("input").GetAttribute("value"));
    });

    [TestMethod]
    public void EnterCommits() => InCulture("en", () =>
    {
        using var context = new BunitContext();
        double? committed = null;
        var field = Field(context, 0, v => committed = v);

        field.Find("input").Input("42.25");
        field.Find("input").KeyDown(Key.Enter);

        Assert.AreEqual(42.3, committed!.Value, Precision, "Rounded to one decimal.");
    });

    [TestMethod]
    public void InvalidInputIsMarkedAndNotCommitted() => InCulture("en", () =>
    {
        using var context = new BunitContext();
        var commits = 0;
        var field = Field(context, 100, _ => commits++, min: 0);

        field.Find("input").Change("12 kg");
        Assert.AreEqual("true", field.Find("input").GetAttribute("aria-invalid"));

        field.Find("input").Change("-5");
        Assert.AreEqual("true", field.Find("input").GetAttribute("aria-invalid"), "Below the minimum.");
        Assert.AreEqual(0, commits);

        field.Find("input").KeyDown(Key.Escape);
        Assert.AreEqual("100", field.Find("input").GetAttribute("value"));
        Assert.AreEqual("false", field.Find("input").GetAttribute("aria-invalid"));
    });

    [TestMethod]
    public void UnchangedValueIsNotCommitted() => InCulture("en", () =>
    {
        using var context = new BunitContext();
        var commits = 0;
        var field = Field(context, 100, _ => commits++);
        field.Find("input").Change("100.0");
        Assert.AreEqual(0, commits);
    });

    [TestMethod]
    public void MixedValueShowsAPlaceholderAndCanBeSet() => InCulture("en", () =>
    {
        using var context = new BunitContext();
        double? committed = null;
        var field = Field(context, null, v => committed = v);
        Assert.AreEqual("—", field.Find("input").GetAttribute("placeholder"));

        field.Find("input").Change("");
        Assert.IsNull(committed, "Leaving a mixed field empty keeps the values.");

        field.Find("input").Change("250");
        Assert.AreEqual(250, committed);
    });

    [TestMethod]
    public void NewValueFromOutsideReplacesTheText() => InCulture("en", () =>
    {
        using var context = new BunitContext();
        var field = Field(context, 100, _ => { });
        field.Render(p => p.Add(f => f.Value, 300.0));
        Assert.AreEqual("300", field.Find("input").GetAttribute("value"));
    });

    [TestMethod]
    public void AngleField() => InCulture("de", () =>
    {
        using var context = new BunitContext();
        double? committed = null;
        var field = context.Render<NumberField>(p => p
            .Add(f => f.Label, "Winkel")
            .Add(f => f.Value, 15.0)
            .Add(f => f.Quantity, Quantity.Angle)
            .Add(f => f.Unit, "°")
            .Add(f => f.ValueChanged, EventCallback.Factory.Create<double>(new object(), v => committed = v)));
        field.Find("input").Change("22,5°");
        Assert.AreEqual(22.5, committed);
    });

    [TestMethod]
    public void TextFieldCommitsTrimmedTextOrNull()
    {
        using var context = new BunitContext();
        string? committed = "unchanged";
        var field = context.Render<TextField>(p => p
            .Add(f => f.Label, "Text")
            .Add(f => f.Value, "c/c")
            .Add(f => f.ValueChanged, EventCallback.Factory.Create<string?>(new object(), v => committed = v)));

        field.Find("input").Change("  spacing  ");
        Assert.AreEqual("spacing", committed);

        field.Find("input").Change(" ");
        Assert.IsNull(committed);
    }

    [TestMethod]
    public async Task PanelShowsTheEmptyTextWithoutFields()
    {
        var html = await StaticRendering.RenderAsync<PropertyPanel>(new Dictionary<string, object?>
        {
            [nameof(PropertyPanel.Title)] = "Properties",
            [nameof(PropertyPanel.EmptyText)] = "Inget markerat.",
        });
        StringAssert.Contains(html, "Inget markerat.");
        StringAssert.Contains(html, ">Properties</h2>");

        using var context = new BunitContext();
        var panel = context.Render<PropertyPanel>(p => p.Add(x => x.IsEmpty, true).Add(x => x.EmptyText, "Nothing").AddChildContent("<span class=\"field\"></span>"));
        Assert.AreEqual("Nothing", panel.Find(".property-panel-empty").TextContent);
        Assert.IsEmpty(panel.FindAll(".field"));
    }
}
