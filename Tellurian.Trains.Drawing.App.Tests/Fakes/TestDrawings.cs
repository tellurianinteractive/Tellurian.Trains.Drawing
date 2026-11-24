using Tellurian.Trains.Drawing.Model.Catalogues;
using Tellurian.Trains.Drawing.Model.Files;

namespace Tellurian.Trains.Drawing.App.Tests.Fakes;

/// <summary>Drawing files for app tests.</summary>
internal static class TestDrawings
{
    public static (Guid Id, string Json) Hall(string name = "Club hall", int revision = 0)
    {
        var hall = Tellurian.Trains.Schedules.Model.Layouts.Halls.Hall.Rectangle(name, 10000, 6000).With(h => h.Revision = revision);
        return (hall.Id, DrawingFileSerializer.Serialize(hall));
    }

    public static string EndProfile() =>
        DrawingFileSerializer.Serialize(SampleCatalogue.Instance.EndProfiles[0]);

    public static Hall ReadHall(string json) => DrawingFileSerializer.DeserializeHall(json);
}
