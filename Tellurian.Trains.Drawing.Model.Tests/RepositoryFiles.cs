using System.Text.Json.Nodes;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>
/// Access to files checked into the repository, such as the published schemas in <c>docs/schema/</c>.
/// Set the environment variable <see cref="UpdateVariable"/> to 1 to let tests rewrite generated files.
/// </summary>
internal static class RepositoryFiles
{
    public const string UpdateVariable = "TELLURIAN_UPDATE_SCHEMAS";
    private const string SolutionFile = "Tellurian.Trains.Drawing.slnx";

    public static bool IsUpdating => Environment.GetEnvironmentVariable(UpdateVariable) == "1";

    public static string Root { get; } = FindRoot();

    public static string SchemaDirectory => Path.Combine(Root, "docs", "schema");

    public static string ExamplesDirectory => Path.Combine(SchemaDirectory, "examples");

    public static void Write(string path, JsonNode content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, $"{content.ToJsonString(new() { WriteIndented = true })}{Environment.NewLine}");
    }

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFile))) return directory.FullName;
        }
        throw new InvalidOperationException($"{SolutionFile} wasn't found above {AppContext.BaseDirectory}.");
    }
}
