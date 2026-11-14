using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Documents;

/// <summary>The drafts opened from files, and the files that couldn't be opened.</summary>
public sealed record OpenResult(IReadOnlyList<Draft> Drafts, IReadOnlyList<ImportProblem> Problems)
{
    public static OpenResult Cancelled { get; } = new([], []);
}
