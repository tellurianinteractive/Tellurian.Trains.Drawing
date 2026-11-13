using System.Text.Json.Serialization;
using Tellurian.Trains.Drawing.Geometry;

namespace Tellurian.Trains.Drawing.Model;

/// <summary>One step of a <see cref="PathDrawing"/>, starting where the previous step ends.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(LineStep), "line")]
[JsonDerivedType(typeof(ArcStep), "arc")]
public abstract record PathStep
{
    public abstract Segment ToSegment(Point2 start);

    public static PathStep From(Segment segment) => segment switch
    {
        LineSegment line => new LineStep(line.End),
        ArcSegment arc => new ArcStep(arc.Center, arc.SweepDegrees),
        _ => throw new NotSupportedException($"Unknown segment type {segment.GetType().Name}."),
    };
}
