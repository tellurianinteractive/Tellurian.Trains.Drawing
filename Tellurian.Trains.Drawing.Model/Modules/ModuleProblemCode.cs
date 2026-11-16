namespace Tellurian.Trains.Drawing.Model.Modules;

/// <summary>Why a module's properties can't be saved; UIs show a text per code.</summary>
public enum ModuleProblemCode
{
    /// <summary>A module without a FREMO number needs a name.</summary>
    NameMissing,

    /// <summary>A FREMO number needs the first owner's prefix (the FREMO convention).</summary>
    PrefixMissing,

    /// <summary>The FREMO number isn't 1–9999.</summary>
    FremoNumberOutOfRange,
}
