using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Modules;

namespace Tellurian.Trains.Drawing.Model.AutoDraw;

/// <summary>An exit with its end profile found in the catalogue (or inferred).</summary>
internal sealed record ResolvedExit(string Label, ExitDirection Direction, EndProfile Profile)
{
    public double PlateWidth { get; } = EndProfileAnalysis.PlateWidth(Profile);
}
