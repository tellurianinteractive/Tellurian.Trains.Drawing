namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>A problem with a module's properties, and the property it concerns.</summary>
public sealed record ModuleProblem(ModuleProblemCode Code, string Property);
