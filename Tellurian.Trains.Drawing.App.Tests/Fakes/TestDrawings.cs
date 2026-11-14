using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.Files;
using Tellurian.Trains.Drawing.Model.Halls;

namespace Tellurian.Trains.Drawing.App.Tests.Fakes;

/// <summary>Drawing files for app tests.</summary>
internal static class TestDrawings
{
    public static (Guid Id, string Json) Hall(string name = "Club hall", int revision = 0)
    {
        var hall = Model.Halls.Hall.Rectangle(name, 10000, 6000) with { Revision = revision };
        return (hall.Id, DrawingFileSerializer.Serialize(hall));
    }

    public static string EndProfile() =>
        DrawingFileSerializer.Serialize(BuiltInCatalogue.Instance.EndProfiles[0]);

    public static Hall ReadHall(string json) => DrawingFileSerializer.DeserializeHall(json);
}
