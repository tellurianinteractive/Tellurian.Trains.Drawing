using Tellurian.Trains.Drawing.Geometry;
using Tellurian.Trains.Drawing.Model.Catalogue;
using Tellurian.Trains.Drawing.Model.EndProfiles;
using Tellurian.Trains.Drawing.Model.Modules;
using Tellurian.Trains.Drawing.Model.Tracks;

namespace Tellurian.Trains.Drawing.Model.AutoDraw;

/// <summary>One run of <see cref="ModuleAutoDraw.Create"/>.</summary>
internal sealed class AutoDrawGenerator(AutoDrawParameters parameters, DrawingCatalogue catalogue)
{
    private const string SummerSuffix = "/SSM";
    private const string SummerBaseStandard = "MMM-2R";
    private const string SummerAddOn = "MMM-Summer";
    private const double MinimumTaperAngle = 1;
    private const double MaximumTaperAngle = 89;
    private const double LoopAngle = 180;
    private const double FullTurn = 360;

    private readonly AutoDrawParameters _p = parameters;
    private readonly List<AutoDrawNote> _notes = [];
    private readonly Dictionary<Guid, EndProfile> _profiles = [];
    private AutoDrawStatus _status = AutoDrawStatus.Complete;

    public AutoDrawResult Run()
    {
        var (standard, addOns) = ResolveStandard();
        if (standard is null) return Fail(AutoDrawNoteCode.UnknownStandard, $"The standard {_p.Standard} isn't in the catalogue.");

        var (exits, internalJoints) = ResolveExits(standard);
        if (exits is null) return Result(null);

        var main = MainPath(standard);
        if (main is null) return Result(null);

        try
        {
            var variant = exits.Count == 1
                ? EndOfLine(standard, main, exits[0], internalJoints)
                : PassThroughOrJunction(standard, main, exits, internalJoints);
            return Result(Module(standard, addOns, variant));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return Fail(AutoDrawNoteCode.InvalidGeometry, $"The module can't be drawn from these parameters: {exception.Message}");
        }
    }

    private (ModuleStandard? Standard, IReadOnlyList<CatalogueReference> AddOns) ResolveStandard()
    {
        var name = _p.Standard.Trim();
        if (!name.EndsWith(SummerSuffix, StringComparison.OrdinalIgnoreCase)) return (catalogue.FindStandard(name), []);

        var standard = catalogue.FindStandard(SummerBaseStandard);
        if (standard is null) return (null, []);
        var addOn = catalogue.FindAddOn(standard.Id, SummerAddOn);
        Note(AutoDrawNoteCode.StandardMapped, $"{name} is drawn as {SummerBaseStandard} with the add-on {SummerAddOn}.");
        return (standard, addOn is null ? [] : [new(addOn.Id, addOn.ShortName)]);
    }

