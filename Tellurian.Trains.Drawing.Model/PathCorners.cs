using Tellurian.Trains.Schedules.Model.Geometry;

namespace Tellurian.Trains.Drawing.Model;

/// <summary>
/// A <see cref="PathDrawing"/> as its corners and the sweep of each step, for editing corner by corner, e.g. the
/// walls of a hall. Step <c>i</c> goes from corner <c>i</c> to corner <c>i + 1</c>; in a closed path the last step
/// returns to corner 0. A sweep of 0 is a straight step; otherwise the step is an arc turning that many degrees
/// (positive counter-clockwise), so an arc keeps its sweep when its corners move.
/// </summary>
public sealed record PathCorners
{
    public PathCorners(IReadOnlyList<Point> corners, IReadOnlyList<double> sweeps, bool isClosed)
    {
        ArgumentNullException.ThrowIfNull(corners);
        ArgumentNullException.ThrowIfNull(sweeps);
        if (corners.Count < (isClosed ? 3 : 2)) throw new ArgumentException(isClosed ? "A closed path needs at least three corners." : "A path needs at least two corners.", nameof(corners));
        if (sweeps.Count != (isClosed ? corners.Count : corners.Count - 1)) throw new ArgumentException("There must be one sweep per step.", nameof(sweeps));
        Corners = corners;
        Sweeps = sweeps;
        IsClosed = isClosed;
    }

    public IReadOnlyList<Point> Corners { get; }
    public IReadOnlyList<double> Sweeps { get; }
    public bool IsClosed { get; }

    public int StepCount => Sweeps.Count;

    /// <summary>The fewest corners a path of this kind can have.</summary>
    public int MinimumCorners => IsClosed ? 3 : 2;

    /// <summary>The corners and sweeps of <paramref name="drawing"/>.</summary>
    public static PathCorners From(PathDrawing drawing)
    {
        ArgumentNullException.ThrowIfNull(drawing);
        var segments = drawing.ToPath().Segments;
        var corners = new List<Point>(segments.Count + 1) { drawing.Start };
        corners.AddRange(segments.Select(s => s.End));
        if (drawing.IsClosed) corners.RemoveAt(corners.Count - 1);
        return new(corners, [.. drawing.Steps.Select(s => s is ArcStep arc ? arc.Sweep : 0)], drawing.IsClosed);
    }

    /// <summary>The drawing of these corners and sweeps.</summary>
    public PathDrawing ToDrawing()
    {
        var steps = new List<PathStep>(StepCount);
        for (var i = 0; i < StepCount; i++)
        {
            var from = Corners[i];
            var to = Corners[(i + 1) % Corners.Count];
            steps.Add(ArcCenter(from, to, Sweeps[i]) is { } center ? new ArcStep(center, Sweeps[i]) : new LineStep(to));
        }
        return new() { Start = Corners[0], Steps = steps, IsClosed = IsClosed };
    }

    /// <summary>The corner where step <paramref name="step"/> ends.</summary>
    public int EndOf(int step) => (step + 1) % Corners.Count;

    /// <summary>The length of step <paramref name="step"/>, along its arc if it is one.</summary>
    public double LengthOf(int step) => ToDrawing().ToPath().Segments[step].Length;

    public PathCorners WithCorner(int index, Point point) =>
        new([.. Corners.Select((c, i) => i == index ? point : c)], Sweeps, IsClosed);

    /// <summary>Moves the corners in <paramref name="indexes"/> by <paramref name="offset"/>.</summary>
    public PathCorners WithCornersMoved(IEnumerable<int> indexes, Vector offset)
    {
        var moved = indexes.ToHashSet();
        return moved.Count == 0 ? this : new([.. Corners.Select((c, i) => moved.Contains(i) ? c + offset : c)], Sweeps, IsClosed);
    }

    /// <summary>Makes step <paramref name="step"/> an arc of <paramref name="sweep"/> degrees, or straight with 0.</summary>
    public PathCorners WithSweep(int step, double sweep) =>
        new(Corners, [.. Sweeps.Select((s, i) => i == step ? sweep : s)], IsClosed);

