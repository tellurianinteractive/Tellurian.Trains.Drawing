using static Tellurian.Trains.Schedules.Model.Layouts.LayoutPartCopying;

namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// Changes how a module is identified (MD-2): its name, number and prefix, and the names of its owners.
/// Blank texts are stored as empty (the name) or missing. The result may be invalid; check it with <see cref="ModuleValidation"/>.
/// </summary>
public static class ModuleIdentification
{
    public static Module WithName(this Module module, string? name)
    {
        ArgumentNullException.ThrowIfNull(module);
        return module.With(m => m.Name = name?.Trim() ?? string.Empty);
    }

    public static Module WithNumber(this Module module, int? number)
    {
        ArgumentNullException.ThrowIfNull(module);
        return module.With(m => m.Number = number);
    }

    public static Module WithPrefix(this Module module, string? prefix)
    {
        ArgumentNullException.ThrowIfNull(module);
        return module.With(m => m.Prefix = TextOrNull(prefix));
    }

    /// <summary>The names of the owners, separated by commas, as one text.</summary>
    public static Module WithModuleOwners(this Module module, string? owners)
    {
        ArgumentNullException.ThrowIfNull(module);
        return module.With(m => m.ModuleOwners = TextOrNull(owners));
    }

    private static string? TextOrNull(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
