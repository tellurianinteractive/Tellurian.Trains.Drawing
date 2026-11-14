using Microsoft.Extensions.Time.Testing;
using Tellurian.Trains.Drawing.Editing.Commands;

namespace Tellurian.Trains.Drawing.Editing.Tests;

[TestClass]
public sealed class AutosaveSchedulerTests
{
    private sealed record Sketch(int Count);

    private static readonly EditCommand<Sketch> Increment = new("Increment", s => s with { Count = s.Count + 1 });

    private sealed class Fixture : IAsyncDisposable
    {
        public Fixture(Func<Sketch, Task>? save = null)
        {
            Scheduler = new(Document, (s, _) => save?.Invoke(s) ?? Store(s), Time, TimeSpan.FromSeconds(1));
        }

        public FakeTimeProvider Time { get; } = new();
        public EditorDocument<Sketch> Document { get; } = new(new(0));
        public List<Sketch> Saved { get; } = [];
        public AutosaveScheduler<Sketch> Scheduler { get; }

        public Task Store(Sketch sketch)
        {
            Saved.Add(sketch);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => Scheduler.DisposeAsync();
    }

    [TestMethod]
    public async Task SavesOnceAfterABurstOfChanges()
    {
        await using var f = new Fixture();
        f.Document.Apply(Increment);
        f.Time.Advance(TimeSpan.FromMilliseconds(600));
        f.Document.Apply(Increment);
        f.Time.Advance(TimeSpan.FromMilliseconds(600));
        Assert.IsEmpty(f.Saved, "Each change restarts the delay.");
        Assert.IsTrue(f.Scheduler.IsPending);

        f.Time.Advance(TimeSpan.FromMilliseconds(500));

        Assert.AreEqual(new Sketch(2), f.Saved.Single());
        Assert.IsFalse(f.Document.IsModified);
        Assert.IsFalse(f.Scheduler.IsPending);
    }

    [TestMethod]
    public async Task FlushSavesAtOnceAndOnlyWhenModified()
    {
        await using var f = new Fixture();
        Assert.IsTrue(await f.Scheduler.FlushAsync());
        Assert.IsEmpty(f.Saved);

        f.Document.Apply(Increment);
        Assert.IsTrue(await f.Scheduler.FlushAsync());
        Assert.HasCount(1, f.Saved);

        f.Time.Advance(TimeSpan.FromSeconds(5));
        Assert.HasCount(1, f.Saved, "The pending save was done by the flush.");
    }

    [TestMethod]
    public async Task UndoToTheSavedVersionCancelsThePendingSave()
    {
        await using var f = new Fixture();
        f.Document.Apply(Increment);
        f.Document.Undo();
        Assert.IsFalse(f.Scheduler.IsPending);
        f.Time.Advance(TimeSpan.FromSeconds(5));
        Assert.IsEmpty(f.Saved);
    }

    [TestMethod]
    public async Task FailedSaveIsReportedAndRetriedOnFlush()
    {
        var fail = true;
        Fixture? fixture = null;
        await using var f = fixture = new Fixture(s => fail ? throw new IOException("Storage is full.") : fixture!.Store(s));
        Exception? reported = null;
        f.Scheduler.SaveFailed += (_, e) => reported = e;

        f.Document.Apply(Increment);
        Assert.IsFalse(await f.Scheduler.FlushAsync());
        Assert.IsInstanceOfType<IOException>(reported);
        Assert.IsTrue(f.Document.IsModified);

        fail = false;
        Assert.IsTrue(await f.Scheduler.FlushAsync());
        Assert.HasCount(1, f.Saved);
        Assert.IsNull(f.Scheduler.LastError);
    }

    [TestMethod]
    public async Task ChangeDuringASaveStaysModified()
    {
        var release = new TaskCompletionSource();
        Fixture? fixture = null;
        await using var f = fixture = new Fixture(async s =>
        {
            await release.Task;
            await fixture!.Store(s);
        });
        f.Document.Apply(Increment);
        var flush = f.Scheduler.FlushAsync();

        f.Document.Apply(Increment);
        release.SetResult();
        await flush;

        Assert.AreEqual(new Sketch(1), f.Saved.Single());
        Assert.IsTrue(f.Document.IsModified, "The second change wasn't in the saved version.");
        await f.Scheduler.FlushAsync();
        Assert.AreEqual(new Sketch(2), f.Saved[^1]);
    }
}
