using System.Reflection;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>The version of Module Draw, written as <c>appVersion</c> in the envelope of the files it saves.</summary>
public static class ModuleDrawVersion
{
    /// <summary>The version of this library, the same as the app's, without build metadata.</summary>
    public static string Current { get; } = Read();

    private static string Read()
    {
        var version = typeof(ModuleDrawVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        var metadata = version.IndexOf('+');
        return metadata < 0 ? version : version[..metadata];
    }
}
