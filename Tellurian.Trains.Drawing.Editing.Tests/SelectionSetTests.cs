using Tellurian.Trains.Drawing.Editing.Selection;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class SelectionSetTests
{
    private static readonly Dictionary<string, BoundingBox> Boxes = new()
    {
        ["a"] = new(new(0, 0), new(100, 100)),
        ["b"] = new(new(200, 0), new(300, 100)),
        ["c"] = new(new(250, 50), new(600, 150)),
    };

    private static SpatialIndex<string> Index()
    {
        var index = new SpatialIndex<string>(100);
        foreach (var (id, box) in Boxes) index.Set(id, box);
        return index;
    }

    [TestMethod]
    public void SelectReplacesAndToggleAddsOrRemoves()
    {
        var selection = new SelectionSet<string>();
        selection.Select("a");
        selection.Toggle("b");
        CollectionAssert.AreEqual(new[] { "a", "b" }, selection.Items.ToArray());
        Assert.IsNull(selection.Single);

        selection.Toggle("a");
        Assert.AreEqual("b", selection.Single);

        selection.Select("c");
        CollectionAssert.AreEqual(new[] { "c" }, selection.Items.ToArray());
    }

    [TestMethod]
    public void ChangedIsRaisedOnlyForChanges()
    {
        var selection = new SelectionSet<string>();
        var changes = 0;
        selection.Changed += (_, _) => changes++;
        selection.Select("a");
        selection.Select("a");
        selection.Clear();
        selection.Clear();
        Assert.AreEqual(2, changes);
    }

    [TestMethod]
    public void RubberBandSelectsItemsWhollyInside()
    {
        var selection = new SelectionSet<string>();
        selection.SelectInside(new(new(-10, -10), new(310, 110)), Index(), id => Boxes[id]);
        CollectionAssert.AreEquivalent(new[] { "a", "b" }, selection.Items.ToArray(), "c is only partly inside.");
    }

    [TestMethod]
    public void AdditiveRubberBandKeepsTheSelection()
    {
        var selection = new SelectionSet<string>();
        selection.Select("c");
        selection.SelectInside(new(new(-10, -10), new(110, 110)), Index(), id => Boxes[id], additive: true);
        CollectionAssert.AreEqual(new[] { "c", "a" }, selection.Items.ToArray());
    }
}
