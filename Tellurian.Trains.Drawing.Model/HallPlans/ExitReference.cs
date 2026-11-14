namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>An exit of a placed module, by module id and exit label; labels are kept across variants (HP-7).</summary>
public sealed record ExitReference(Guid ModuleId, string ExitLabel)
{
    public override string ToString() => $"{ModuleId}:{ExitLabel}";
}
