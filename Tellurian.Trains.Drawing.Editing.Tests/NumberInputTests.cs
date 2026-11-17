using System.Globalization;
using Tellurian.Trains.Drawing.Editing.Input;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class NumberInputTests
{
    private static readonly string[] AppCultures = ["en", "sv", "de", "da", "nb"];

    private static CultureInfo Culture(string name) => CultureInfo.GetCultureInfo(name);

    [TestMethod]
    [DataRow("sv", "1200", 1200.0)]
    [DataRow("sv", "1200,5", 1200.5)]
    [DataRow("sv", "1 200,5", 1200.5)]
    [DataRow("sv", "1 200,5", 1200.5)]
    [DataRow("sv", "12,5 m", 12500.0)]
    [DataRow("sv", "-35", -35.0)]
    [DataRow("sv", "−35", -35.0)]
    [DataRow("sv", "  42 ", 42.0)]
    [DataRow("sv", ",5", 0.5)]
    [DataRow("nb", "1 200,5", 1200.5)]
    [DataRow("de", "1.200,5", 1200.5)]
    [DataRow("de", "1.200.000", 1200000.0)]
    [DataRow("de", "1 200,5", 1200.5)]
    [DataRow("de", "1,2 m", 1200.0)]
    [DataRow("da", "1.200,5", 1200.5)]
    [DataRow("da", "120cm", 1200.0)]
    [DataRow("en", "1200.5", 1200.5)]
    [DataRow("en", "1,500", 1500.0)]
    [DataRow("en", "1,200.5", 1200.5)]
    [DataRow("en", "12.5 m", 12500.0)]
    [DataRow("en", "1.5M", 1500.0)]
    [DataRow("en", "1200 mm", 1200.0)]
    public void LengthsAreWrittenTheCulturesWay(string culture, string text, double expected)
    {
        Assert.IsTrue(NumberInput.TryParse(text, Quantity.Length, Culture(culture), out var value), $"{culture}: {text}");
        Assert.AreEqual(expected, value, Precision, $"{culture}: {text}");
    }

    [TestMethod]
    [DataRow("sv", "12.5")]
    [DataRow("sv", "1,200.5")]
    [DataRow("de", "12.5")]
    [DataRow("de", "1,200.5")]
    [DataRow("da", "1.20")]
    [DataRow("en", "1,5")]
    [DataRow("en", "12,5 m")]
    [DataRow("en", "1.200,5")]
    [DataRow("en", "1,20,000")]
    [DataRow("en", "1,500.2.5")]
    public void OtherCulturesSeparatorsAreRefused(string culture, string text) =>
        Assert.IsFalse(NumberInput.TryParse(text, Quantity.Length, Culture(culture), out _), $"{culture}: {text}");

    [TestMethod]
    [DataRow("sv", "30", 30.0)]
    [DataRow("sv", "22,5°", 22.5)]
    [DataRow("en", "22.5°", 22.5)]
    [DataRow("en", "-15 °", -15.0)]
    [DataRow("en", "90deg", 90.0)]
    public void Angles(string culture, string text, double expected)
    {
        Assert.IsTrue(NumberInput.TryParse(text, Quantity.Angle, Culture(culture), out var value), text);
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
    [DataRow("-", Quantity.Number)]
    [DataRow("1e400", Quantity.Number)]
    public void NotNumbers(string text, Quantity quantity)
    {
        foreach (var culture in AppCultures)
            Assert.IsFalse(NumberInput.TryParse(text, quantity, Culture(culture), out _), $"{culture}: {text}");
    }

    [TestMethod]
    public void FormatFollowsTheCultureWithoutGrouping()
    {
        Assert.AreEqual("1200,5", NumberInput.Format(1200.5, 1, Culture("sv")));
        Assert.AreEqual("1200.5", NumberInput.Format(1200.5, 1, Culture("en")));
        Assert.AreEqual("1200", NumberInput.Format(1200.04, 1, Culture("de")));
        Assert.AreEqual("0", NumberInput.Format(-0.0001, 2, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void FormattedValuesParseBack()
    {
        foreach (var culture in AppCultures.Select(Culture))
        {
            var text = NumberInput.Format(-1234.75, 2, culture);
            Assert.IsTrue(NumberInput.TryParse(text, Quantity.Length, culture, out var value), $"{culture}: {text}");
            Assert.AreEqual(-1234.75, value, Precision);
        }
    }
}
