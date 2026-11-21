using Tellurian.Trains.Drawing.Model.Measures;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>The derived measures of one module variant, as written in a module file.</summary>
public sealed record VariantMeasures(Guid VariantId, ModuleMeasures Measures);
