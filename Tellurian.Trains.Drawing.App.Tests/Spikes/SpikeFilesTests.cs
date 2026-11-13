using Tellurian.Trains.Drawing.App.Spikes;

namespace Tellurian.Trains.Drawing.App.Tests.Spikes;

[TestClass]
public sealed class SpikeFilesTests
{
    [TestMethod]
    public void SampleZipCanBeReadBack()
    {
        var bytes = SpikeFiles.CreateSampleZip(25);

        var result = SpikeFiles.ReadZip(new MemoryStream(bytes));

        Assert.AreEqual(25, result.Files);
        Assert.AreEqual(25, result.Valid);
        Assert.AreEqual("module: Spike module 001", result.First);
    }

    [TestMethod]
    public void InvalidJsonIsReported()
    {
        var (ok, text) = SpikeFiles.Describe("{ not json");

        Assert.IsFalse(ok);
        StringAssert.StartsWith(text, "invalid JSON");
    }
}
