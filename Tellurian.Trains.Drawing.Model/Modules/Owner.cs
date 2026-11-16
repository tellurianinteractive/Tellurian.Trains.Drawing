namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>A person or group that owns (part of) a module (Registry <c>ModuleOwnership</c>).</summary>
public sealed record Owner
{
    /// <summary>
    /// The owner's full name (Registry person's or group's name). Optional: an upload to the Module Registry fills it in.
    /// Not the same as the user name in the settings, since the one who draws isn't always the owner.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The owner's FREMO prefix, e.g. <c>SFjä</c> (Registry <c>FremoOwnerSignature</c>, or a group's short name).
    /// The first owner's prefix and the module's <see cref="Module.FremoNumber"/> form the FREMO designation.
    /// </summary>
    public string? Prefix { get; init; }

    /// <summary>The owned share, 0–1, if known.</summary>
    public double? Share { get; init; }

    public int? RegistryPersonId { get; init; }
    public int? RegistryGroupId { get; init; }
}
