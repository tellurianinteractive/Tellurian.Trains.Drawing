using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model;

namespace Tellurian.Trains.Drawing.App.Sandbox;

/// <summary>The sandbox's test document: a few shapes to pan, zoom, select, move and undo.</summary>
public sealed record SandboxDocument(IReadOnlyList<SandboxShape> Shapes)
{
    public static SandboxDocument Sample { get; } = new(
    [
        new(Guid.NewGuid(), PathDrawing.Rectangle(new(600, 250), 1200, 500, Angle.Zero)),
        new(Guid.NewGuid(), PathDrawing.Rectangle(new(2000, 600), 800, 300, new(30))),
        new(Guid.NewGuid(), PathDrawing.Circle(new(3200, 300), 400)),
        new(Guid.NewGuid(), PathDrawing.From(new PathBuilder(new Pose(new(0, 1200), Angle.Zero)).Straight(1000).Curve(2000, 30).Build())),
    ]);

    public SandboxShape? Find(Guid id) => Shapes.FirstOrDefault(s => s.Id == id);

    public BoundingBox Bounds => Shapes.Select(s => s.Path.ToPath().Bounds).Aggregate(BoundingBox.Empty, (a, b) => a.Union(b));
}
