using Tellurian.Trains.Drawing.App.Files;
using Tellurian.Trains.Drawing.App.Storage;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Documents;

/// <summary>
/// Opens drawing files as drafts in the workbench and saves drafts to files (UC-5, spec §6).
/// Files are the primary result: a saved draft has no unsaved changes; the draft is kept for resuming work.
/// </summary>
public sealed class DrawingDocuments(IFileService files, IWorkbench workbench, TimeProvider time)
{
    private bool _persistenceRequested;

    public bool CanSaveInPlace => files.CanSaveInPlace;
    public bool CanReopen => files.CanReopen;

    /// <summary>Lets the user pick drawing files and opens each as a draft; returns the opened drafts and the problems.</summary>
    public async Task<OpenResult> OpenAsync(CancellationToken cancellationToken = default)
    {
        var picked = await files.OpenAsync(FileTypes.Drawings, multiple: true, cancellationToken);
        if (picked is null) return OpenResult.Cancelled;
        var drafts = new List<Draft>();
        var problems = new List<ImportProblem>();
        foreach (var file in picked)
        {
            var draft = await OpenFileAsync(file, cancellationToken);
            if (draft.Problem is not null) problems.Add(draft.Problem);
            else drafts.Add(draft.Draft!);
        }
        return new(drafts, problems);
    }

    /// <summary>Re-opens a recent file from its stored handle; null if it can't be re-opened without picking it again.</summary>
    public async Task<OpenResult?> ReopenAsync(RecentFile recent, CancellationToken cancellationToken = default)
    {
        if (recent.HandleId is null) return null;
        var file = await files.ReopenAsync(recent.HandleId, cancellationToken);
        if (file is null) return null;
        var (draft, problem) = await OpenFileAsync(file, cancellationToken);
        return draft is null ? new([], [problem!]) : new([draft], []);
    }

    /// <summary>
    /// Opens <paramref name="file"/> as a draft. A draft with unsaved changes is kept rather than replaced,
    /// so opening a file never loses work.
    /// </summary>
    public async Task<(Draft? Draft, ImportProblem? Problem)> OpenFileAsync(OpenedFile file, CancellationToken cancellationToken = default)
    {
        DrawingIdentity identity;
        try
        {
            identity = DrawingIdentity.Read(file.Text);
            var header = DrawingFileSerializer.ReadHeader(file.Text);
            if (!header.IsSupported)
                return (null, new(file.Name, $"The file has {header.Kind} schema version {header.SchemaVersion}, which is newer than this version of the app reads. Update the app to open it."));
        }
        catch (DrawingFileException ex)
        {
            return (null, new(file.Name, ex.Message));
        }
        if (identity.Id is not { } id) return (null, new(file.Name, "The file has no drawing id."));

        var existing = await workbench.GetDraftAsync(id, cancellationToken);
        var now = time.GetUtcNow();
        var draft = existing is { HasUnsavedChanges: true }
            ? existing
            : new Draft
            {
                Id = id,
                Kind = identity.Kind,
                Name = identity.Name,
                Json = file.Text,
                FileName = file.Name,
                HandleId = file.HandleId,
                Revision = identity.Revision,
                Modified = now,
            };
        if (!ReferenceEquals(draft, existing)) await workbench.SaveDraftAsync(draft, cancellationToken);
        await workbench.AddRecentFileAsync(new() { Id = id, Kind = identity.Kind, Name = identity.Name, FileName = file.Name, HandleId = file.HandleId, Used = now }, cancellationToken);
        return (draft, null);
    }

    /// <summary>Starts a draft for a new drawing, given as a complete drawing file; it isn't saved to a file yet.</summary>
    public async Task<Draft> CreateAsync(string json, CancellationToken cancellationToken = default)
    {
        var identity = DrawingIdentity.Read(json);
        var draft = new Draft
        {
            Id = identity.Id ?? throw new ArgumentException("A new drawing must have an id.", nameof(json)),
            Kind = identity.Kind,
            Name = identity.Name,
            Json = json,
            HasUnsavedChanges = true,
            Revision = identity.Revision,
            Modified = time.GetUtcNow(),
        };
        await workbench.SaveDraftAsync(draft, cancellationToken);
        return draft;
    }

