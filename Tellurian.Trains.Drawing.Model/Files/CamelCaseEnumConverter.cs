using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tellurian.Trains.Drawing.Model.Files;

/// <summary>
/// Writes enum values as camelCase strings, e.g. <c>"endProfile"</c>, <c>"twoRail"</c>. Not used for drawing files, which
/// have their enum values as in the code (<see cref="DrawingJsonContext"/>); the app uses it for what it stores itself
/// and exchanges with its scripts.
/// </summary>
public sealed class CamelCaseEnumConverter<TEnum>() : JsonStringEnumConverter<TEnum>(JsonNamingPolicy.CamelCase)
    where TEnum : struct, Enum;
