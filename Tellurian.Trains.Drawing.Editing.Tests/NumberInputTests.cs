using System.Globalization;
using Tellurian.Trains.Drawing.Editing.Input;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class NumberInputTests
{
    [TestMethod]
    [DataRow("1200", 1200.0)]
    [DataRow("1200.5", 1200.5)]
    [DataRow("1200,5", 1200.5)]
    [DataRow("1 200,5", 1200.5)]
    [DataRow("1 200,5", 1200.5)]
    [DataRow("1.200,5", 1200.5)]
    [DataRow("1,200.5", 1200.5)]
    [DataRow("-35", -35.0)]
    [DataRow("−35", -35.0)]
    [DataRow("  42 ", 42.0)]
    [DataRow("1200 mm", 1200.0)]
    [DataRow("120cm", 1200.0)]
    [DataRow("1,2 m", 1200.0)]
    [DataRow("1.5M", 1500.0)]
    public void Lengths(string text, double expected)
    {
        Assert.IsTrue(NumberInput.TryParse(text, Quantity.Length, out var value), text);
        Assert.AreEqual(expected, value, Precision, text);
    }

    [TestMethod]
    [DataRow("30", 30.0)]
    [DataRow("22,5°", 22.5)]
    [DataRow("-15 °", -15.0)]
    [DataRow("90deg", 90.0)]
    public void Angles(string text, double expected)
    {
        Assert.IsTrue(NumberInput.TryParse(text, Quantity.Angle, out var value), text);
        Assert.AreEqual(expected, value, Precision);
    }

    [TestMethod]
    [DataRow("", Quantity.Length)]
    [DataRow("abc", Quantity.Length)]
    [DataRow("12 kg", Quantity.Length)]
    [DataRow("30 mm", Quantity.Angle)]
    [DataRow("1.2 m", Quantity.Number)]
    [DataRow("1-2", Quantity.Number)]
    [DataRow("mm", Quantity.Length)]
    [DataRow("1e400", Quantity.Number)]
    public void NotNumbers(string text, Quantity quantity) =>
        Assert.IsFalse(NumberInput.TryParse(text, quantity, out _), text);

    [TestMethod]
    public void FormatFollowsTheCultureWithoutGrouping()
    {
        Assert.AreEqual("1200,5", NumberInput.Format(1200.5, 1, CultureInfo.GetCultureInfo("sv-SE")));
        Assert.AreEqual("1200.5", NumberInput.Format(1200.5, 1, CultureInfo.GetCultureInfo("en")));
        Assert.AreEqual("1200", NumberInput.Format(1200.04, 1, CultureInfo.GetCultureInfo("de")));
        Assert.AreEqual("0", NumberInput.Format(-0.0001, 2, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void FormattedValuesParseBack()
    {
        foreach (var culture in new[] { "en", "sv-SE", "de", "da-DK", "nb-NO" }.Select(CultureInfo.GetCultureInfo))
        {
            var text = NumberInput.Format(-1234.75, 2, culture);
            Assert.IsTrue(NumberInput.TryParse(text, Quantity.Length, out var value), $"{culture}: {text}");
            Assert.AreEqual(-1234.75, value, Precision);
        }
    }
}
