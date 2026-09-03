using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Posted Limit funnel: warm fills a fixed buffer from FILO exits; cab ticks
    /// only recompute chord along, pop takes, and push refill candidates.
    /// Zero alloc on <see cref="Tick"/> / <see cref="PublishIfChanged"/>.
    /// </summary>
    public sealed class PostedLimitFunnel
    {
        /// <summary>Unlocked sit/Y may hold both exits (≤ MaxDepth each).</summary>
        public const int MaxSlots = PostedLimitFilo.MaxDepth * 2;

        private readonly ParsedPostedBoard[] _plus =
            new ParsedPostedBoard[PostedLimitFilo.MaxDepth];

        private readonly ParsedPostedBoard[] _minus =
            new ParsedPostedBoard[PostedLimitFilo.MaxDepth];

        private readonly ParsedPostedBoard[] _slots = new ParsedPostedBoard[MaxSlots];

        private readonly float[] _along = new float[MaxSlots];

        private readonly bool[] _onPath = new bool[MaxSlots];

        private readonly BoardTakeDetector _takes = new BoardTakeDetector();

        private int _plusCount;

        private int _minusCount;

        private int _count;

        private float? _stickyKmh;

        private bool _directionLocked;

        private float _warmForwardX;

        private float _warmForwardZ;

        private float _travelX;

        private float _travelZ;

        private float _lastTakeAlongMeters;

        public bool RequireOnPath { get; set; }

        public int Count => _count;

        public int PlusCount => _plusCount;

        public int MinusCount => _minusCount;

        public float? StickyKmh => _stickyKmh;

        public bool DirectionLocked => _directionLocked;

        /// <summary>Along metres of the board that last set sticky via <see cref="Tick"/>.</summary>
        public float LastTakeAlongMeters => _lastTakeAlongMeters;

        public int ActiveCapacity =>
            _directionLocked ? PostedLimitFilo.MaxDepth : MaxSlots;

        /// <summary>
        /// Event warm: partition exits and fill the funnel nearest-first.
        /// Pass <paramref name="preserveSticky"/> on town/empty refill so Limit
        /// does not flash 120 (Player.log SW→— / empty wipe).
        /// </summary>
        public void Warm(
            ParsedPostedBoard[] boards,
            float originX,
            float originY,
            float originZ,
            float forwardX,
            float forwardY,
            float forwardZ,
            float? preserveSticky = null)
        {
            PostedLimitFilo.PartitionExits(
                boards,
                originX,
                originY,
                originZ,
                forwardX,
                forwardY,
                forwardZ,
                out var plus,
                out var minus);
            _plusCount = CopyBoards(plus, _plus);
            _minusCount = CopyBoards(minus, _minus);
            _warmForwardX = forwardX;
            _warmForwardZ = forwardZ;
            _travelX = forwardX;
            _travelZ = forwardZ;
            _directionLocked = false;
            _stickyKmh = preserveSticky;
            RebuildSlots(originX, originY, originZ, forwardX, forwardY, forwardZ);
            if (preserveSticky is not null)
            {
                DropBehindWithoutTake();
            }
            else
            {
                // Reverse / spawn warm: forget which boards were ahead. A refill
                // warm keeps that memory so already-passed boards stay passed.
                _takes.Reset();
            }
        }

        /// <summary>
        /// Apply travel; lock polarity above crawl and drop the opposite exit.
        /// </summary>
        public void SetTravel(
            float travelX,
            float travelY,
            float travelZ,
            float speedKmh,
            float locoX,
            float locoY,
            float locoZ)
        {
            if (PostedLimitFilo.ShouldFreezeAtStandstill(speedKmh))
            {
                return;
            }

            _travelX = travelX;
            _travelZ = travelZ;
            if (_directionLocked || !PostedLimitFilo.ShouldLockDirection(speedKmh))
            {
                return;
            }

            _directionLocked = true;
            var warmLen = Math.Sqrt(
                (_warmForwardX * _warmForwardX) + (_warmForwardZ * _warmForwardZ));
            var travelLen = Math.Sqrt((travelX * travelX) + (travelZ * travelZ));
            var keepPlus = true;
            if (warmLen >= 1e-4 && travelLen >= 1e-4)
            {
                var dot = ((_warmForwardX * travelX) + (_warmForwardZ * travelZ))
                    / (warmLen * travelLen);
                keepPlus = dot >= 0.0;
            }

            if (keepPlus)
            {
                _minusCount = 0;
            }
            else
            {
                _plusCount = CopyBoards(_minus, _plus, _minusCount);
                _minusCount = 0;
            }

            _warmForwardX = travelX;
            _warmForwardZ = travelZ;
            RebuildSlots(locoX, locoY, locoZ, travelX, travelY, travelZ);
        }

        /// <summary>
        /// Cab tick: refresh chord along; pop boards at/behind loco when rolling.
        /// </summary>
        public void Tick(
            float locoX,
            float locoY,
            float locoZ,
            float travelX,
            float travelY,
            float travelZ,
            float speedKmh)
        {
            if (PostedLimitFilo.ShouldFreezeAtStandstill(speedKmh))
            {
                return;
            }

            _travelX = travelX;
            _travelZ = travelZ;
            for (var i = 0; i < _count; i++)
            {
                _along[i] = PostedLimitFilo.AlongMeters(
                    locoX,
                    locoY,
                    locoZ,
                    travelX,
                    travelY,
                    travelZ,
                    _slots[i]);
            }

            SortSlotsByAlong();
            if (!PostedLimitFilo.ShouldPopOnTick(speedKmh, _directionLocked))
            {
                return;
            }

            while (_count > 0 && _along[0] <= 0f)
            {
                if (RequireOnPath && !_onPath[0])
                {
                    RemoveAt(0);
                    continue;
                }

                if (PostedPathAheadGate.ShouldSkipSymmetricDualThrough(_slots[0], diverging: false))
                {
                    RemoveAt(0);
                    continue;
                }

                if (!PostedPathAheadGate.ShouldTakeBehind(_along[0], _onPath[0]))
                {
                    RemoveAt(0);
                    continue;
                }

                _lastTakeAlongMeters = _along[0];
                _stickyKmh = _slots[0].ThroughKmh;
                RemoveAt(0);
            }
        }

        /// <summary>
        /// Push one farther look-ahead board when the funnel has room.
        /// </summary>
        public bool TryPush(
            in ParsedPostedBoard board,
            float locoX,
            float locoY,
            float locoZ,
            float travelX,
            float travelY,
            float travelZ)
        {
            if (_count >= ActiveCapacity)
            {
                return false;
            }

            var along = PostedLimitFilo.AlongMeters(
                locoX,
                locoY,
                locoZ,
                travelX,
                travelY,
                travelZ,
                board);
            if (along <= 0f)
            {
                return false;
            }

            if (_count > 0 && along <= _along[_count - 1])
            {
                return false;
            }

            for (var i = 0; i < _count; i++)
            {
                if (_slots[i].InstanceId == board.InstanceId)
                {
                    return false;
                }
            }

            _slots[_count] = board;
            _along[_count] = along;
            _onPath[_count] = true;
            _count++;
            SortSlotsByAlong();
            return true;
        }

        /// <summary>
        /// Maps publish authority: rebuild slots from on-path roster using
        /// path-abs remaining. FILO Warm/Tick stay chord. Zero alloc.
        /// </summary>
        public void Evaluate(
            IReadOnlyList<ParsedPostedBoard> roster,
            PathSegmentAlong[] segments,
            int segmentCount,
            float locoX,
            float locoY,
            float locoZ,
            float travelX,
            float travelY,
            float travelZ,
            float speedKmh,
            int locoTrackId = 0,
            float locoSpanMeters = float.NaN)
        {
            RequireOnPath = true;
            _travelX = travelX;
            _travelZ = travelZ;
            _count = 0;
            if (roster == null || roster.Count == 0 || segments == null || segmentCount <= 0)
            {
                return;
            }

            var onSpan = PostedPathAheadGate.TryAbsFromSpan(
                segments,
                segmentCount,
                locoTrackId,
                locoSpanMeters,
                out var locoAbs);
            if (!onSpan)
            {
                locoAbs = PostedPathAheadGate.LocoAbsOnPath(
                    locoX,
                    locoY,
                    locoZ,
                    segments,
                    segmentCount,
                    locoTrackId);
            }

            var segIdx = PostedPathAheadGate.SelectSegmentIndex(
                locoX,
                locoZ,
                segments,
                segmentCount,
                locoTrackId);
            var hintX = 0f;
            var hintZ = 1f;
            if (segIdx >= 0 && segIdx < segments.Length)
            {
                hintX = segments[segIdx].HintX;
                hintZ = segments[segIdx].HintZ;
            }

            var popping = PostedLimitFilo.ShouldPopOnTick(speedKmh, _directionLocked);
            float? takenKmh = null;
            var takenAlong = float.NegativeInfinity;
            var n = roster.Count;
            for (var r = 0; r < n; r++)
            {
                var board = roster[r];
                if (!PostedPathAheadGate.IsBoardOnPath(
                        in board,
                        segments,
                        segmentCount))
                {
                    continue;
                }

                var remaining = Remaining(
                    in board,
                    segments,
                    segmentCount,
                    onSpan,
                    locoAbs,
                    travelX,
                    travelZ,
                    hintX,
                    hintZ);
                // Next slots are ahead only. Behind boards still take via
                // Observe; they must not fill MaxDepth and hide a 60@100 m.
                if (remaining > PostedLimitFilo.SnapshotAlongMinMeters)
                {
                    InsertByRemaining(in board, remaining, onPath: true);
                }

                if (!popping
                    || PostedPathAheadGate.ShouldSkipSymmetricDualThrough(in board, diverging: false)
                    || PostedBoardHarvestCodec.FacesAway(in board, travelX, travelZ))
                {
                    continue;
                }

                // v1 rule: a board governs only on the ahead → behind crossing,
                // so one bad frame cannot silently consume it.
                if (_takes.Observe(board.InstanceId, board.ThroughKmh, remaining) is float take
                    && remaining > takenAlong)
                {
                    takenKmh = take;
                    takenAlong = remaining;
                }
            }

            if (takenKmh is float governing)
            {
                _lastTakeAlongMeters = takenAlong;
                _stickyKmh = governing;
            }

            if (!popping)
            {
                return;
            }

            while (_count > 0 && _along[0] <= 0f)
            {
                RemoveAt(0);
            }
        }

        /// <summary>
        /// After a travel roster refresh, mark on-path boards already behind as
        /// seen so <see cref="Evaluate"/> does not cold-start take them.
        /// </summary>
        public void SeedRefreshBehindFromRoster(
            IReadOnlyList<ParsedPostedBoard> roster,
            PathSegmentAlong[] segments,
            int segmentCount,
            float locoX,
            float locoY,
            float locoZ,
            float travelX,
            float travelY,
            float travelZ,
            int locoTrackId = 0,
            float locoSpanMeters = float.NaN)
        {
            if (roster == null || roster.Count == 0 || segments == null || segmentCount <= 0)
            {
                return;
            }

            var onSpan = PostedPathAheadGate.TryAbsFromSpan(
                segments,
                segmentCount,
                locoTrackId,
                locoSpanMeters,
                out var locoAbs);
            if (!onSpan)
            {
                locoAbs = PostedPathAheadGate.LocoAbsOnPath(
                    locoX,
                    locoY,
                    locoZ,
                    segments,
                    segmentCount,
                    locoTrackId);
            }

            var segIdx = PostedPathAheadGate.SelectSegmentIndex(
                locoX,
                locoZ,
                segments,
                segmentCount,
                locoTrackId);
            var hintX = 0f;
            var hintZ = 1f;
            if (segIdx >= 0 && segIdx < segments.Length)
            {
                hintX = segments[segIdx].HintX;
                hintZ = segments[segIdx].HintZ;
            }

            var n = roster.Count;
            for (var r = 0; r < n; r++)
            {
                var board = roster[r];
                if (_takes.WasSeen(board.InstanceId)
                    || !PostedPathAheadGate.IsBoardOnPath(in board, segments, segmentCount))
                {
                    continue;
                }

                var remaining = Remaining(
                    in board,
                    segments,
                    segmentCount,
                    onSpan,
                    locoAbs,
                    travelX,
                    travelZ,
                    hintX,
                    hintZ);
                if (remaining <= 0f)
                {
                    _takes.SeedAlreadyBehind(board.InstanceId);
                }
            }
        }

        /// <summary>
        /// Route remaining. Bezier span when the live layer resolved both loco
        /// and board onto hops; chord projection only as the dump fallback.
        /// </summary>
        private static float Remaining(
            in ParsedPostedBoard board,
            PathSegmentAlong[] segments,
            int segmentCount,
            bool locoOnSpan,
            float locoAbs,
            float travelX,
            float travelZ,
            float hintX,
            float hintZ)
        {
            if (locoOnSpan
                && board.HasSpan
                && PostedPathAheadGate.TryAbsFromSpan(
                    segments,
                    segmentCount,
                    board.TrackId,
                    board.SpanMeters,
                    out var spanAbs))
            {
                // Both ends share one arc-length axis built along travel, so the
                // chord-hint polarity flip is neither needed nor safe on a bend.
                return spanAbs - locoAbs;
            }

            return PostedPathAheadGate.BoardRemaining(
                PostedPathAheadGate.BoardAbsMeters(in board, segments, segmentCount),
                locoAbs,
                travelX,
                travelZ,
                hintX,
                hintZ);
        }

        /// <summary>
        /// Add any ahead board not already in the funnel (then re-sort).
        /// Used to refill after a pop without another FoT.
        /// </summary>
        public bool TryAddAhead(
            in ParsedPostedBoard board,
            float locoX,
            float locoY,
            float locoZ,
            float travelX,
            float travelY,
            float travelZ)
        {
            if (_count >= ActiveCapacity)
            {
                return false;
            }

            var along = PostedLimitFilo.AlongMeters(
                locoX,
                locoY,
                locoZ,
                travelX,
                travelY,
                travelZ,
                board);
            if (along <= 0f)
            {
                return false;
            }

            for (var i = 0; i < _count; i++)
            {
                if (_slots[i].InstanceId == board.InstanceId)
                {
                    return false;
                }
            }

            _slots[_count] = board;
            _along[_count] = along;
            _onPath[_count] = true;
            _count++;
            SortSlotsByAlong();
            return true;
        }

        /// <summary>
        /// Fill free slots from a warm-time roster (no FoT). Returns how many added.
        /// </summary>
        public int RefillFrom(
            IReadOnlyList<ParsedPostedBoard> roster,
            float locoX,
            float locoY,
            float locoZ,
            float travelX,
            float travelY,
            float travelZ)
        {
            if (roster == null || roster.Count == 0)
            {
                return 0;
            }

            var added = 0;
            while (_count < ActiveCapacity)
            {
                var bestIdx = -1;
                var bestAlong = float.PositiveInfinity;
                for (var i = 0; i < roster.Count; i++)
                {
                    var board = roster[i];
                    if (ContainsId(board.InstanceId))
                    {
                        continue;
                    }

                    var along = PostedLimitFilo.AlongMeters(
                        locoX,
                        locoY,
                        locoZ,
                        travelX,
                        travelY,
                        travelZ,
                        board);
                    if (along <= 0f || along >= bestAlong)
                    {
                        continue;
                    }

                    bestAlong = along;
                    bestIdx = i;
                }

                if (bestIdx < 0)
                {
                    break;
                }

                if (!TryAddAhead(
                        roster[bestIdx],
                        locoX,
                        locoY,
                        locoZ,
                        travelX,
                        travelY,
                        travelZ))
                {
                    break;
                }

                added++;
            }

            return added;
        }

        public bool ContainsId(int instanceId)
        {
            for (var i = 0; i < _count; i++)
            {
                if (_slots[i].InstanceId == instanceId)
                {
                    return true;
                }
            }

            return false;
        }

        public PostedLimitSnapshot ToSnapshot()
        {
            // Unlocked sit holds both exits — chord flip-flops Next; withhold
            // until lock. Evaluate already filtered to the thrown path, so
            // RequireOnPath may publish Next without a crawl lock (9.1.4).
            if (!_directionLocked && !RequireOnPath)
            {
                return new PostedLimitSnapshot(_stickyKmh, _count, null, null);
            }

            float? nextKmh = null;
            float? nextAlong = null;
            var from = _stickyKmh ?? SpeedLimitState.UnrestrictedKmh;
            var fromWhole = (int)Math.Round(from, MidpointRounding.AwayFromZero);
            for (var i = 0; i < _count; i++)
            {
                if (!PostedLimitFilo.IsVisibleAlong(_along[i]))
                {
                    continue;
                }

                var whole = (int)Math.Round(_slots[i].ThroughKmh, MidpointRounding.AwayFromZero);
                if (whole == fromWhole)
                {
                    continue;
                }

                if (RequireOnPath && !_onPath[i])
                {
                    continue;
                }

                if (PostedPathAheadGate.ShouldSkipSymmetricDualThrough(_slots[i], diverging: false))
                {
                    continue;
                }

                if (RequireOnPath
                    ? PostedBoardHarvestCodec.FacesAway(in _slots[i], _travelX, _travelZ)
                    : !PostedBoardHarvestCodec.FacesTravel(in _slots[i], _travelX, _travelZ))
                {
                    continue;
                }

                nextKmh = _slots[i].ThroughKmh;
                nextAlong = _along[i];
                break;
            }

            return new PostedLimitSnapshot(_stickyKmh, _count, nextKmh, nextAlong);
        }

        /// <param name="raiseEvent">
        /// False = hitch isolate: update publish cache only, skip EventBus→HUD.
        /// </param>
        public bool PublishIfChanged(ref PostedLimitCache cache, bool raiseEvent = true)
        {
            var snap = ToSnapshot();
            if (!PostedLimitTelemetry.Observe(in snap, ref cache, out var published))
            {
                return false;
            }

            if (raiseEvent)
            {
                YmsEventBus.RaisePostedLimitChanged(published);
            }

            return true;
        }

        public void Reset()
        {
            _plusCount = 0;
            _minusCount = 0;
            _count = 0;
            _stickyKmh = null;
            _directionLocked = false;
            _warmForwardX = 0f;
            _warmForwardZ = 0f;
            _travelX = 0f;
            _travelZ = 0f;
            _lastTakeAlongMeters = 0f;
            RequireOnPath = false;
            _takes.Reset();
        }

        public void SetOnPath(int index, bool onPath)
        {
            if (index >= 0 && index < _count)
            {
                _onPath[index] = onPath;
            }
        }

        public void SetAllOnPath(bool onPath)
        {
            for (var i = 0; i < _count; i++)
            {
                _onPath[i] = onPath;
            }
        }

        /// <summary>Test/diagnostics: along metres for slot i (0 = head).</summary>
        public float AlongAt(int index) =>
            index >= 0 && index < _count ? _along[index] : 0f;

        public ParsedPostedBoard BoardAt(int index) =>
            index >= 0 && index < _count ? _slots[index] : default;

        private void RebuildSlots(
            float originX,
            float originY,
            float originZ,
            float forwardX,
            float forwardY,
            float forwardZ)
        {
            _count = 0;
            AddExit(_plus, _plusCount, originX, originY, originZ, forwardX, forwardY, forwardZ);
            if (!_directionLocked)
            {
                AddExit(_minus, _minusCount, originX, originY, originZ, forwardX, forwardY, forwardZ);
            }

            SortSlotsByAlong();
        }

        /// <summary>Drop already-behind boards without rewriting sticky (refill warm).</summary>
        private void DropBehindWithoutTake()
        {
            while (_count > 0 && _along[0] <= 0f)
            {
                RemoveAt(0);
            }
        }

        private void AddExit(
            ParsedPostedBoard[] src,
            int srcCount,
            float originX,
            float originY,
            float originZ,
            float forwardX,
            float forwardY,
            float forwardZ)
        {
            for (var i = 0; i < srcCount; i++)
            {
                if (_count >= ActiveCapacity)
                {
                    break;
                }

                _slots[_count] = src[i];
                _along[_count] = PostedLimitFilo.AlongMeters(
                    originX,
                    originY,
                    originZ,
                    forwardX,
                    forwardY,
                    forwardZ,
                    src[i]);
                _onPath[_count] = true;
                _count++;
            }
        }

        private void InsertByRemaining(in ParsedPostedBoard board, float remaining, bool onPath)
        {
            var cap = ActiveCapacity;
            if (cap <= 0)
            {
                return;
            }

            if (_count >= cap && remaining >= _along[_count - 1])
            {
                return;
            }

            var i = 0;
            while (i < _count && _along[i] <= remaining)
            {
                i++;
            }

            var destCount = _count < cap ? _count + 1 : _count;
            for (var j = destCount - 1; j > i; j--)
            {
                _slots[j] = _slots[j - 1];
                _along[j] = _along[j - 1];
                _onPath[j] = _onPath[j - 1];
            }

            _slots[i] = board;
            _along[i] = remaining;
            _onPath[i] = onPath;
            _count = destCount;
        }

        private void RemoveAt(int index)
        {
            for (var i = index; i < _count - 1; i++)
            {
                _slots[i] = _slots[i + 1];
                _along[i] = _along[i + 1];
                _onPath[i] = _onPath[i + 1];
            }

            _count--;
        }

        private void SortSlotsByAlong()
        {
            for (var i = 1; i < _count; i++)
            {
                var board = _slots[i];
                var along = _along[i];
                var onPath = _onPath[i];
                var j = i - 1;
                while (j >= 0 && _along[j] > along)
                {
                    _slots[j + 1] = _slots[j];
                    _along[j + 1] = _along[j];
                    _onPath[j + 1] = _onPath[j];
                    j--;
                }

                _slots[j + 1] = board;
                _along[j + 1] = along;
                _onPath[j + 1] = onPath;
            }
        }

        private static int CopyBoards(ParsedPostedBoard[] src, ParsedPostedBoard[] dest) =>
            CopyBoards(src, dest, src == null ? 0 : src.Length);

        private static int CopyBoards(ParsedPostedBoard[] src, ParsedPostedBoard[] dest, int srcCount)
        {
            if (src == null || srcCount <= 0)
            {
                return 0;
            }

            var n = srcCount < dest.Length ? srcCount : dest.Length;
            if (n > src.Length)
            {
                n = src.Length;
            }

            for (var i = 0; i < n; i++)
            {
                dest[i] = src[i];
            }

            return n;
        }
    }
}
