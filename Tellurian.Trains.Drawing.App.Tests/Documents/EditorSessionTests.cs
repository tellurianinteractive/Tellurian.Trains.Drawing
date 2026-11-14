using Bunit;
using Microsoft.Extensions.Time.Testing;
using Tellurian.Trains.Drawing.App.Documents;
using Tellurian.Trains.Drawing.App.Sandbox;
using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.App.Tests.Fakes;
using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model;

namespace Tellurian.Trains.Drawing.App.Tests.Documents;

/// <summary>Phase 4 autosave and AP-7: no work is lost when the app reloads itself, and editors reopen as they were.</summary>
[TestClass]
public sealed class EditorSessionTests
{
    private const string Key = "test";

    private static readonly EditCommand<SandboxDocument> AddSquare =
        new("Add square", d => d.WithShape(PathDrawing.Rectangle(new(0, 0), 100, 100, Angle.Zero)));

    private sealed class Fixture : IDisposable
    {
        public Fixture()
        {
            Context.JSInterop.SetupVoid("location.reload").SetVoidResult();
            Reload = new(Context.JSInterop.JSRuntime);
            Session = new(Key, Document, (d, ct) => Store.SaveAsync(d, ct), Workbench, Reload, Time, TimeSpan.FromSeconds(1));
        }

        public BunitContext Context { get; } = new();
        public InMemoryWorkbench Workbench { get; } = new();
        public SandboxStore Store => new(Workbench);
        public FakeTimeProvider Time { get; } = new();
        public SafeReload Reload { get; }
        public EditorDocument<SandboxDocument> Document { get; } = new(new([]));
        public EditorSession<SandboxDocument> Session { get; }

        public void Dispose() => Context.Dispose();
    }

    [TestMethod]
    public async Task ChangesAreAutosavedAfterTheDelay()
    {
        using var f = new Fixture();
        f.Document.Apply(AddSquare);
        Assert.IsNull(await f.Store.LoadAsync());

        f.Time.Advance(TimeSpan.FromSeconds(1));

        Assert.HasCount(1, (await f.Store.LoadAsync())!.Shapes);
        await f.Session.DisposeAsync();
    }

    [TestMethod]
    public async Task ReloadFlushesTheDocumentAndTheEditorState()
    {
        using var f = new Fixture();
        f.Session.CaptureState = () => new(10, 20, 0.5, ["a"]);
        f.Document.Apply(AddSquare);

        var error = await f.Reload.ReloadAsync();

        Assert.IsNull(error);
        Assert.HasCount(1, (await f.Store.LoadAsync())!.Shapes, "Flushed before the delay passed.");
        var state = await EditorSession<SandboxDocument>.LoadStateAsync(f.Workbench, Key);
        Assert.IsNotNull(state);
        Assert.AreEqual((10.0, 20.0, 0.5), (state.CenterX, state.CenterY, state.Scale));
        CollectionAssert.AreEqual(new[] { "a" }, state.Selection.ToArray());
        Assert.HasCount(1, f.Context.JSInterop.Invocations["location.reload"]);
        await f.Session.DisposeAsync();
    }

    [TestMethod]
    public async Task FailedSaveCancelsTheReload()
    {
        using var context = new BunitContext();
        context.JSInterop.SetupVoid("location.reload").SetVoidResult();
        var reload = new SafeReload(context.JSInterop.JSRuntime);
        var document = new EditorDocument<SandboxDocument>(new([]));
        await using var session = new EditorSession<SandboxDocument>(Key, document,
            (_, _) => throw new StorageException("The database is blocked by another tab."), new InMemoryWorkbench(), reload, new FakeTimeProvider());
        document.Apply(AddSquare);

        var error = await reload.ReloadAsync();

        Assert.IsInstanceOfType<StorageException>(error);
        Assert.IsEmpty(context.JSInterop.Invocations);
    }

    [TestMethod]
    public async Task ClosingTheEditorSavesAndUnregisters()
    {
        using var f = new Fixture();
        f.Document.Apply(AddSquare);

        await f.Session.DisposeAsync();

        Assert.HasCount(1, (await f.Store.LoadAsync())!.Shapes);
        f.Document.Apply(AddSquare);
        await f.Reload.ReloadAsync();
        Assert.HasCount(1, (await f.Store.LoadAsync())!.Shapes, "A closed editor is no longer flushed.");
    }

    [TestMethod]
    public async Task MissingOrUnreadableStateIsNull()
    {
        var workbench = new InMemoryWorkbench();
        Assert.IsNull(await EditorSession<SandboxDocument>.LoadStateAsync(workbench, Key));
        await workbench.SetValueAsync(EditorState.StorageKey(Key), "{ broken");
        Assert.IsNull(await EditorSession<SandboxDocument>.LoadStateAsync(workbench, Key));
    }

    [TestMethod]
    public async Task SandboxDocumentRoundTrips()
    {
        var store = new SandboxStore(new InMemoryWorkbench());
        var document = SandboxDocument.Sample.WithDimension(Dimension.Place(new(0, 0), new(1000, 0), new(500, 100)));
        await store.SaveAsync(document);

        var read = await store.LoadAsync();

        Assert.IsNotNull(read);
        CollectionAssert.AreEqual(document.Shapes.Select(s => s.Id).ToArray(), read.Shapes.Select(s => s.Id).ToArray());
        Assert.AreEqual(document.Bounds, read.Bounds);
        Assert.AreEqual(document.Dimensions.Single(), read.Dimensions.Single());
    }
}