    private (IReadOnlyList<ResolvedExit>? Exits, int InternalJoints) ResolveExits(ModuleStandard standard)
    {
        var exits = new List<ResolvedExit>();
        var internalJoints = 0;
        foreach (var exit in _p.Exits)
        {
            if (exit.EndProfileId is null && string.Equals(exit.EndProfile.Trim(), ModuleAutoDraw.InternalEndProfile, StringComparison.OrdinalIgnoreCase))
            {
                internalJoints++;
                continue;
            }
            var profile = (exit.EndProfileId is { } id ? catalogue.FindEndProfile(id) : null) ?? catalogue.FindEndProfile(exit.EndProfile, standard.Scale);
            if (profile is null)
            {
                profile = DefaultEndProfile(standard);
                if (profile is null) return (null, 0);
                Note(AutoDrawNoteCode.EndProfileNotFound, $"The end profile {exit.EndProfile} of exit {exit.Label} isn't in the catalogue; {profile.Designation} is used.");
            }
            exits.Add(new(exit.Label, exit.Direction, profile));
        }
        if (internalJoints > 0)
            Note(AutoDrawNoteCode.InternalExitsBecameJoints, $"{internalJoints} exit(s) with the {ModuleAutoDraw.InternalEndProfile} end profile are drawn as joints between sections.");

        if (exits.Count == 0)
        {
            var profile = DefaultEndProfile(standard);
            if (profile is null) return (null, 0);
            exits.AddRange(_p.IsEndOfLine
                ? [new("A", ExitDirection.Undefined, profile)]
                : [new("W", ExitDirection.West, profile), new("E", ExitDirection.East, profile)]);
            Note(AutoDrawNoteCode.ExitsInferred, $"Exits inferred: {exits.Count} × {profile.Designation}.");
        }
        else if (exits.Count >= 2 && exits.All(e => e.Direction == ExitDirection.Undefined))
        {
            Note(AutoDrawNoteCode.ExitDirectionsUndefined, $"Exit directions are undefined; exit {exits[0].Label} is drawn as the first exit.");
        }

        if (_p.IsEndOfLine && exits.Count > 1)
            Note(AutoDrawNoteCode.EndOfLineFlagMismatch, $"The module is flagged as end of line but has {exits.Count} exits.");
        else if (!_p.IsEndOfLine && exits.Count == 1)
            Note(AutoDrawNoteCode.EndOfLineFlagMismatch, "The module has one exit but isn't flagged as end of line.");

        foreach (var exit in exits)
        {
            if (exit.PlateWidth < Tolerance.Length)
            {
                Note(AutoDrawNoteCode.EndProfileWithoutOutline, $"The end profile {exit.Profile.Designation} has no end-plate outline, so its width is unknown.", AutoDrawStatus.NeedsManualDrawing);
                return (null, 0);
            }
            _profiles[exit.Profile.Id] = exit.Profile;
        }
        return (exits, internalJoints);
    }

    private EndProfile? DefaultEndProfile(ModuleStandard standard)
    {
        var doubleTrack = (_p.NumberOfThroughTracks ?? 1) >= 2;
        var id = doubleTrack ? standard.DefaultDoubleTrackEndProfileId : standard.DefaultSingleTrackEndProfileId;
        var profile = id is { } value ? catalogue.FindEndProfile(value) : null;
        if (profile is null)
            Note(AutoDrawNoteCode.MissingDefaultEndProfile, $"The standard {standard.ShortName} has no default {(doubleTrack ? "double" : "single")}-track end profile in the catalogue.", AutoDrawStatus.NeedsManualDrawing);
        return profile;
    }

    private SegmentPath? MainPath(ModuleStandard standard)
    {
        var straight = Math.Max(0, _p.Straight ?? 0);
        var angle = Math.Abs(_p.Angle ?? 0);
        var radius = _p.Radius is > 0 ? _p.Radius.Value : (double?)null;
        if (angle >= Tolerance.Angle && radius is null)
        {
            radius = standard.DefaultCurveRadius;
            if (radius is null)
            {
                Note(AutoDrawNoteCode.NoGeometry, $"The module has an angle but no radius, and {standard.ShortName} has no default radius.", AutoDrawStatus.NeedsManualDrawing);
                return null;
            }
            Note(AutoDrawNoteCode.DefaultRadiusUsed, $"The module has an angle but no radius; the default radius {radius:0} mm of {standard.ShortName} is used.");
        }
        if (straight < Tolerance.Length && angle < Tolerance.Angle)
        {
            Note(AutoDrawNoteCode.NoGeometry, "The module has no straight length and no curve.", AutoDrawStatus.NeedsManualDrawing);
            return null;
        }
        if (angle >= FullTurn - Tolerance.Angle || _p.IsTurntable)
            Note(AutoDrawNoteCode.CheckAngleOrTurntable, _p.IsTurntable ? "Turntable module: drawn as far as possible." : $"An angle of {angle:0.#}° is drawn as far as possible.", AutoDrawStatus.Check);

        var turn = _p.CurveDirection == CurveDirection.Left ? angle : -angle;
        var builder = new PathBuilder(Pose.Origin);
        return (_p.StraightPosition switch
        {
            StraightPosition.After => builder.Curve(radius ?? 0, turn).Straight(straight),
            StraightPosition.Split => builder.Straight(straight / 2).Curve(radius ?? 0, turn).Straight(straight / 2),
            _ => builder.Straight(straight).Curve(radius ?? 0, turn),
        }).Build();
    }

