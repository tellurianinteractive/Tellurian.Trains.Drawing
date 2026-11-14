using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>Writes enum values as camelCase strings, e.g. <c>"endProfile"</c>, <c>"twoRail"</c>.</summary>
public sealed class CamelCaseEnumConverter<TEnum>() : JsonStringEnumConverter<TEnum>(JsonNamingPolicy.CamelCase)
    where TEnum : struct, Enum;