    /// <summary>Stores a changed drawing in its draft and marks it as having unsaved changes (autosave).</summary>
    public Task<Draft> UpdateAsync(Draft draft, string json, CancellationToken cancellationToken = default) =>
        UpdateAsync(draft.Id, json, cancellationToken);

    /// <summary>
    /// Stores a changed drawing in the stored draft with <paramref name="draftId"/>, keeping its file name and handle,
    /// so an editor never overwrites what a save to a file has recorded meanwhile.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The draft doesn't exist any more.</exception>
    public async Task<Draft> UpdateAsync(Guid draftId, string json, CancellationToken cancellationToken = default)
    {
        var draft = await workbench.GetDraftAsync(draftId, cancellationToken) ?? throw new KeyNotFoundException($"There is no draft {draftId}.");
        var identity = DrawingIdentity.Read(json);
        var updated = draft with { Json = json, Name = identity.Name, HasUnsavedChanges = true, Modified = time.GetUtcNow() };
        await workbench.SaveDraftAsync(updated, cancellationToken);
        return updated;
    }

    /// <summary>
    /// Opens a drawing that isn't in a file, e.g. a profile from the user's catalogue, for editing: its existing draft
    /// if there is one, otherwise a new draft without unsaved changes.
    /// </summary>
    public async Task<Draft> EditAsync(string json, CancellationToken cancellationToken = default)
    {
        var identity = DrawingIdentity.Read(json);
        var id = identity.Id ?? throw new ArgumentException("A drawing to edit must have an id.", nameof(json));
        if (await workbench.GetDraftAsync(id, cancellationToken) is { } existing) return existing;
        var draft = await CreateAsync(json, cancellationToken) with { HasUnsavedChanges = false };
        await workbench.SaveDraftAsync(draft, cancellationToken);
        return draft;
    }

    /// <summary>Saves a draft to a file; null if the user cancelled.</summary>
    public async Task<Draft?> SaveAsync(Guid draftId, bool saveAs, CancellationToken cancellationToken = default)
    {
        var draft = await workbench.GetDraftAsync(draftId, cancellationToken) ?? throw new KeyNotFoundException($"There is no draft {draftId}.");
        var saved = await files.SaveAsync(draft.Json, SuggestedFileName(draft), draft.HandleId, saveAs, cancellationToken);
        if (saved is null) return null;
        var now = time.GetUtcNow();
        var updated = draft with { FileName = saved.Name, HandleId = saved.HandleId ?? (files.CanSaveInPlace ? draft.HandleId : null), HasUnsavedChanges = false, Modified = now };
        await workbench.SaveDraftAsync(updated, cancellationToken);
        await workbench.AddRecentFileAsync(new() { Id = draft.Id, Kind = draft.Kind, Name = draft.Name, FileName = saved.Name, HandleId = updated.HandleId, Used = now }, cancellationToken);
        if (!_persistenceRequested)
        {
            _persistenceRequested = true;
            await workbench.RequestPersistenceAsync(cancellationToken);
        }
        return updated;
    }

    /// <summary>
    /// Characters not allowed in file names on Windows, the strictest of the common systems. A fixed list, as
    /// <see cref="Path.GetInvalidFileNameChars"/> in the browser is the Unix one, which allows most of them.
    /// </summary>
    private const string InvalidFileNameCharacters = "<>:\"/\\|?*";

    /// <summary>The previous file name, or one made from the drawing's name, so the browser can offer to replace the old file.</summary>
    public static string SuggestedFileName(Draft draft)
    {
        if (!string.IsNullOrWhiteSpace(draft.FileName)) return draft.FileName;
        var name = new string([.. draft.Name.Trim().Select(c => char.IsControl(c) || InvalidFileNameCharacters.Contains(c) ? '-' : c)]);
        return $"{(name.Length == 0 ? draft.Kind.ToString() : name)}{DrawingFileFormat.Extension(draft.Kind)}";
    }
}