    private ModuleVariant PassThroughOrJunction(ModuleStandard standard, SegmentPath main, IReadOnlyList<ResolvedExit> exits, int internalJoints)
    {
        var first = exits.FirstOrDefault(e => e.Direction == ExitDirection.West) ?? exits[0];
        var rest = exits.Where(e => e != first).ToList();
        var second = rest.FirstOrDefault(e => e.Direction == ExitDirection.East) ?? rest[0];
        var extras = rest.Where(e => e != second).ToList();

        var firstExit = Exit(first, main.StartPose.Reversed());
        var secondExit = Exit(second, main.EndPose);
        var length = main.Length;

        if (extras.Count > 0) return Junction(standard, main, firstExit, secondExit, first, second, extras, internalJoints);

        var (right, left) = SideProfiles(main, first.PlateWidth / 2, second.PlateWidth / 2, Math.Max(first.PlateWidth, second.PlateWidth), isEndOfLine: false);
        var variant = new ModuleVariant
        {
            Exits = [firstExit, secondExit],
            MainPath = PathDrawing.From(main),
            Sides =
            [
                new() { FromExit = first.Label, ToExit = second.Label, Path = PathDrawing.From(SideBuilder.Build(main, right, 0, length)) },
                new() { FromExit = second.Label, ToExit = first.Label, Path = PathDrawing.From(SideBuilder.Build(main, left, 0, length).Reversed()) },
            ],
            Tracks = ThroughTracks(standard, main, firstExit, first.Profile, secondExit, second.Profile),
        };
        return Finish(standard, main, variant, right, left, internalJoints);
    }

    private ModuleVariant EndOfLine(ModuleStandard standard, SegmentPath main, ResolvedExit only, int internalJoints)
    {
        var exit = Exit(only, main.StartPose.Reversed());
        var length = main.Length;
        var (right, left) = SideProfiles(main, only.PlateWidth / 2, only.PlateWidth / 2, only.PlateWidth, isEndOfLine: true);
        var rightSide = SideBuilder.Build(main, right, 0, length);
        var leftSide = SideBuilder.Build(main, left, 0, length).Reversed();
        var side = new SegmentPath([.. rightSide.Segments, new LineSegment(rightSide.End, leftSide.Start), .. leftSide.Segments]);

        var isLoop = Math.Abs(_p.Angle ?? 0) >= LoopAngle - Tolerance.Angle;
        if (isLoop)
            Note(AutoDrawNoteCode.LoopModule, "One exit and a curve of 180° or more: probably a loop module. The return-loop track is drawn by hand.", AutoDrawStatus.Check);

        var variant = new ModuleVariant
        {
            Exits = [exit],
            MainPath = PathDrawing.From(main),
            Sides = [new() { FromExit = only.Label, ToExit = only.Label, Path = PathDrawing.From(side) }],
            Tracks = isLoop ? [] : TerminusTracks(standard, main, exit, only.Profile),
        };
        return Finish(standard, main, variant, right, left, internalJoints, rightSide.Length);
    }

