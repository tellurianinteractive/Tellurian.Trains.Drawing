namespace Tellurian.Trains.Drawing.App.Tests;

/// <summary>Source folders the tests read from, found from the repository root.</summary>
internal static class RepositoryPaths
{
    public static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Tellurian.Trains.Drawing.slnx"))) directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("The repository root wasn't found.");
        }
    }

    public static string App => Path.Combine(Root, "Tellurian.Trains.Drawing.App");

    public static string Translations => Path.Combine(Root, "Tellurian.Trains.Drawing.App.Translations");

    /// <summary>The Markdown documents (<c>wwwroot/Content</c>) of the Translations project.</summary>
    public static string Content => Path.Combine(Translations, "wwwroot", "Content");
}
