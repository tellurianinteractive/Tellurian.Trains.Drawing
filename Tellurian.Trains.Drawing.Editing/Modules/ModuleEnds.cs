using Tellurian.Trains.Drawing.Editing.Commands;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.Editing.Modules;

/// <summary>
/// The commands that change a module's ends and shape (<see cref="ModuleRedraw"/>): another end profile at an exit,
/// an end plate turned the other side out, or the curve turning the other way. A change that can't be made leaves
/// the module as it was.
/// </summary>
public static class ModuleEnds
{
    /// <summary>Gives exit <paramref name="exitLabel"/> the end profile <paramref name="profile"/>, redrawing the module.</summary>
    public static IEditCommand<ModuleFileContent> SetEndProfile(string exitLabel, EndProfile profile, DrawingCatalogue catalogue) =>
        new EditCommand<ModuleFileContent>("Change end profile", content => ModuleRedraw.WithEndProfile(content, exitLabel, profile, catalogue) ?? content);

    /// <summary>Turns the end plate at exit <paramref name="exitLabel"/> the other side out.</summary>
    public static IEditCommand<ModuleFileContent> FlipEnd(string exitLabel) =>
        new EditCommand<ModuleFileContent>("Flip end profile", content => ModuleRedraw.WithEndFlipped(content, exitLabel) ?? content);

    /// <summary>Makes the curve turn the other way, redrawing the module.</summary>
    public static IEditCommand<ModuleFileContent> FlipCurve(DrawingCatalogue catalogue) =>
        new EditCommand<ModuleFileContent>("Flip curve", content => ModuleRedraw.WithCurveFlipped(content, catalogue) ?? content);
}
