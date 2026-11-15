using System.Globalization;
using Tellurian.Trains.Drawing.Components.Properties;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.Components.Tests;

[TestClass]
public sealed class RevisionLogListTests
{
    [TestMethod]
    public async Task ShowsEachPersonWithTheLastDateWithoutJs()
    {
        IReadOnlyList<DrawingRevision> log =
        [
            new() { Name = "Anna", Date = new(2026, 11, 20) },
            new() { Name = "Bo", Date = new(2026, 11, 16) },
        ];

        var html = await StaticRendering.RenderAsync<RevisionLogList>(new Dictionary<string, object?>
        {
            [nameof(RevisionLogList.Log)] = log,
            [nameof(RevisionLogList.Title)] = "Ändringslogg",
        });

        StringAssert.Contains(html, "Ändringslogg");
        Assert.IsLessThan(html.IndexOf("Bo", StringComparison.Ordinal), html.IndexOf("Anna", StringComparison.Ordinal), "Newest first, as in the drawing.");
        StringAssert.Contains(html, "datetime=\"2026-11-20\"");
        StringAssert.Contains(html, new DateOnly(2026, 11, 16).ToString("d", CultureInfo.CurrentCulture));
    }

    [TestMethod]
    public async Task AnEmptyLogShowsTheEmptyText()
    {
        var html = await StaticRendering.RenderAsync<RevisionLogList>(new Dictionary<string, object?>
        {
            [nameof(RevisionLogList.Log)] = Array.Empty<DrawingRevision>(),
            [nameof(RevisionLogList.EmptyText)] = "Nobody yet.",
        });

        StringAssert.Contains(html, "Nobody yet.");
        Assert.DoesNotContain("<table", html);
    }
}
