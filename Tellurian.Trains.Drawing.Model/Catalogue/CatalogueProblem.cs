namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>A problem with a catalogue entry, and the property it concerns.</summary>
public sealed record CatalogueProblem(CatalogueProblemCode Code, string Property);
