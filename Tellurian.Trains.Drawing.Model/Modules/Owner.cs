namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>A person or group that owns (part of) a module (Registry <c>ModuleOwnership</c>).</summary>
public sealed record Owner
{
    public required string Name { get; init; }

    /// <summary>The owned share, 0–1, if known.</summary>
    public double? Share { get; init; }

    public int? RegistryPersonId { get; init; }
    public int? RegistryGroupId { get; init; }
}