    /// <summary>
    /// Sets the length of the straight step <paramref name="step"/> by moving its end corner along the step;
    /// null if the step is an arc, has no length or <paramref name="length"/> isn't positive.
    /// </summary>
    public PathCorners? WithLength(int step, double length)
    {
        if (length <= 0 || Sweeps[step] != 0) return null;
        var from = Corners[step];
        var direction = Corners[EndOf(step)] - from;
        if (direction.Length < Tolerance.Length) return null;
        return WithCorner(EndOf(step), from + direction.Normalized * length);
    }

    /// <summary>Splits step <paramref name="step"/> in two at its middle; an arc becomes two arcs of half the sweep.</summary>
    public PathCorners WithCornerInserted(int step)
    {
        var middle = ToDrawing().ToPath().Segments[step] is var segment ? segment.PointAt(segment.Length / 2) : default;
        var corners = Corners.ToList();
        var sweeps = Sweeps.ToList();
        corners.Insert(step + 1, middle);
        sweeps[step] /= 2;
        sweeps.Insert(step + 1, sweeps[step]);
        return new(corners, sweeps, IsClosed);
    }

    /// <summary>
    /// Removes corner <paramref name="index"/>; the steps on each side become one straight step. Null when the path
    /// would have too few corners.
    /// </summary>
    public PathCorners? WithoutCorner(int index)
    {
        if (Corners.Count <= MinimumCorners || index < 0 || index >= Corners.Count) return null;
        var corners = Corners.ToList();
        var sweeps = Sweeps.ToList();
        corners.RemoveAt(index);
        if (IsClosed)
        {
            sweeps[(index - 1 + sweeps.Count) % sweeps.Count] = 0;
            sweeps.RemoveAt(index);
        }
        else if (index == 0) sweeps.RemoveAt(0);
        else if (index == Corners.Count - 1) sweeps.RemoveAt(sweeps.Count - 1);
        else
        {
            sweeps[index - 1] = 0;
            sweeps.RemoveAt(index);
        }
        return new(corners, sweeps, IsClosed);
    }

    /// <summary>
    /// The centre, size and rotation if the path is a closed rectangle of straight steps, e.g. a rectangular pillar
    /// or hall: the width runs along the first step, turned <paramref name="rotation"/> from the X axis, and the
    /// depth along the last step back to corner 0.
    /// </summary>
    public bool TryGetRectangle(out Point center, out double width, out double depth, out Angle rotation)
    {
        center = default;
        width = depth = 0;
        rotation = default;
        if (!IsClosed || Corners.Count != 4 || Sweeps.Any(s => s != 0)) return false;
        Vector along = Corners[1] - Corners[0], across = Corners[3] - Corners[0];
        if (along.Length < Tolerance.Length || across.Length < Tolerance.Length) return false;
        // A rectangle: the opposite corner closes the parallelogram and the sides are perpendicular.
        if (!Corners[2].IsCloseTo(Corners[0] + along + across, Tolerance.Length) || Math.Abs(along.Normalized.Dot(across.Normalized)) > Tolerance.Length) return false;
        center = Point.Lerp(Corners[0], Corners[2], 0.5);
        width = along.Length;
        depth = across.Length;
        rotation = along.Direction;
        return true;
    }

    /// <summary>
    /// The centre of the arc from <paramref name="from"/> to <paramref name="to"/> turning <paramref name="sweep"/>
    /// degrees; null for a straight step, or when the ends are too close for an arc.
    /// </summary>
    public static Point? ArcCenter(Point from, Point to, double sweep)
    {
        var chord = to - from;
        if (sweep == 0 || Math.Abs(sweep) >= 360 || chord.Length < Tolerance.Length) return null;
        // The centre lies on the chord's perpendicular bisector, (chord / 2) / tan(sweep / 2) to the left.
        var halfSweep = sweep * Math.PI / 360;
        return Point.Lerp(from, to, 0.5) + chord.Normalized.LeftNormal * (chord.Length / 2 / Math.Tan(halfSweep));
    }
}
