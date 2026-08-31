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
            if (along < -1f || along > segment.LengthMeters + 1f)
            {
                return false;
            }

            var nx = hx / hintLen;
            var nz = hz / hintLen;
            var lat = Math.Abs((dx * nz) - (dz * nx));
            return lat <= lateralMaxMeters;
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

        /// <summary>Absolute meters along the warm-time path from segment geometry.</summary>
        public static float LocoAbsMeters(
            float locoX,
            float locoY,
            float locoZ,
            in PathSegmentAlong segment)
        {
            var dx = locoX - segment.EntryX;
            var dz = locoZ - segment.EntryZ;
            var hx = segment.HintX;
            var hz = segment.HintZ;
            var hintLenSq = (hx * hx) + (hz * hz);
            float alongOnTrack;
            if (hintLenSq < 1e-8f)
            {
                alongOnTrack = (float)Math.Sqrt((dx * dx) + (dz * dz));
            }
            else
            {
                var hintLen = (float)Math.Sqrt(hintLenSq);
                alongOnTrack = ((dx * hx) + (dz * hz)) / hintLen;
            }

            if (alongOnTrack < 0f)
            {
                alongOnTrack = 0f;
            }

            return segment.EntryDistanceMeters + alongOnTrack;
        }

        /// <summary>Relative ahead/behind from warm-time board abs and rolling loco abs.</summary>
        public static float BoardRemaining(float boardAbsMeters, float locoAbsMeters) =>
            boardAbsMeters - locoAbsMeters;
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
            float lengthMeters)
        {
            EntryDistanceMeters = entryDistanceMeters;
            EntryX = entryX;
            EntryY = entryY;
            EntryZ = entryZ;
            HintX = hintX;
            HintZ = hintZ;
            LengthMeters = lengthMeters;
        }

        public float EntryDistanceMeters { get; }
        public float EntryX { get; }
        public float EntryY { get; }
        public float EntryZ { get; }
        public float HintX { get; }
        public float HintZ { get; }
        public float LengthMeters { get; }
    }
}