    private ModuleVariant Junction(ModuleStandard standard, SegmentPath main, ModuleExit firstExit, ModuleExit secondExit,
        ResolvedExit first, ResolvedExit second, IReadOnlyList<ResolvedExit> extras, int internalJoints)
    {
        var spacing = Math.Max(first.PlateWidth, second.PlateWidth) + ModuleAutoDraw.ProvisionalExitGap;
        var east = extras.Where(e => e.Direction != ExitDirection.West).ToList();
        var west = extras.Where(e => e.Direction == ExitDirection.West).ToList();
        var endPose = main.EndPose;
        var eastExits = east.Select((e, i) => Exit(e, endPose with { Position = endPose.Position + endPose.Left * (spacing * (i + 1)) }, provisional: true));
        var westExits = west.Select((e, i) => Exit(e, firstExit.Pose with { Position = main.StartPose.Position + main.StartPose.Left * (spacing * (i + 1)) }, provisional: true)).Reverse();
        IReadOnlyList<ModuleExit> ordered = [firstExit, secondExit, .. eastExits, .. westExits];

        var plates = ordered.ToDictionary(e => e.Label, e => ModuleGeometry.EndPlate(e, _profiles[e.EndProfile.Id]));
        var sides = ordered.Select((exit, i) =>
        {
            var next = ordered[(i + 1) % ordered.Count];
            return new ModuleSide { FromExit = exit.Label, ToExit = next.Label, Path = PathDrawing.Polyline([plates[exit.Label].RightCorner, plates[next.Label].LeftCorner]) };
        }).ToList();

        Note(AutoDrawNoteCode.JunctionNeedsCompletion, $"Junction: the exits {string.Join(", ", extras.Select(e => e.Label))} are placed provisionally; place them and draw the track work by hand.", AutoDrawStatus.NeedsManualCompletion);

        var variant = new ModuleVariant
        {
            Exits = ordered,
            MainPath = PathDrawing.From(main),
            Sides = sides,
            Tracks = ThroughTracks(standard, main, firstExit, first.Profile, secondExit, second.Profile),
        };
        var outline = ModuleGeometry.Outline(variant, Lookup)!;
        if (Math.Max(_p.NumberOfSections ?? 1, internalJoints + 1) > 1)
            Note(AutoDrawNoteCode.SectionsNotGenerated, "Sections of junction modules are drawn by hand; the module is drawn as one section.");

        variant = variant with { Sections = [new() { Label = "1", Outline = PathDrawing.From(outline) }] };
        if (_p.IsOperationPlace)
        {
            var side = sides[0].Path.ToPath();
            var outward = -(side.End - side.Start).Normalized.LeftNormal * standard.OperatorSpaceDepth;
            variant = variant with
            {
                OperatorSpaces = [new() { Attachment = new() { SideIndex = 0 }, Depth = standard.OperatorSpaceDepth, Area = PathDrawing.Polyline([side.Start + outward, side.End + outward, side.End, side.Start], isClosed: true) }],
            };
        }
        return WithDuckUnder(variant, outline);
    }

    /// <summary>Sections, internal joints, operator space and duck-under area for pass-through and end-of-line modules.</summary>
    private ModuleVariant Finish(ModuleStandard standard, SegmentPath main, ModuleVariant variant, OffsetProfile right, OffsetProfile left, int internalJoints, double? operatorSideLength = null)
    {
        var length = main.Length;
        var count = Math.Max(1, Math.Max(_p.NumberOfSections ?? 1, internalJoints + 1));
        var cuts = Enumerable.Range(0, count + 1).Select(k => length * k / count).ToList();
        var sections = new List<Section>();
        var joints = new List<InternalJoint>();
        for (var k = 0; k < count; k++)
        {
            var r = SideBuilder.Build(main, right, cuts[k], cuts[k + 1]);
            var l = SideBuilder.Build(main, left, cuts[k], cuts[k + 1]).Reversed();
            var outline = new SegmentPath([.. r.Segments, new LineSegment(r.End, l.Start), .. l.Segments, new LineSegment(l.End, r.Start)], isClosed: true);
            sections.Add(new() { Label = $"{k + 1}", Outline = PathDrawing.From(outline) });
            if (k > 0)
            {
                joints.Add(new()
                {
                    FirstSection = $"{k}",
                    SecondSection = $"{k + 1}",
                    Start = SideBuilder.Beside(main, cuts[k], right.At(cuts[k])),
                    End = SideBuilder.Beside(main, cuts[k], left.At(cuts[k])),
                });
            }
        }
        variant = variant with { Sections = sections, InternalJoints = joints };

        if (_p.IsOperationPlace)
        {
            var depth = standard.OperatorSpaceDepth;
            // Counter-clockwise, like all areas: along the outer edge, then back along the side.
            var outer = SideBuilder.Build(main, right.Shifted(-depth), 0, length);
            var inner = SideBuilder.Build(main, right, 0, length).Reversed();
            var area = new SegmentPath([.. outer.Segments, new LineSegment(outer.End, inner.Start), .. inner.Segments, new LineSegment(inner.End, outer.Start)], isClosed: true);
            variant = variant with
            {
                OperatorSpaces = [new() { Attachment = new() { SideIndex = 0, To = operatorSideLength }, Depth = depth, Area = PathDrawing.From(area) }],
            };
        }
        return WithDuckUnder(variant, ModuleGeometry.Outline(variant, Lookup)!);
    }

