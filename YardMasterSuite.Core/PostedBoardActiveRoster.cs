namespace YardMasterSuite.Core
{
    /// <summary>
    /// Parsed posted speed board — string-free for the HUD tick (Active Roster).
    /// Dual km/h captured on rare FoT so Limit can keep facing authority without
    /// re-reading SignDebug.text every frame.
    /// </summary>
    public readonly struct ParsedPostedBoard
    {
        public ParsedPostedBoard(
            int instanceId,
            float x,
            float y,
            float z,
            float forwardX,
            float forwardZ,
            float rightX,
            float rightZ,
            float throughKmh,
            float divergeKmh,
            bool isDual,
            bool junctionNearby,
            int trackId = 0,
            float spanMeters = float.NaN)
        {
            SpanMeters = spanMeters;
            InstanceId = instanceId;
            X = x;
            Y = y;
            Z = z;
            ForwardX = forwardX;
            ForwardZ = forwardZ;
            RightX = rightX;
            RightZ = rightZ;
            ThroughKmh = throughKmh;
            DivergeKmh = divergeKmh;
            IsDual = isDual;
            JunctionNearby = junctionNearby;
            TrackId = trackId;
        }

        public int InstanceId { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float ForwardX { get; }
        public float ForwardZ { get; }
        public float RightX { get; }
        public float RightZ { get; }
        public float ThroughKmh { get; }
        public float DivergeKmh { get; }
        public bool IsDual { get; }
        public bool JunctionNearby { get; }

        /// <summary>
        /// Live <c>RailTrack</c> instance id when known (v1 / DVRouteManager hop
        /// membership). Zero = fall back to 12 m chord corridor.
        /// </summary>
        public int TrackId { get; }

        /// <summary>
        /// Bezier span on <see cref="TrackId"/>, resolved once per sign. NaN
        /// until the live layer has attached the board to a hop.
        /// </summary>
        public float SpanMeters { get; }

        public bool HasSpan => TrackId != 0 && !float.IsNaN(SpanMeters);

        public ParsedPostedBoard WithTrackId(int trackId) =>
            WithTrackSpan(trackId, SpanMeters);

        public ParsedPostedBoard WithTrackSpan(int trackId, float spanMeters) =>
            new ParsedPostedBoard(
                InstanceId,
                X,
                Y,
                Z,
                ForwardX,
                ForwardZ,
                RightX,
                RightZ,
                ThroughKmh,
                DivergeKmh,
                IsDual,
                JunctionNearby,
                trackId,
                spanMeters);
    }

    /// <summary>
    /// Spatial filter + governing-behind helpers. Heavy FoT stays on a rare
    /// refresh; HUD ticks walk this roster with float math.
    /// </summary>
    public static class PostedBoardActiveRoster
    {
        public const float ActiveRadiusMeters = 2500f;
        public const float RefreshSeconds = 45f;
        public const float EmptyRetrySeconds = 8f;
        public const int MaxEmptyRetries = 3;
        public const float MoveInvalidateMeters = 1000f;
        public const float LookbackMeters = 600f;
        public const float TakeAheadMeters = 250f;
        public const float LookaheadMinMeters = 1600f;
        public const float LookaheadSecondsOfSpeed = 12f;

        /// <summary>How far ahead Next may look (v1 1.16 floor, grows with speed).</summary>
        public static float LookaheadMeters(float speedKmh)
        {
            if (!(speedKmh > 0f))
            {
                return LookaheadMinMeters;
            }

            var fromSpeed = speedKmh * LookaheadSecondsOfSpeed;
            return fromSpeed > LookaheadMinMeters ? fromSpeed : LookaheadMinMeters;
        }

        public static bool WithinActiveRadius(
            float signX,
            float signY,
            float signZ,
            float originX,
            float originY,
            float originZ)
        {
            var dx = signX - originX;
            var dy = signY - originY;
            var dz = signZ - originZ;
            var r = ActiveRadiusMeters;
            return (dx * dx) + (dy * dy) + (dz * dz) <= r * r;
        }

        public static bool NeedsRefresh(
            float now,
            float lastRefreshAt,
            float originX,
            float originZ,
            float lastOriginX,
            float lastOriginZ,
            bool hasLastOrigin,
            bool rosterEmpty = false,
            int emptyRetriesDone = 0)
        {
            if (!hasLastOrigin || lastRefreshAt < 0f)
            {
                return true;
            }

            var dx = originX - lastOriginX;
            var dz = originZ - lastOriginZ;
            var m = MoveInvalidateMeters;
            if ((dx * dx) + (dz * dz) >= m * m)
            {
                return true;
            }

            if (rosterEmpty
                && emptyRetriesDone < MaxEmptyRetries
                && now - lastRefreshAt >= EmptyRetrySeconds)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Travel refresh after ~1 km driven or ~1 km XZ from last warm origin.
        /// XZ alone misses winding mainline (SW→FH curves; net displacement &lt; arc).
        /// </summary>
        public static bool NeedsTravelRefresh(
            float traveledSinceWarmMeters,
            float originX,
            float originZ,
            float lastOriginX,
            float lastOriginZ,
            bool hasLastOrigin)
        {
            if (traveledSinceWarmMeters >= MoveInvalidateMeters)
            {
                return true;
            }

            if (!hasLastOrigin)
            {
                return false;
            }

            var dx = originX - lastOriginX;
            var dz = originZ - lastOriginZ;
            var m = MoveInvalidateMeters;
            return (dx * dx) + (dz * dz) >= m * m;
        }

        public static float PickKmh(ParsedPostedBoard board, bool diverging) =>
            board.IsDual && diverging ? board.DivergeKmh : board.ThroughKmh;

        public static float? SelectGoverningBehindKmh(
            ParsedPostedBoard[] boards,
            float locoX,
            float locoY,
            float locoZ,
            float forwardX,
            float forwardY,
            float forwardZ,
            float lookbackMeters)
        {
            if (boards == null || boards.Length == 0 || lookbackMeters <= 0f)
            {
                return null;
            }

            float? bestLimit = null;
            var bestAlong = float.NegativeInfinity;
            var lookbackSq = lookbackMeters * lookbackMeters;

            for (var i = 0; i < boards.Length; i++)
            {
                var board = boards[i];
                var dx = board.X - locoX;
                var dy = board.Y - locoY;
                var dz = board.Z - locoZ;
                var distSq = (dx * dx) + (dy * dy) + (dz * dz);
                if (distSq > lookbackSq)
                {
                    continue;
                }

                var along = (dx * forwardX) + (dy * forwardY) + (dz * forwardZ);
                if (along >= 0f || along < -lookbackMeters)
                {
                    continue;
                }

                if (along <= bestAlong)
                {
                    continue;
                }

                bestAlong = along;
                bestLimit = board.ThroughKmh;
            }

            return bestLimit;
        }
    }
}
