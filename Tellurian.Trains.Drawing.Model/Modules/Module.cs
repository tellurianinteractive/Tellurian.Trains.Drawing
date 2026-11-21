using System.Text.Json;
using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Shared = Tellurian.Trains.Schedules.Model.Layouts;
using static Tellurian.Trains.Schedules.Model.Layouts.ModuleExtensions;

namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// One module as it is set up: the module of the shared layout model (<see cref="Shared.Module"/>: what it is called,
/// who owns it, its standard, its operation location and its tracks as train planning sees them), with what Module
/// Draw adds: its identity, its standard in the catalogue, its properties and its one <see cref="Drawing"/>.
/// <para>
/// From the shared model: <see cref="Shared.Module.Name"/> (may be empty when the module is identified by its
/// designation, see <see cref="ModuleValidation"/>), <see cref="Shared.Module.Prefix"/> and
/// <see cref="Shared.Module.Number"/> (1–9999), which together form the designation, e.g. <c>SFjä 0012</c>,
/// <see cref="Shared.Module.ModuleOwners"/> (the names of the owners in one text),
/// <see cref="Shared.Module.PackageLabel"/> (the box the module is transported in; the modules of a box have the same
/// label and are submitted to a meeting together), <see cref="Shared.Module.Standard"/> (by short name, with the scale),
/// <see cref="Shared.Module.OperationLocation"/> (null for a module that is plain line) and
/// <see cref="Shared.Module.Tracks"/>, which every module has and the track elements of the drawing refer to.
/// </para>
/// </summary>
public sealed class Module : Shared.Module
{
    /// <summary>The identity of the module. Written first in a file, before the properties of the shared model.</summary>
    [JsonPropertyOrder(FirstInFile)]
    public required Guid Id { get; set; }

    /// <summary>
    /// The id of the module's standard in Module Draw's catalogue (<see cref="ModuleStandard.Id"/>).
    /// <see cref="Shared.Module.Standard"/> names the same standard for readers without the catalogue.
    /// </summary>
    public required Guid StandardId { get; set; }

    /// <summary>The add-ons of the standard that the module meets.</summary>
    public IReadOnlyList<CatalogueReference> AddOns { get; set; } = [];

    public string? Theme { get; set; }
    public int? RepresentsFromYear { get; set; }
    public int? RepresentsUptoYear { get; set; }

    /// <summary>As entered; compared with the drawing.</summary>
    public int? NumberOfThroughTracks { get; set; }

    public bool HasNormalGauge { get; set; } = true;
    public bool HasNarrowGauge { get; set; }
    public bool IsDuckunder { get; set; }
    public bool IsTurntable { get; set; }
    public bool IsEndOfLine { get; set; }
    /// <summary>
    /// The module turns an asymmetric end profile over (German <i>Endewender</i>, Registry flag): an asymmetric
    /// feature, such as the high side of FREMO-H0-B96, is on one side of the track at one end and on the other side at the other end.
    /// </summary>
    public bool IsEndProfileSwitcher { get; set; }
    public bool IsSignalModule { get; set; }
    public bool IsStandAlone { get; set; }

    /// <summary>Registry functional state code.</summary>
    public int? FunctionalState { get; set; }

    /// <summary>Registry landscape state code.</summary>
    public int? LandscapeState { get; set; }

    public string? Note { get; set; }

    /// <summary>Incremented on every save.</summary>
    public int Revision { get; set; }

    /// <summary>Who saved the drawing, and the last date each of them did, newest first (<see cref="Files.DrawingRevisionLog"/>).</summary>
    public IReadOnlyList<Files.DrawingRevision> RevisionLog { get; set; } = [];

    /// <summary>What other systems keep about the module, with one entry per system; read and written back unchanged (<see cref="Files.ExternalReferences"/>).</summary>
    public IReadOnlyDictionary<string, JsonElement>? ExternalReferences { get; set; }

    /// <summary>The geometry of the module, with its track elements. Written last in a file, being the largest part.</summary>
    [JsonPropertyOrder(LastInFile)]
    public required ModuleDrawing Drawing { get; set; }

    /// <summary>
    /// A copy that can be changed without changing this, for the editor's undo and drafts: the shared model copies the
    /// operation location and the tracks; what Module Draw adds is immutable and is shared by the copy.
    /// </summary>
    public override Module Copy() => (Module)base.Copy();

    /// <summary>
    /// The module's title from the shared model (<see cref="Shared.ModuleExtensions"/>): what it is called, without its
    /// standard. The designation, scale and display name are the shared model's too.
    /// </summary>
    public override string ToString() => this.Title;

    private const int FirstInFile = -1;
    private const int LastInFile = 1;
}
