using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Tests.Storage;

[TestClass]
public sealed class RecentFilesTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 14, 0, 0, 0, TimeSpan.Zero);

    private static RecentFile File(int minute, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Kind = DrawingFileKind.Module,
        Name = $"M{minute}",
        FileName = $"m{minute}.tdmodule",
        Used = Start.AddMinutes(minute),
    };

    [TestMethod]
    public void NewestComesFirst()
    {
        var (kept, dropped) = RecentFiles.Add([File(1), File(2)], File(3));
        CollectionAssert.AreEqual(new[] { "M3", "M2", "M1" }, kept.Select(f => f.Name).ToArray());
        Assert.IsEmpty(dropped);
    }

    [TestMethod]
    public void SameDrawingReplacesItsEntry()
    {
        var id = Guid.NewGuid();
        var (kept, _) = RecentFiles.Add([File(1, id), File(2)], File(3, id));
        Assert.HasCount(2, kept);
        Assert.AreEqual("M3", kept[0].Name);
    }

    [TestMethod]
    public void OldestAreDroppedBeyondTheLimit()
    {
        var current = Enumerable.Range(0, RecentFiles.MaxCount).Select(i => File(i)).ToList();
        var (kept, dropped) = RecentFiles.Add(current, File(100));
        Assert.HasCount(RecentFiles.MaxCount, kept);
        Assert.AreEqual("M0", dropped.Single().Name);
    }
}
