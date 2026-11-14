namespace Tellurian.Trains.Drawing.App.Layout;

/// <summary>The app's version as shown, e.g. "0.2.0", from Directory.Build.props.</summary>
public static class AppVersion
{
    public static string Current { get; } = typeof(AppVersion).Assembly.GetName().Version?.ToString(3) ?? "?";
}
