using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Storage;

/// <summary>
/// Work in progress on one drawing, autosaved in the workbench (spec §6, UC-5). A draft is never the only
/// copy of finished work: the app reminds the user to save a file while it has unsaved changes.
/// </summary>
public sealed record Draft
{
    /// <summary>The id of the drawing.</summary>
    public required Guid Id { get; init; }

    public required DrawingFileKind Kind { get; init; }
    public required string Name { get; init; }

    /// <summary>The drawing as a complete drawing file.</summary>
    public required string Json { get; init; }

    /// <summary>The name of the file it was opened from or last saved to.</summary>
    public string? FileName { get; init; }

    /// <summary>The stored file handle, where the browser can save in place.</summary>
    public string? HandleId { get; init; }

    /// <summary>True while the draft has changes that aren't saved to a file.</summary>
    public bool HasUnsavedChanges { get; init; }

    /// <summary>The revision of the drawing, as in its file.</summary>
    public int Revision { get; init; }

    /// <summary>When the draft was last changed, UTC.</summary>
    public required DateTimeOffset Modified { get; init; }
}