    private ModuleVariant WithDuckUnder(ModuleVariant variant, SegmentPath outline) =>
        _p.IsDuckunder ? variant with { DuckUnderAreas = [new() { Area = PathDrawing.From(outline) }] } : variant;

    /// <summary>The right and left side offsets, including a widened body (ModuleDrawing §5.3).</summary>
    private (OffsetProfile Right, OffsetProfile Left) SideProfiles(SegmentPath main, double startHalfWidth, double endHalfWidth, double plateWidth, bool isEndOfLine)
    {
        var length = main.Length;
        var right = OffsetProfile.Linear(length, -startHalfWidth, -endHalfWidth);
        var left = OffsetProfile.Linear(length, startHalfWidth, endHalfWidth);
        var width = _p.Width ?? plateWidth;
        if (width < plateWidth - Tolerance.Length)
        {
            Note(AutoDrawNoteCode.WidthBelowPlateIgnored, $"The width {width:0} mm is smaller than the end plate ({plateWidth:0} mm) and is ignored.");
            return (right, left);
        }
        if (width <= plateWidth + Tolerance.Length) return (right, left);

        var target = width - Math.Max(startHalfWidth, endHalfWidth);
        var widened = Widened(length, startHalfWidth, isEndOfLine ? target : endHalfWidth, target, isEndOfLine);
        return _p.WideningSide == WideningSide.Left ? (right, widened) : (widened.Negated(), left);
    }

    private OffsetProfile Widened(double length, double start, double end, double target, bool isEndOfLine)
    {
        var slope = Math.Tan(Math.Clamp(_p.TaperAngle, MinimumTaperAngle, MaximumTaperAngle) * Math.PI / 180);
        var startTaper = (target - start) / slope;
        var endTaper = isEndOfLine ? 0 : (target - end) / slope;
        if (startTaper + endTaper <= length)
            return OffsetProfile.FromKnots((0, start), (startTaper, target), (length - endTaper, target), (length, isEndOfLine ? target : end));

        Note(AutoDrawNoteCode.WideningShortened, "The module is too short to reach its full width with the taper angle; it is narrower than the given width.");
        if (isEndOfLine) return OffsetProfile.FromKnots((0, start), (length, start + slope * length));
        var peakAt = Math.Clamp((end - start + slope * length) / (2 * slope), 0, length);
        return OffsetProfile.FromKnots((0, start), (peakAt, start + slope * peakAt), (length, end));
    }

