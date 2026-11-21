using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.HallPlans;

/// <summary>
/// An exit of a placed module, by module id and the exit's side and number on that side, e.g. Right 2 (HP-7).
/// Exit labels are not used: they are free text in the module drawing.
/// </summary>
/// <param name="ModuleId">The placed module.</param>
/// <param name="Side">The side of the module the exit is on.</param>
/// <param name="Number">The number of the exit among those on the side, from 1.</param>
public sealed record ExitReference(Guid ModuleId, ExitDirection Side, int Number = 1) : ExtensibleRecord
{
    /// <summary>The exit of <paramref name="moduleId"/> at <paramref name="place"/>.</summary>
    public static ExitReference To(Guid moduleId, ExitPlace place)
    {
        ArgumentNullException.ThrowIfNull(place);
        return new(moduleId, place.Side, place.Number);
    }

    /// <summary>The side and number, without the module.</summary>
    [JsonIgnore]
    public ExitPlace Place => new(Side, Number);

    public override string ToString() => $"{ModuleId}:{Side} {Number}";
}
