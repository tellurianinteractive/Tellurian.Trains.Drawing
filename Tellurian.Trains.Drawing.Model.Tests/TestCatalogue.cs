using Tellurian.Trains.Drawing.Model.Catalogues;
using Tellurian.Trains.Drawing.Model.EndProfiles;

namespace Tellurian.Trains.Drawing.Model.Tests;

/// <summary>A small catalogue for auto-draw tests, with hand-made end profiles (see <see cref="TestProfiles"/>).</summary>
internal static class TestCatalogue
{
    public static readonly Guid FremoId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid MmmId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid SummerId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    public static readonly EndProfile FremoSingle = TestProfiles.FremoSingle(Guid.Parse("20000000-0000-0000-0000-000000000001")).With(edited => { edited.Designation = "FREMO-H0-B96"; });
    public static readonly EndProfile FremoDouble = TestProfiles.Double(45).With(edited => { edited.Id = Guid.Parse("20000000-0000-0000-0000-000000000002"); edited.Designation = "FREMO-H0-2E96"; });
    public static readonly EndProfile FremoDoubleWide = TestProfiles.Double(55).With(edited => { edited.Id = Guid.Parse("20000000-0000-0000-0000-000000000003"); edited.Designation = "TEST-2-55"; });
    public static readonly EndProfile Mmm480 = TestProfiles.MmmSingle().With(edited => { edited.Id = Guid.Parse("20000000-0000-0000-0000-000000000004"); edited.Designation = "MMM480"; });

    public static readonly Catalogue Instance = new()
    {
        Scales = [new() { ShortName = "H0", Denominator = 87 }],
        Standards =
        [
            new()
            {
                Id = FremoId,
                ShortName = "FREMO/H0-RE",
                Scale = new() { ShortName = "H0" },
                Gauges = [new(TestProfiles.H0Gauge)],
                DefaultCurveRadius = 2000,
                MinimumReverseCurveRadius = 1000,
                DefaultSingleTrackEndProfileId = FremoSingle.Id,
                DefaultDoubleTrackEndProfileId = FremoDouble.Id,
            },
            new()
            {
                Id = MmmId,
                ShortName = "MMM-2R",
                Scale = new() { ShortName = "H0" },
                Gauges = [new(TestProfiles.H0Gauge)],
                DefaultSingleTrackEndProfileId = Mmm480.Id,
            },
        ],
        AddOns = [new() { Id = SummerId, ShortName = "MMM-Summer", BaseStandardId = MmmId }],
        EndProfiles = [FremoSingle, FremoDouble, FremoDoubleWide, Mmm480],
    };

    public static EndProfile? Lookup(Guid id) => Instance.FindEndProfile(id);
}
