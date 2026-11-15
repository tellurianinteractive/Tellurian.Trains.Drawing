using Tellurian.Trains.Drawing.Model.AutoDraw;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>
/// Writes the example files in <c>docs/schema/examples/</c> for the Module Registry developers,
/// auto-drawn from Registry-like data with the built-in catalogue.
/// </summary>
internal static class SchemaExamples
{
    private static DrawingCatalogue Catalogue => BuiltInCatalogue.Instance;

    private static readonly AutoDrawExit West = new() { Label = "W", Direction = ExitDirection.West, EndProfile = "FREMO-H0-B96" };
    private static readonly AutoDrawExit East = new() { Label = "E", Direction = ExitDirection.East, EndProfile = "FREMO-H0-B96" };

    public static void Write(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach (var file in Directory.GetFiles(directory)) File.Delete(file);

        Save(directory, "fremo-h0-b96", DrawingFileKind.EndProfile, DrawingFileSerializer.Serialize(Catalogue.FindEndProfile("FREMO-H0-B96", "H0")!));
        Save(directory, "mmm480", DrawingFileKind.EndProfile, DrawingFileSerializer.Serialize(Catalogue.FindEndProfile("MMM480", "H0")!));
        Module(directory, "straight-1200", new() { Name = "Straight 1200", Standard = "FREMO/H0-RE", Straight = 1200, Length = 1200, Width = 500, NumberOfThroughTracks = 1, Exits = [West, East] });
        Module(directory, "curve-r2000-30", new() { Name = "Curve R2000 30°", Standard = "FREMO/H0-RE", Radius = 2000, Angle = 30, Length = 1047, NumberOfThroughTracks = 1, Exits = [West, East] });
        Module(directory, "station-3-sections", new() { Name = "Station", Standard = "FREMO/H0-RE", Straight = 3000, Width = 600, NumberOfSections = 3, NumberOfThroughTracks = 1, IsOperationPlace = true, Exits = [West, East] });
        Module(directory, "terminus-double-track", new() { Name = "Terminus", Standard = "FREMO/H0-RE", Straight = 1200, NumberOfThroughTracks = 2, IsEndOfLine = true, Exits = [West with { EndProfile = "FREMO-H0-2E96" }] });
        Module(directory, "mmm-2r-summer-curve", new() { Name = "Summer curve", Standard = "MMM-2R/SSM", Radius = 1500, Angle = 45, Straight = 300, StraightPosition = StraightPosition.Split });
        Save(directory, "club-hall", DrawingFileKind.Hall, DrawingFileSerializer.Serialize(TestHalls.Club()));
        Save(directory, "club-meeting", DrawingFileKind.HallPlan, DrawingFileSerializer.Serialize(TestHalls.Plan(Catalogue)));
    }

    private static void Module(string directory, string name, AutoDrawParameters parameters)
    {
        var module = ModuleAutoDraw.Create(parameters, Catalogue).Module ?? throw new InvalidOperationException($"{name} couldn't be drawn.");
        Save(directory, name, DrawingFileKind.Module, DrawingFileSerializer.Serialize(ModuleFileContent.Create(module, Catalogue.FindEndProfile)));
    }

    private static void Save(string directory, string name, DrawingFileKind kind, string json) =>
        File.WriteAllText(Path.Combine(directory, $"{name}{DrawingFileFormat.Extension(kind)}"), $"{json}{Environment.NewLine}");
}
