namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>
/// Changes how a module is identified (MD-2): its name, FREMO number, and the first owner's FREMO prefix and full name.
/// Blank texts are stored as empty or missing. The first owner is created when a prefix or name is given, and removed
/// again when it is left with nothing. The result may be invalid; check it with <see cref="ModuleValidation"/>.
/// </summary>
public static class ModuleIdentification
{
    public static Module WithName(this Module module, string? name)
    {
        ArgumentNullException.ThrowIfNull(module);
        return module with { Name = name?.Trim() ?? string.Empty };
    }

    public static Module WithFremoNumber(this Module module, int? fremoNumber)
    {
        ArgumentNullException.ThrowIfNull(module);
        return module with { FremoNumber = fremoNumber };
    }

    public static Module WithOwnerPrefix(this Module module, string? prefix) =>
        module.WithFirstOwner(o => o with { Prefix = Blank(prefix) ? null : prefix!.Trim() });

    public static Module WithOwnerName(this Module module, string? name) =>
        module.WithFirstOwner(o => o with { Name = name?.Trim() ?? string.Empty });

    private static Module WithFirstOwner(this Module module, Func<Owner, Owner> change)
    {
        ArgumentNullException.ThrowIfNull(module);
        var owner = change(module.Owners.Count > 0 ? module.Owners[0] : new Owner());
        var others = module.Owners.Skip(1);
        return module with { Owners = IsEmpty(owner) ? [.. others] : [owner, .. others] };
    }

    private static bool IsEmpty(Owner owner) =>
        Blank(owner.Name) && Blank(owner.Prefix) && owner.Share is null && owner.RegistryPersonId is null && owner.RegistryGroupId is null;

    private static bool Blank(string? text) => string.IsNullOrWhiteSpace(text);
}
