using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Editing.Selection;
using Tellurian.Trains.Drawing.Editing.Snapping;
using Tellurian.Trains.Drawing.Editing.Tools;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class SelectMoveToolTests
{
    private const double Reach = 10;

    private sealed record Box(string Id, Point Min, double Width, double Height)
    {
        public BoundingBox Bounds => new(Min, new(Min.X + Width, Min.Y + Height));
        public IEnumerable<Point> Corners => [Min, new(Min.X + Width, Min.Y), new(Min.X + Width, Min.Y + Height), new(Min.X, Min.Y + Height)];
    }

    private sealed record Boxes(IReadOnlyList<Box> Items)
    {
        public Box this[string id] => Items.Single(b => b.Id == id);
    }

    private sealed class BoxScene : ISelectableScene<Boxes, string>
    {
        public bool TryHitTest(Boxes document, Point point, double tolerance, out string id)
        {
            id = document.Items.LastOrDefault(b => b.Bounds.Inflated(tolerance).Contains(point))?.Id ?? string.Empty;
            return id.Length > 0;
        }

        public BoundingBox BoundsOf(Boxes document, string id) => document[id].Bounds;

        public SpatialIndex<string> Index(Boxes document)
        {
            var index = new SpatialIndex<string>(100);
            foreach (var box in document.Items) index.Set(box.Id, box.Bounds);
            return index;
        }

        public IEnumerable<SnapTarget> SnapTargetsOf(Boxes document, string id) => document[id].Corners.Select(c => new PointTarget(c, id));

        public IEditCommand<Boxes> Move(IReadOnlyList<string> ids, Vector offset) =>
            new EditCommand<Boxes>($"Move {ids.Count}", d => d with { Items = [.. d.Items.Select(b => ids.Contains(b.Id) ? b with { Min = b.Min + offset } : b)] });
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            Document = new(new([new("a", new(0, 0), 100, 100), new("b", new(300, 0), 100, 100), new("c", new(1000, 1000), 50, 50)]));
            Tool = new(Document, Selection, Scene, Snap);
            Refresh();
            Document.Changed += (_, _) => Refresh();
        }

        public EditorDocument<Boxes> Document { get; }
        public SelectionSet<string> Selection { get; } = new();
        public BoxScene Scene { get; } = new();
        public SnapEngine Snap { get; } = new(cellSize: 100);
        public SelectMoveTool<Boxes, string> Tool { get; }

        public void Click(Point at, bool additive = false)
        {
            Tool.PointerDown(Pointer(at, additive));
            Tool.PointerUp(Pointer(at, additive));
        }

        public void Drag(Point from, Point to, bool additive = false, bool constrained = false, bool noSnap = false)
        {
            Tool.PointerDown(Pointer(from, additive));
            Tool.PointerMove(Pointer(Point.Lerp(from, to, 0.5), additive, constrained, noSnap));
            Tool.PointerMove(Pointer(to, additive, constrained, noSnap));
            Tool.PointerUp(Pointer(to, additive, constrained, noSnap));
        }

        public static ToolPointer Pointer(Point at, bool additive = false, bool constrained = false, bool noSnap = false) =>
            new(at, Reach) { IsAdditive = additive, IsConstrained = constrained, IsSnapSuppressed = noSnap };

        private void Refresh()
        {
            Snap.Clear();
            foreach (var box in Document.Current.Items) Snap.AddRange(Scene.SnapTargetsOf(Document.Current, box.Id));
        }
    }

    [TestMethod]
    public void ClickSelectsAndClickOnEmptySpaceClears()
    {
        var f = new Fixture();
        f.Click(new(50, 50));
        Assert.AreEqual("a", f.Selection.Single);
        f.Click(new(600, 600));
        Assert.IsTrue(f.Selection.IsEmpty);
    }

    [TestMethod]
    public void AdditiveClickToggles()
    {
        var f = new Fixture();
        f.Click(new(50, 50));
        f.Click(new(350, 50), additive: true);
        CollectionAssert.AreEqual(new[] { "a", "b" }, f.Selection.Items.ToArray());
        f.Click(new(50, 50), additive: true);
        CollectionAssert.AreEqual(new[] { "b" }, f.Selection.Items.ToArray());
    }

    [TestMethod]
    public void ClickOnAnItemOfAMultipleSelectionSelectsOnlyIt()
    {
        var f = new Fixture();
        f.Selection.Set(["a", "b"]);
        f.Click(new(350, 50));
        CollectionAssert.AreEqual(new[] { "b" }, f.Selection.Items.ToArray());
    }

    [TestMethod]
    public void DragMovesTheSelectionAsOneUndoableCommand()
    {
        var f = new Fixture();
        f.Selection.Set(["a", "c"]);
        f.Drag(new(50, 50), new(250, 550), noSnap: true);

        Assert.AreEqual(new Point(200, 500), f.Document.Current["a"].Min);
        Assert.AreEqual(new Point(1200, 1500), f.Document.Current["c"].Min);
        Assert.AreEqual(new Point(300, 0), f.Document.Current["b"].Min);
        Assert.AreEqual("Move 2", f.Document.UndoDescription);

        f.Document.Undo();
        Assert.AreEqual(new Point(0, 0), f.Document.Current["a"].Min);
        Assert.AreEqual(new Point(1000, 1000), f.Document.Current["c"].Min);
    }

    [TestMethod]
    public void DraggingAnUnselectedItemSelectsAndMovesIt()
    {
        var f = new Fixture();
        f.Selection.Select("a");
        f.Drag(new(1025, 1025), new(1125, 1025), noSnap: true);
        Assert.AreEqual("c", f.Selection.Single);
        Assert.AreEqual(new Point(1100, 1000), f.Document.Current["c"].Min);
        Assert.AreEqual(new Point(0, 0), f.Document.Current["a"].Min);
    }

    [TestMethod]
    public void DraggedCornerSnapsToAnotherItemsCorner()
    {
        var f = new Fixture();
        // Grab a's top right corner and drop it 6 mm from b's top left corner.
        f.Drag(new(100, 100), new(294, 103));
        Assert.AreEqual(new Point(200, 0), f.Document.Current["a"].Min, "a's corner (100,100) snapped to b's corner (300,100).");
    }

    [TestMethod]
    public void ItemsDoNotSnapToThemselves()
    {
        var f = new Fixture();
        f.Drag(new(100, 100), new(106, 108));
        Assert.AreEqual(new Point(6, 8), f.Document.Current["a"].Min, "a doesn't snap to its own corner where it started.");
    }

    [TestMethod]
    public void ConstrainedDragIsHorizontalOrVertical()
    {
        var f = new Fixture();
        f.Drag(new(50, 50), new(250, 90), constrained: true, noSnap: true);
        Assert.AreEqual(new Point(200, 0), f.Document.Current["a"].Min);
    }

    [TestMethod]
    public void DragPreviewIsShownUntilRelease()
    {
        var f = new Fixture();
        f.Tool.PointerDown(Fixture.Pointer(new(50, 50)));
        f.Tool.PointerMove(Fixture.Pointer(new(150, 50), noSnap: true));
        Assert.AreEqual(new Vector(100, 0), f.Tool.DragOffset);
        Assert.AreEqual(new Point(0, 0), f.Document.Current["a"].Min, "The document changes only on release.");
    }

    [TestMethod]
    public void EscapeAbandonsADrag()
    {
        var f = new Fixture();
        f.Tool.PointerDown(Fixture.Pointer(new(50, 50)));
        f.Tool.PointerMove(Fixture.Pointer(new(150, 50)));
        Assert.IsTrue(f.Tool.KeyDown("Escape"));
        f.Tool.PointerUp(Fixture.Pointer(new(150, 50)));
        Assert.IsNull(f.Tool.DragOffset);
        Assert.IsFalse(f.Document.CanUndo);
        Assert.IsFalse(f.Tool.KeyDown("Escape"), "Escape does nothing when the tool is idle.");
    }

    [TestMethod]
    public void RubberBandSelectsItemsInside()
    {
        var f = new Fixture();
        f.Tool.PointerDown(Fixture.Pointer(new(-50, -50)));
        f.Tool.PointerMove(Fixture.Pointer(new(450, 150)));
        Assert.IsNotNull(f.Tool.RubberBand);
        f.Tool.PointerUp(Fixture.Pointer(new(450, 150)));
        CollectionAssert.AreEquivalent(new[] { "a", "b" }, f.Selection.Items.ToArray());
        Assert.IsNull(f.Tool.RubberBand);
    }

    [TestMethod]
    public void TinyMovementIsStillAClick()
    {
        var f = new Fixture();
        f.Drag(new(50, 50), new(52, 51));
        Assert.AreEqual("a", f.Selection.Single);
        Assert.IsFalse(f.Document.CanUndo);
    }
}
