namespace Tellurian.Trains.Drawing.Model.EndProfiles;

/// <summary>
/// The shape of a module end, drawn in end view as seen from outside the module (spec §3.1):
/// X lateral (positive right), Y up, origin on the centre line at rail top (RÖK).
/// Whether two ends fit is worked out from the drawings (<see cref="JointFitEvaluator"/>).
/// </summary>
public sealed record EndProfile
{
    public required Guid Id { get; init; }

    /// <summary>The designation, e.g. "FREMO-H0-B96" or "MMM480" (Registry <c>ModuleEndProfile.Designation</c>).</summary>
    public required string Designation { get; init; }

    /// <summary>The short name of the scale.</summary>
    public required string Scale { get; init; }

    /// <summary>The standards the profile belongs to.</summary>
    public IReadOnlyList<Guid> StandardIds { get; init; } = [];

    /// <summary>The end-plate outline: a closed path. Shown for information; it isn't a fit criterion.</summary>
    public PathDrawing? Outline { get; init; }

    /// <summary>The landscape surface across the end: an open path.</summary>
    public PathDrawing? GroundLine { get; init; }

    public IReadOnlyList<EndProfileTrack> Tracks { get; init; } = [];
    public IReadOnlyList<Circle> ScrewHoles { get; init; } = [];
    public IReadOnlyList<Circle> Dowels { get; init; } = [];
    public IReadOnlyList<PathDrawing> CablePassages { get; init; } = [];

    /// <summary>Identifies the screw-hole pattern, used for fit when holes aren't drawn.</summary>
    public string? ScrewHolePattern { get; init; }

    public DocumentReference? Source { get; init; }

    /// <summary>The Module Registry end profile id, if known.</summary>
    public int? RegistryId { get; init; }

    /// <summary>Incremented on every save.</summary>
    public int Revision { get; init; }

    public bool HasScrewHoles => ScrewHoles.Count > 0;

    /// <summary>The drawing mirrored about its centre line (X → −X).</summary>
    public EndProfile MirroredX() => this with
    {
        Outline = Outline?.MirroredX(),
        GroundLine = GroundLine?.MirroredX(),
        Tracks = [.. Tracks.Select(t => t.MirroredX())],
        ScrewHoles = [.. ScrewHoles.Select(h => h.MirroredX())],
        Dowels = [.. Dowels.Select(d => d.MirroredX())],
        CablePassages = [.. CablePassages.Select(c => c.MirroredX())],
    };

    /// <summary>The drawing as it is built at an exit with <paramref name="orientation"/>.</summary>
    public EndProfile Oriented(ProfileOrientation orientation) =>
        orientation == ProfileOrientation.Mirrored ? MirroredX() : this;

    public override string ToString() => Designation;
}
