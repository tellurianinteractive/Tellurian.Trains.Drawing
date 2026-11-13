using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>Hand-made end profiles for tests. The values are plausible but not taken from the real standards.</summary>
internal static class TestProfiles
{
    public const double H0Gauge = 16.5;

    /// <summary>FREMO-like H0 single track: 500 mm plate, four symmetric screw holes.</summary>
    public static EndProfile FremoSingle(Guid? id = null) => EndProfileTemplate.Create(new()
    {
        Id = id,
        Designation = "TEST-FREMO-1",
        Scale = "H0",
        PlateWidth = 500,
        PlateTop = -40,
        PlateBottom = -140,
        Gauge = H0Gauge,
        ScrewHoles = [new(-150, -70), new(150, -70), new(-150, -110), new(150, -110)],
        ScrewHolePattern = "FREMO-H0",
    });

    /// <summary>MMM-like H0 single track: 480 mm plate, same track, other screw holes.</summary>
    public static EndProfile MmmSingle() => EndProfileTemplate.Create(new()
    {
        Designation = "TEST-MMM480",
        Scale = "H0",
        PlateWidth = 480,
        PlateTop = -40,
        PlateBottom = -120,
        Gauge = H0Gauge,
        ScrewHoles = [new(-200, -80), new(200, -80)],
        ScrewHolePattern = "MMM",
    });

    public static EndProfile Double(double centreDistance, RailSystem railSystem = RailSystem.TwoRail) => EndProfileTemplate.Create(new()
    {
        Designation = $"TEST-DOUBLE-{centreDistance}",
        Scale = "H0",
        PlateWidth = 500,
        PlateTop = -40,
        PlateBottom = -140,
        Gauge = H0Gauge,
        RailSystem = railSystem,
        TrackOffsets = EndProfileTemplate.CentredOffsets(2, centreDistance),
        ScrewHoles = [new(-150, -70), new(150, -70)],
    });

    /// <summary>
    /// Like <see cref="FremoSingle"/>, but with an embankment on the left: the ground line is 30 mm
    /// higher on the left half than on the right. Asymmetric in the ground line only.
    /// </summary>
    public static EndProfile Asymmetric() => FremoSingle() with
    {
        Id = Guid.NewGuid(),
        Designation = "TEST-ASYM",
        GroundLine = PathDrawing.Polyline([new(-250, -10), new(-60, -10), new(-30, -40), new(250, -40)]),
    };

    /// <summary>Single track with a single screw hole to the right: asymmetric in the screw holes.</summary>
    public static EndProfile AsymmetricHoles() => FremoSingle() with
    {
        Id = Guid.NewGuid(),
        Designation = "TEST-ASYM-HOLES",
        ScrewHoles = [new Circle(new Point(150, -70), 8)],
    };

    public static EndProfile WithoutScrewHoles(string? pattern) => FremoSingle() with
    {
        Id = Guid.NewGuid(),
        ScrewHoles = [],
        ScrewHolePattern = pattern,
    };
}