    private IReadOnlyList<TrackElement> ThroughTracks(ModuleStandard standard, SegmentPath main, ModuleExit firstExit, EndProfile firstProfile, ModuleExit secondExit, EndProfile secondProfile)
    {
        if (_p.NumberOfThroughTracks == 0) return [];
        var atStart = Laterals(main, 0, firstExit, firstProfile);
        var atEnd = Laterals(main, main.Length, secondExit, secondProfile);
        CheckTrackCount(Math.Max(atStart.Count, atEnd.Count));

        var tracks = new List<TrackElement>();
        foreach (var (from, to) in PairTracks(atStart, atEnd, firstExit, secondExit))
        {
            if (Math.Abs(from - to) < Tolerance.Length)
            {
                tracks.Add(Generated(main.Offset(from)));
            }
            else if (ReverseCurves(standard, main, from, to) is { } path)
            {
                tracks.Add(Generated(path));
            }
            else
            {
                Note(AutoDrawNoteCode.TrackNotGenerated, $"The track from {from:0.#} mm to {to:0.#} mm beside the centre line can't be generated with reverse curves; draw it by hand.", AutoDrawStatus.NeedsManualCompletion);
            }
        }
        return tracks;
    }

    private IReadOnlyList<TrackElement> TerminusTracks(ModuleStandard standard, SegmentPath main, ModuleExit exit, EndProfile profile)
    {
        if (_p.NumberOfThroughTracks == 0) return [];
        var laterals = Laterals(main, 0, exit, profile);
        CheckTrackCount(laterals.Count);
        var trackLength = main.Length - ModuleAutoDraw.BufferStopMargin;
        if (trackLength < Tolerance.Length)
        {
            Note(AutoDrawNoteCode.TrackNotGenerated, "The module is too short for tracks with buffer stops.", AutoDrawStatus.NeedsManualCompletion);
            return [];
        }
        var centre = main.Between(0, trackLength);
        return [.. laterals.SelectMany(lateral =>
        {
            var path = centre.Offset(lateral);
            return new TrackElement[] { Generated(path), new BufferStop { Pose = path.EndPose.Reversed(), IsGenerated = true } };
        })];
    }

    private void CheckTrackCount(int fromProfiles)
    {
        if (_p.NumberOfThroughTracks is { } given && given != fromProfiles)
            Note(AutoDrawNoteCode.ThroughTracksMismatch, $"{given} through track(s) are registered, but the end profiles have {fromProfiles}; the end profiles are followed.");
    }

    /// <summary>The lateral offsets (positive left of the main path) of the tracks at an exit, from right to left.</summary>
    private static List<double> Laterals(SegmentPath main, double at, ModuleExit exit, EndProfile profile)
    {
        var pose = main.PoseAt(at);
        return [.. ModuleGeometry.TrackPositions(exit, profile).Select(t => (t.Position - pose.Position).Dot(pose.Left)).Order()];
    }

    /// <summary>
    /// Pairs track position i at the first exit with position i at the second. When the counts differ,
    /// the nearest positions are paired and the rest are reported.
    /// </summary>
    private List<(double From, double To)> PairTracks(List<double> atStart, List<double> atEnd, ModuleExit firstExit, ModuleExit secondExit)
    {
        if (atStart.Count == atEnd.Count) return [.. atStart.Zip(atEnd)];

        var candidates = atStart.SelectMany((from, i) => atEnd.Select((to, j) => (i, j, Distance: Math.Abs(from - to)))).OrderBy(c => c.Distance).ThenBy(c => c.i);
        var (usedStart, usedEnd) = (new HashSet<int>(), new HashSet<int>());
        var pairs = new List<(double, double)>();
        foreach (var (i, j, _) in candidates)
        {
            if (usedStart.Contains(i) || usedEnd.Contains(j)) continue;
            usedStart.Add(i);
            usedEnd.Add(j);
            pairs.Add((atStart[i], atEnd[j]));
        }
        foreach (var i in Enumerable.Range(0, atStart.Count).Where(i => !usedStart.Contains(i)))
            Note(AutoDrawNoteCode.TrackNotGenerated, $"The track at exit {firstExit.Label} ({atStart[i]:0.#} mm beside the centre line) has no counterpart at exit {secondExit.Label}; draw it by hand.", AutoDrawStatus.NeedsManualCompletion);
        foreach (var j in Enumerable.Range(0, atEnd.Count).Where(j => !usedEnd.Contains(j)))
            Note(AutoDrawNoteCode.TrackNotGenerated, $"The track at exit {secondExit.Label} ({atEnd[j]:0.#} mm beside the centre line) has no counterpart at exit {firstExit.Label}; draw it by hand.", AutoDrawStatus.NeedsManualCompletion);
        return pairs;
    }

