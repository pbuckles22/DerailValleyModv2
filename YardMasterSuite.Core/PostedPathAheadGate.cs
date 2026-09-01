using System;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Event-only path/FILO gates for posted Limit (6.10). Cab tick uses cached
    /// path + along projection; no TryBuild or FoT on Update.
    /// </summary>
    public static class PostedPathAheadGate
    {
        /// <summary>Match <see cref="TrackPathAheadMaxHops"/> in Unity layer.</summary>
        public const int PathDictionaryCapacity = 128;

        public const int TrackPathAheadMaxHops = 128;

        public const float YardPollIntervalSeconds = 1f;

        /// <summary>Cached path covers the loco track — joint hop is a dict lookup.</summary>
        public static bool PathStillValid(bool hasPath, int locoTrackId, bool locoTrackOnPath) =>
            hasPath && locoTrackId != 0 && locoTrackOnPath;

        /// <summary>Rebuild when a junction on the cached corridor throws (branch fp change).</summary>
        public static bool ShouldRebuildForThrow(int lastFingerprint, int currentFingerprint, bool hasPath) =>
            hasPath && currentFingerprint != 0 && currentFingerprint != lastFingerprint;

        /// <summary>
        /// Loco left the warm-time corridor — rebuild path cache only, no FoT.
        /// SW-FH-82 2.8.1.6: Next jumped 73m → 127m then froze at 15m.
        /// </summary>
        public static bool ShouldRebuildForPathLoss(bool hadPath, bool pathStillValid) =>
            hadPath && !pathStillValid;

        /// <summary>Retry TryBuild once per loco track after a path miss.</summary>
        public static bool ShouldRetryPath(
            bool hasFiloWarm,
            bool hasPath,
            int locoTrackId,
            int lastRetryTrackId) =>
            hasFiloWarm && !hasPath && locoTrackId != 0 && locoTrackId != lastRetryTrackId;

        /// <summary>
        /// 9.1.2 Win 7: live thrown corridor uses Evaluate (path-abs overwrite).
        /// Path-miss stays chord Tick. Maps dest is not required — a board on
        /// this slice still governs.
        /// </summary>
        public static bool ShouldEvaluateMapsAuthority(int pathSegmentCount) =>
            pathSegmentCount > 0;

        /// <summary>HUD metres jumped the wrong way (path → chord flicker or clamp snap).</summary>
        public const float AlongJumpMeters = 40f;

        /// <summary>
        /// Board is on the travel rail if within this of a path segment.
        /// 12 m: curved exit boards (e.g. SW 1398156 ~11.7 m chord) stay on-path;
        /// 8 m rejected them. Parallel bleed stays a facing + path-span problem.
        /// </summary>
        public const float CorridorLateralMeters = 12f;

        public static bool IsOnCorridor(
            float boardX,
            float boardZ,
            in PathSegmentAlong segment,
            float lateralMaxMeters = CorridorLateralMeters)
        {
            var hx = segment.HintX;
            var hz = segment.HintZ;
            var hintLenSq = (hx * hx) + (hz * hz);
            var dx = boardX - segment.EntryX;
            var dz = boardZ - segment.EntryZ;
            if (hintLenSq < 1e-8f)
            {
                var dist = (float)Math.Sqrt((dx * dx) + (dz * dz));
                return dist <= lateralMaxMeters;
            }

            var hintLen = (float)Math.Sqrt(hintLenSq);
            var along = ((dx * hx) + (dz * hz)) / hintLen;
            if (along < -1f || along > segment.ChordLengthMeters + 1f)
            {
                return false;
            }

            var nx = hx / hintLen;
            var nz = hz / hintLen;
            var lat = Math.Abs((dx * nz) - (dz * nx));
            return lat <= lateralMaxMeters + BowMeters(in segment);
        }

        /// <summary>
        /// How far a hop bows off its own chord. A flat lateral gate rejects the
        /// middle of any real curve — a 157 m quarter-circle sits 29 m off the
        /// chord, so the loco fails to be "on" the track it is standing on.
        /// </summary>
        public static float BowMeters(in PathSegmentAlong segment)
        {
            var arc = segment.LengthMeters;
            var chord = segment.ChordLengthMeters;
            if (arc <= 0f || chord <= 0f || arc <= chord)
            {
                return 0f;
            }

            // Circular approximation: theta ~ sqrt(24(1 - chord/arc)), sagitta ~ arc*theta/8.
            var theta = (float)Math.Sqrt(24.0 * (1.0 - (chord / arc)));
            return arc * theta / 8f;
        }

        public static bool IsOnAnyCorridor(
            float boardX,
            float boardZ,
            PathSegmentAlong[] segments,
            int count,
            float lateralMaxMeters = CorridorLateralMeters)
        {
            if (segments == null || count <= 0)
            {
                return false;
            }

            var n = count > segments.Length ? segments.Length : count;
            for (var i = 0; i < n; i++)
            {
                if (IsOnCorridor(boardX, boardZ, in segments[i], lateralMaxMeters))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// On-path when the board's live hop id is in the walk (v1
        /// <c>path.TryGetValue(boardTrack)</c> / DVRouteManager
        /// <c>path.IndexOf(currentTrack)</c>). Unknown track id keeps the 12 m
        /// chord gate for HTP dumps.
        /// </summary>
        public static bool IsBoardOnPath(
            in ParsedPostedBoard board,
            PathSegmentAlong[] segments,
            int count,
            float lateralMaxMeters = CorridorLateralMeters)
        {
            if (board.TrackId != 0)
            {
                return HopHasTrack(segments, count, board.TrackId);
            }

            return IsOnAnyCorridor(board.X, board.Z, segments, count, lateralMaxMeters);
        }

        public static bool HopHasTrack(PathSegmentAlong[]? segments, int count, int trackId)
        {
            if (trackId == 0 || segments == null || count <= 0)
            {
                return false;
            }

            var n = count > segments.Length ? segments.Length : count;
            for (var i = 0; i < n; i++)
            {
                if (segments[i].TrackId == trackId)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsAlongJump(float previousRemaining, float currentRemaining) =>
            previousRemaining > 0f
            && currentRemaining - previousRemaining >= AlongJumpMeters;

        /// <summary>
        /// Path remaining when the corridor is valid; chord when it is not.
        /// World already past the board wins if path remaining is still stuck positive
        /// (segment-end clamp / stale abs).
        /// </summary>
        public static float ResolveAlong(float pathRemaining, float chordAlong, bool havePathAbs)
        {
            if (!havePathAbs)
            {
                return chordAlong;
            }

            if (pathRemaining > 0f && chordAlong <= 0f)
            {
                return chordAlong;
            }

            return pathRemaining;
        }

        public static bool YardPollDue(float now, float lastPollAt) =>
            lastPollAt < 0f || now - lastPollAt >= YardPollIntervalSeconds;

        public static int JunctionBranchFingerprint(JunctionBranchState[] junctions, int count)
        {
            var fp = 0;
            if (junctions == null || count <= 0)
            {
                return fp;
            }

            if (count > junctions.Length)
            {
                count = junctions.Length;
            }

            for (var i = 0; i < count; i++)
            {
                var j = junctions[i];
                if (j.JunctionId == 0)
                {
                    continue;
                }

                fp ^= (j.JunctionId * 397) ^ j.SelectedBranch;
            }

            return fp;
        }

        /// <summary>
        /// Along-on-track metres, unclamped. Negative = before segment entry.
        /// </summary>
        public static float AlongOnTrack(float x, float z, in PathSegmentAlong segment)
        {
            var dx = x - segment.EntryX;
            var dz = z - segment.EntryZ;
            var hx = segment.HintX;
            var hz = segment.HintZ;
            var hintLenSq = (hx * hx) + (hz * hz);
            if (hintLenSq < 1e-8f)
            {
                return (float)Math.Sqrt((dx * dx) + (dz * dz));
            }

            var hintLen = (float)Math.Sqrt(hintLenSq);
            return ((dx * hx) + (dz * hz)) / hintLen;
        }

        /// <summary>
        /// Pick the live path segment. Prefer a hop the loco is on (12 m
        /// corridor) so Bezier LengthMeters on a stale yard hop cannot trap
        /// remaining (Win 5 cab: Next 40 stuck 296 m then 600 m, never take).
        /// </summary>
        public static int SelectSegmentIndex(
            float x,
            float z,
            PathSegmentAlong[] segments,
            int count,
            int locoTrackId = 0)
        {
            if (segments == null || count <= 0)
            {
                return -1;
            }

            var n = count > segments.Length ? segments.Length : count;
            if (locoTrackId != 0)
            {
                for (var i = 0; i < n; i++)
                {
                    if (segments[i].TrackId == locoTrackId)
                    {
                        return i;
                    }
                }
            }
            var firstOnCorridor = -1;
            var firstInSpan = -1;
            for (var i = 0; i < n; i++)
            {
                var along = AlongOnTrack(x, z, in segments[i]);
                var inSpan = along < 0f || along <= segments[i].ChordLengthMeters;
                var onCorridor = IsOnCorridor(x, z, in segments[i]);
                if (onCorridor && inSpan)
                {
                    return i;
                }

                if (onCorridor && firstOnCorridor < 0)
                {
                    firstOnCorridor = i;
                }

                if (inSpan && firstInSpan < 0)
                {
                    firstInSpan = i;
                }
            }

            if (firstOnCorridor >= 0)
            {
                return firstOnCorridor;
            }

            if (firstInSpan >= 0)
            {
                return firstInSpan;
            }

            return n - 1;
        }

        /// <summary>Absolute meters along the warm-time path from segment geometry.</summary>
        public static float LocoAbsMeters(
            float locoX,
            float locoY,
            float locoZ,
            in PathSegmentAlong segment)
        {
            var alongOnTrack = AlongOnTrack(locoX, locoZ, in segment);
            if (alongOnTrack < 0f)
            {
                alongOnTrack = 0f;
            }

            return segment.EntryDistanceMeters + alongOnTrack;
        }

        /// <summary>
        /// Path abs after segment swap. Before the first entry, along stays
        /// negative so reverse remaining does not freeze at the clamp.
        /// </summary>
        public static float LocoAbsOnPath(
            float locoX,
            float locoY,
            float locoZ,
            PathSegmentAlong[] segments,
            int count,
            int locoTrackId = 0)
        {
            var i = SelectSegmentIndex(locoX, locoZ, segments, count, locoTrackId);
            if (i < 0)
            {
                return 0f;
            }

            var along = AlongOnTrack(locoX, locoZ, in segments[i]);
            if (along < 0f)
            {
                // Before the first entry, keep the negative so reverse remaining
                // does not freeze at the clamp.
                return segments[i].EntryDistanceMeters + (i > 0 ? 0f : along);
            }

            return segments[i].EntryDistanceMeters + ChordToArc(along, in segments[i]);
        }

        /// <summary>Board abs on the corridor segment it sits on; else path projection.</summary>
        public static float BoardAbsMeters(
            float boardX,
            float boardZ,
            PathSegmentAlong[] segments,
            int count)
        {
            if (segments == null || count <= 0)
            {
                return 0f;
            }

            var n = count > segments.Length ? segments.Length : count;
            for (var i = 0; i < n; i++)
            {
                if (!IsOnCorridor(boardX, boardZ, in segments[i]))
                {
                    continue;
                }

                return segments[i].EntryDistanceMeters
                    + ChordToArc(AlongOnTrack(boardX, boardZ, in segments[i]), in segments[i]);
            }

            return LocoAbsOnPath(boardX, 0f, boardZ, segments, count);
        }

        /// <summary>
        /// Route distance from a live Bezier span, the way v1 measured it:
        /// <c>RailTrack.GetClosestPoint(...).span</c> converted against the hop's
        /// own arc length. Chord projection cannot do this — on a bend the chord
        /// saturates short of <see cref="PathSegmentAlong.LengthMeters"/>, so
        /// remaining freezes at the arc-minus-chord surplus and no board is taken.
        /// </summary>
        public static bool TryAbsFromSpan(
            PathSegmentAlong[]? segments,
            int count,
            int trackId,
            float spanMeters,
            out float absMeters)
        {
            absMeters = 0f;
            if (segments == null || count <= 0 || trackId == 0 || float.IsNaN(spanMeters))
            {
                return false;
            }

            var n = count > segments.Length ? segments.Length : count;
            for (var i = 0; i < n; i++)
            {
                if (segments[i].TrackId != trackId)
                {
                    continue;
                }

                absMeters = segments[i].EntryDistanceMeters
                    + TrackPathSpan.WithinTrackMeters(
                        spanMeters,
                        segments[i].LengthMeters,
                        segments[i].TravelIncreasingSpan);
                return true;
            }

            return false;
        }

        /// <summary>Board abs on its hop when TrackId is set; else 12 m corridor.</summary>
        public static float BoardAbsMeters(
            in ParsedPostedBoard board,
            PathSegmentAlong[]? segments,
            int count)
        {
            if (segments == null || count <= 0)
            {
                return 0f;
            }

            if (board.TrackId != 0)
            {
                var n = count > segments.Length ? segments.Length : count;
                for (var i = 0; i < n; i++)
                {
                    if (segments[i].TrackId != board.TrackId)
                    {
                        continue;
                    }

                    return AbsOnHop(board.X, board.Z, in segments[i]);
                }
            }

            return BoardAbsMeters(board.X, board.Z, segments, count);
        }

        private static float AbsOnHop(float x, float z, in PathSegmentAlong segment) =>
            segment.EntryDistanceMeters + ChordToArc(AlongOnTrack(x, z, in segment), in segment);

        /// <summary>
        /// Chord projection scaled onto the hop's arc. Entry distances accumulate
        /// arc length, so a raw chord offset added to them under-reads on a bend
        /// and leaves remaining stuck positive.
        /// </summary>
        public static float ChordToArc(float chordAlongMeters, in PathSegmentAlong segment)
        {
            var chord = segment.ChordLengthMeters;
            var arc = segment.LengthMeters;
            if (chordAlongMeters <= 0f)
            {
                return 0f;
            }

            if (chord <= 1e-4f || arc <= 0f)
            {
                return chordAlongMeters;
            }

            var clamped = chordAlongMeters > chord ? chord : chordAlongMeters;
            return clamped * (arc / chord);
        }

        /// <summary>
        /// +1 when travel aligns with the path-segment hint, −1 when reverse.
        /// Remaining = (boardAbs − locoAbs) × this (9.1.2 Win 5).
        /// </summary>
        public static float PathTravelPolarity(
            float travelX,
            float travelZ,
            float hintX,
            float hintZ) =>
            ((travelX * hintX) + (travelZ * hintZ)) < 0f ? -1f : 1f;

        /// <summary>Relative ahead/behind from warm-time board abs and rolling loco abs.</summary>
        public static float BoardRemaining(float boardAbsMeters, float locoAbsMeters) =>
            boardAbsMeters - locoAbsMeters;

        /// <summary>Path remaining with travel polarity (reverse flips ahead/behind).</summary>
        public static float BoardRemaining(
            float boardAbsMeters,
            float locoAbsMeters,
            float travelX,
            float travelZ,
            float hintX,
            float hintZ) =>
            (boardAbsMeters - locoAbsMeters)
            * PathTravelPolarity(travelX, travelZ, hintX, hintZ);

        /// <summary>
        /// Behind take is same-rail and within <see cref="PostedBoardActiveRoster.TakeAheadMeters"/>.
        /// Far throat ghosts (e.g. harvest 1396790) must not set sticky.
        /// </summary>
        public static bool ShouldTakeBehind(float remainingMeters, bool sameRail) =>
            sameRail
            && remainingMeters <= 0f
            && remainingMeters >= -PostedBoardActiveRoster.TakeAheadMeters;

        /// <summary>
        /// Symmetric junction dual on the through path must not govern Next or take
        /// (9.1.2 Win 4 — e.g. SW harvest board 1398162 50/50). Asymmetric duals
        /// (60/40 through) still govern via <see cref="ParsedPostedBoard.ThroughKmh"/>.
        /// </summary>
        public static bool ShouldSkipSymmetricDualThrough(in ParsedPostedBoard board, bool diverging)
        {
            if (diverging || !board.IsDual || !board.JunctionNearby)
            {
                return false;
            }

            var through = (int)Math.Round(board.ThroughKmh, MidpointRounding.AwayFromZero);
            var diverge = (int)Math.Round(board.DivergeKmh, MidpointRounding.AwayFromZero);
            return through == diverge;
        }
    }

    public readonly struct JunctionBranchState
    {
        public JunctionBranchState(int junctionId, int selectedBranch)
        {
            JunctionId = junctionId;
            SelectedBranch = selectedBranch;
        }

        public int JunctionId { get; }
        public int SelectedBranch { get; }
    }

    /// <summary>Unity-free segment projection inputs for <see cref="PostedPathAheadGate.LocoAbsMeters"/>.</summary>
    public readonly struct PathSegmentAlong
    {
        public PathSegmentAlong(
            float entryDistanceMeters,
            float entryX,
            float entryY,
            float entryZ,
            float hintX,
            float hintZ,
            float lengthMeters,
            int trackId = 0,
            bool travelIncreasingSpan = true,
            float chordLengthMeters = 0f)
        {
            EntryDistanceMeters = entryDistanceMeters;
            EntryX = entryX;
            EntryY = entryY;
            EntryZ = entryZ;
            HintX = hintX;
            HintZ = hintZ;
            LengthMeters = lengthMeters;
            TrackId = trackId;
            TravelIncreasingSpan = travelIncreasingSpan;
            ChordLengthMeters = chordLengthMeters > 0f ? chordLengthMeters : lengthMeters;
        }

        public float EntryDistanceMeters { get; }
        public float EntryX { get; }
        public float EntryY { get; }
        public float EntryZ { get; }
        public float HintX { get; }
        public float HintZ { get; }
        public float LengthMeters { get; }

        /// <summary>Live or dump hop id. Zero = chord-only membership.</summary>
        public int TrackId { get; }

        /// <summary>
        /// True when travelling this hop increases <c>RailTrack</c> span. Needed
        /// to turn a Bezier span into distance from the hop entry.
        /// </summary>
        public bool TravelIncreasingSpan { get; }

        /// <summary>
        /// Straight In→Out distance. Equals <see cref="LengthMeters"/> on tangent
        /// track; shorter on a bend, and the gap is how far the rail bows away
        /// from the chord that <see cref="PostedPathAheadGate.AlongOnTrack"/> uses.
        /// </summary>
        public float ChordLengthMeters { get; }
    }
}
