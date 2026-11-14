using System.Text.Json;
using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>Writes an <see cref="Angle"/> as a plain number of degrees.</summary>
public sealed class AngleJsonConverter : JsonConverter<Angle>
{
    public override Angle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(reader.GetDouble());

    public override void Write(Utf8JsonWriter writer, Angle value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Degrees);
}