    /// <summary>
    /// A track that moves sideways from <paramref name="from"/> to <paramref name="to"/> with two reverse
    /// curves centred on a straight module. Null if it doesn't fit, or if the module isn't straight.
    /// </summary>
    private static SegmentPath? ReverseCurves(ModuleStandard standard, SegmentPath main, double from, double to)
    {
        if (main.Segments.Any(s => s is not LineSegment)) return null;
        var radius = standard.MinimumReverseCurveRadius ?? ModuleAutoDraw.FallbackReverseCurveRadius;
        var shift = to - from;
        if (Math.Abs(shift) > 2 * radius) return null;
        var turn = Math.Acos(1 - Math.Abs(shift) / (2 * radius));
        var curvesLength = 2 * radius * Math.Sin(turn);
        if (curvesLength > main.Length) return null;
        var straight = (main.Length - curvesLength) / 2;
        var degrees = Math.Sign(shift) * turn * 180 / Math.PI;
        var start = main.StartPose with { Position = main.StartPose.Position + main.StartPose.Left * from };
        return new PathBuilder(start).Straight(straight).Curve(radius, degrees).Curve(radius, -degrees).Straight(straight).Build();
    }

    private static FlexTrack Generated(SegmentPath path) => new() { Path = PathDrawing.From(path), IsMainTrack = true, IsGenerated = true };

    private static ModuleExit Exit(ResolvedExit exit, Pose pose, bool provisional = false) => new()
    {
        Label = exit.Label,
        Direction = exit.Direction,
        EndProfile = new(exit.Profile.Id, exit.Profile.Designation),
        Pose = pose,
        IsPositionProvisional = provisional,
    };

    private Module Module(ModuleStandard standard, IReadOnlyList<CatalogueReference> addOns, ModuleVariant variant) => new()
    {
        Id = _p.ModuleId ?? Guid.NewGuid(),
        Name = _p.Name,
        Standard = new(standard.Id, standard.ShortName),
        Scale = standard.Scale,
        AddOns = addOns,
        NumberOfThroughTracks = _p.NumberOfThroughTracks,
        IsDuckunder = _p.IsDuckunder,
        IsTurntable = _p.IsTurntable,
        IsEndOfLine = _p.IsEndOfLine || variant.Exits.Count == 1,
        Variants =
        [
            variant with
            {
                Name = _p.ConfigurationLabel ?? string.Empty,
                IsDefault = true,
                RegistryModuleId = _p.RegistryModuleId,
                RegistryMeasures = new()
                {
                    Straight = _p.Straight,
                    Radius = _p.Radius,
                    Angle = _p.Angle,
                    Length = _p.Length,
                    Width = _p.Width,
                    NumberOfSections = _p.NumberOfSections,
                },
                AutoDraw = new() { GeneratorVersion = ModuleAutoDraw.GeneratorVersion, Parameters = _p },
            },
        ],
    };

    private EndProfile? Lookup(Guid id) => _profiles.GetValueOrDefault(id);

    private void Note(AutoDrawNoteCode code, string message, AutoDrawStatus status = AutoDrawStatus.Complete)
    {
        _notes.Add(new(code, message));
        if (status > _status) _status = status;
    }

    private AutoDrawResult Fail(AutoDrawNoteCode code, string message)
    {
        Note(code, message, AutoDrawStatus.NeedsManualDrawing);
        return Result(null);
    }

    private AutoDrawResult Result(Module? module) => new()
    {
        Status = module is null ? AutoDrawStatus.NeedsManualDrawing : _status,
        Module = module,
        Notes = _notes,
    };
}
