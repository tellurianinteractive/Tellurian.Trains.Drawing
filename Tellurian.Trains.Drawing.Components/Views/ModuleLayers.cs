namespace Tellurian.Trains.Drawing.Components.Views;

/// <summary>The layers of a module view (EX-8, HP-14).</summary>
[Flags]
public enum ModuleLayers
{
    None = 0,

    /// <summary>The baseboard sections and the internal joints between them.</summary>
    Sections = 1 << 0,

    /// <summary>The end plates at the exits.</summary>
    EndPlates = 1 << 1,

    Tracks = 1 << 2,

    /// <summary>
    /// The end profile shading at the exits (dark above rail top, light below), with the end profiles' short names.
    /// </summary>
    Exits = 1 << 3,

    OperatorSpaces = 1 << 4,
    PublicSides = 1 << 5,
    DuckUnderAreas = 1 << 6,
    Labels = 1 << 7,

    /// <summary>The module's FREMO designation or name, with the owner's name, along the track (MD-23).</summary>
    Name = 1 << 8,

    All = Sections | EndPlates | Tracks | Exits | OperatorSpaces | PublicSides | DuckUnderAreas | Labels | Name,
}
