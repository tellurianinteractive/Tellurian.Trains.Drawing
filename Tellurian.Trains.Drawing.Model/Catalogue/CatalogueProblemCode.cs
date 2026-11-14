namespace Tellurian.Trains.Drawing.Model.Catalogue;

/// <summary>Why a catalogue entry can't be saved; UIs show a text per code.</summary>
public enum CatalogueProblemCode
{
    ShortNameMissing,

    /// <summary>Another entry of the same kind (and base standard, for add-ons) has the short name.</summary>
    ShortNameInUse,

    ScaleUnknown,

    /// <summary>A length that must be greater than zero isn't.</summary>
    NotPositive,

    /// <summary>A default end profile doesn't exist or doesn't belong to the standard.</summary>
    EndProfileNotOfStandard,

    TrackColorInvalid,
    BaseStandardUnknown,
    EndProfileUnknown,

    /// <summary>An override needs two different end profiles.</summary>
    SameEndProfile,

    /// <summary>An override's end profiles are of different scales.</summary>
    DifferentScales,

    /// <summary>There already is an override for the pair.</summary>
    OverrideExists,
}
