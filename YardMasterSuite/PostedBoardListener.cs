using System;
using System.Collections.Generic;
using DV.Signs;
using UnityEngine;
using YardMasterSuite.Core;
using Object = UnityEngine.Object;

namespace YardMasterSuite
{
    /// <summary>
    /// Posted Limit: event FoT warms <see cref="PostedLimitFunnel"/>; cab tick
    /// SetTravel + corridor mark + Tick (off-Maps) or Evaluate (Maps dest)
    /// + Publish. Maps/Switch List legs require path-corridor Next.
    /// </summary>
    public sealed class PostedBoardListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        internal static Func<bool>? IsWorldSession;

        /// <summary>
        /// Hitch isolate (2.8.1.13–15). False = EventBus→HUD Limit/Next live.
        /// LogAhead still only runs after PublishIfChanged (sticky/Next km/h or bucket).
        /// </summary>
        internal const bool IsolateEventBus = false;

        /// <summary>
        /// Hitch isolate (2.8.1.15). False = cab SetTravel/Tick/Refill/Publish run.
        /// Limit math was exonerated; leftover hitch is other cab systems.
        /// </summary>
        internal const bool IsolateLimitTick = false;

        private readonly PostedLimitFunnel _funnel = new PostedLimitFunnel();

        private readonly List<ParsedPostedBoard> _roster = new List<ParsedPostedBoard>(64);

        private readonly PathSegmentAlong[] _segAlong =
            new PathSegmentAlong[TrackPathAhead.MaxHops];

        private readonly Dictionary<int, TrackPathAhead.Segment> _livePath =
            new Dictionary<int, TrackPathAhead.Segment>(TrackPathAhead.PathDictionaryCapacity);

        private readonly int[] _walkTrackIds = new int[TrackPathAhead.MaxHops];

        private readonly JunctionBranchState[] _juncScratch =
            new JunctionBranchState[TrackGraphDump.MaxJunctions];

        private int _pathFp;

        private int _pathSegCount;

        private int _lastRetryTrackId;

        private string _alongSrc = "chord";

        private PostedLimitCache _cache;

        private bool _hasWarm;

        private string? _filoYard;

        private float _lastYardPollAt = -999f;

        private string? _polledYard;

        private float _lastRefreshAt = -999f;

        private float _lastWarmX;

        private float _lastWarmZ;

        private bool _hasWarmOrigin;

        private float _travelSinceWarmMeters;

        private int _emptyRetriesDone;

        private float _stickyTravelX;

        private float _stickyTravelZ;

        private bool _hasStickyTravel;

        private bool _lockLogged;

        private bool _boardsHarvestWritten;

        private bool _graphHarvestWritten;

        private float _graphScanAt = -999f;

        private readonly ParsedPostedBoard[] _harvestBoardScratch =
            new ParsedPostedBoard[128];

        private int _aheadFp;

        private float _logSticky = float.NaN;

        private float _logNext = float.NaN;

        private readonly AheadBoard[] _aheadNearest = new AheadBoard[AheadBoards.DiagnosticCap];

        private void OnEnable()
        {
            ResetSession();
        }

        private void OnDisable()
        {
            ResetSession();
        }

        private void Update()
        {
            var inWorld = IsWorldSession?.Invoke() ?? false;
            var car = inWorld ? UsableTrainProbe.TryGetUsableLoco() : null;
            if (car == null || !car.IsLoco)
            {
                return;
            }

            Vector3 pos;
            Vector3 travel;
            float speedKmh;
            try
            {
                pos = car.transform.position;
                travel = TravelForward(car);
                speedKmh = SpeedDisplay.ToKilometersPerHour(car.GetAbsSpeed());
            }
            catch
            {
                return;
            }

            var now = Time.unscaledTime;
            if (_hasWarm && speedKmh > 0.5f)
            {
                _travelSinceWarmMeters += speedKmh * (Time.deltaTime / 3.6f);
            }

            if (_hasStickyTravel
                && PostedStickyLimit.ShouldClearForReverse(
                    speedKmh,
                    _stickyTravelX,
                    _stickyTravelZ,
                    travel.x,
                    travel.z))
            {
                SoftWarm("reverse", pos, travel, preserveSticky: null);
            }

            MaybeWarm(pos, travel, speedKmh, now, car);
            if (!_hasWarm || IsolateLimitTick)
            {
                return;
            }

            _funnel.SetTravel(
                travel.x,
                travel.y,
                travel.z,
                speedKmh,
                pos.x,
                pos.y,
                pos.z);
            var mapsLeg = RouteDestSession.HasDestination
                || (SwitchListSession.HasActive && !SwitchListSession.IsComplete);
            MarkCorridor(car, pos, travel, speedKmh);
            if (!mapsLeg)
            {
                _boardsHarvestWritten = false;
                _graphHarvestWritten = false;
                _graphScanAt = -999f;
            }
            else
            {
                MaybeWriteBoardsHarvest(pos, travel, mapsLeg: true);
                MaybeWriteTrackGraph(pos, travel, speedKmh, now);
            }
            if (_funnel.DirectionLocked && !_lockLogged)
            {
                _lockLogged = true;
                EmitLog?.Invoke(PostedBoardTelemetry.FormatFiloLock(_funnel.Count));
            }

            var countBefore = _funnel.Count;
            var stickyBefore = _funnel.StickyKmh;
            var useEvaluate = PostedPathAheadGate.ShouldEvaluateMapsAuthority(_pathSegCount);
            if (useEvaluate)
            {
                var onSpan = LocoTrackProbe.TryResolveSpan(car, out var locoTrackId, out var locoSpan);
                _alongSrc = onSpan ? "span" : "path";
                _funnel.Evaluate(
                    _roster,
                    _segAlong,
                    _pathSegCount,
                    pos.x,
                    pos.y,
                    pos.z,
                    travel.x,
                    travel.y,
                    travel.z,
                    speedKmh,
                    onSpan ? locoTrackId : LocoTrackProbe.ResolveTrackId(car),
                    locoSpan);
            }
            else
            {
                _funnel.Tick(
                    pos.x,
                    pos.y,
                    pos.z,
                    travel.x,
                    travel.y,
                    travel.z,
                    speedKmh);
            }

            if (_funnel.StickyKmh is float taken
                && (stickyBefore is not float was || was != taken))
            {
                EmitLog?.Invoke(
                    PostedBoardTelemetry.FormatFiloTake(
                        taken,
                        _funnel.LastTakeAlongMeters,
                        _alongSrc));
                _stickyTravelX = travel.x;
                _stickyTravelZ = travel.z;
                _hasStickyTravel = true;
            }

            if (!useEvaluate
                && PostedLimitFilo.ShouldRefillAfterPop(
                    countBefore,
                    _funnel.Count,
                    _funnel.ActiveCapacity,
                    _roster.Count))
            {
                _funnel.RefillFrom(
                    _roster,
                    pos.x,
                    pos.y,
                    pos.z,
                    travel.x,
                    travel.y,
                    travel.z);
                ApplyOnPathFlags();
            }

            // EventBus on publish; FormatAhead only if km/h actually changed (ShouldLogAhead).
            if (_funnel.PublishIfChanged(ref _cache, raiseEvent: !IsolateEventBus)
                && !IsolateEventBus)
            {
                LogAhead(speedKmh);
            }
        }

        private void MaybeWarm(Vector3 pos, Vector3 travel, float speedKmh, float now, TrainCar car)
        {
            if (PostedPathAheadGate.YardPollDue(now, _lastYardPollAt))
            {
                _lastYardPollAt = now;
                _polledYard = TryYardId();
            }

            if (!_hasWarm)
            {
                SoftWarm("spawn", pos, travel, preserveSticky: null);
            }
            else if (PostedLimitFilo.ShouldRewarmForYard(_filoYard, _polledYard))
            {
                SoftWarm(
                    "town " + (_filoYard ?? "—") + "→" + (_polledYard ?? "—"),
                    pos,
                    travel,
                    preserveSticky: _funnel.StickyKmh);
            }
            else if (PostedLimitFilo.ShouldEmptyFot()
                && _funnel.Count == 0
                && _emptyRetriesDone < PostedBoardActiveRoster.MaxEmptyRetries
                && now - _lastRefreshAt >= PostedBoardActiveRoster.EmptyRetrySeconds)
            {
                SoftWarm("empty", pos, travel, preserveSticky: _funnel.StickyKmh);
            }
            else if (_roster.Count > 0
                && PostedBoardActiveRoster.NeedsTravelRefresh(
                    _travelSinceWarmMeters,
                    pos.x,
                    pos.z,
                    _lastWarmX,
                    _lastWarmZ,
                    _hasWarmOrigin))
            {
                TravelRefresh(pos, travel, speedKmh, car);
            }
        }

        private void SoftWarm(
            string reason,
            Vector3 origin,
            Vector3 travel,
            float? preserveSticky)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var raw = RefreshRoster(origin);
            var all = _roster.Count == 0
                ? Array.Empty<ParsedPostedBoard>()
                : _roster.ToArray();
            _funnel.Warm(
                all,
                origin.x,
                origin.y,
                origin.z,
                travel.x,
                travel.y,
                travel.z,
                preserveSticky);
            _hasWarm = true;
            if (preserveSticky is null)
            {
                _hasStickyTravel = false;
            }

            _lockLogged = false;
            if (!string.IsNullOrEmpty(_polledYard))
            {
                _filoYard = _polledYard;
            }

            _lastRefreshAt = Time.unscaledTime;
            _lastWarmX = origin.x;
            _lastWarmZ = origin.z;
            _hasWarmOrigin = true;
            _travelSinceWarmMeters = 0f;
            _emptyRetriesDone = all.Length == 0 ? _emptyRetriesDone + 1 : 0;
            PostedLimitTelemetry.Reset(ref _cache);
            _aheadFp = 0;
            _logSticky = float.NaN;
            _logNext = float.NaN;
            _pathFp = 0;
            _pathSegCount = 0;
            _livePath.Clear();
            _lastRetryTrackId = 0;
            sw.Stop();
            EmitLog?.Invoke(
                PostedBoardTelemetry.FormatFiloWarm(
                    reason,
                    _funnel.PlusCount,
                    _funnel.MinusCount,
                    raw,
                    all.Length,
                    sw.ElapsedMilliseconds));
            EmitLog?.Invoke(FormatFiloHead());
        }

        /// <summary>
        /// Re-scan FoT after ~1 km travel without funnel Warm (preserves sticky,
        /// direction lock, and take-detector memory).
        /// </summary>
        private void TravelRefresh(Vector3 pos, Vector3 travel, float speedKmh, TrainCar car)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var raw = RefreshRoster(pos);
            _lastRefreshAt = Time.unscaledTime;
            _lastWarmX = pos.x;
            _lastWarmZ = pos.z;
            _hasWarmOrigin = true;
            _travelSinceWarmMeters = 0f;
            RebuildLivePath(car, pos, travel, speedKmh);
            var onSpan = LocoTrackProbe.TryResolveSpan(car, out var locoTrackId, out var locoSpan);
            _funnel.SeedRefreshBehindFromRoster(
                _roster,
                _segAlong,
                _pathSegCount,
                pos.x,
                pos.y,
                pos.z,
                travel.x,
                travel.y,
                travel.z,
                onSpan ? locoTrackId : LocoTrackProbe.ResolveTrackId(car),
                locoSpan);
            sw.Stop();
            EmitLog?.Invoke(
                PostedBoardTelemetry.FormatFiloWarm(
                    "travel",
                    _funnel.PlusCount,
                    _funnel.MinusCount,
                    raw,
                    _roster.Count,
                    sw.ElapsedMilliseconds));
        }

        private string FormatFiloHead()
        {
            float? plusKmh = null;
            float? minusKmh = null;
            var plusAlong = 0f;
            var minusAlong = 0f;
            for (var i = 0; i < _funnel.Count; i++)
            {
                var along = _funnel.AlongAt(i);
                var kmh = _funnel.BoardAt(i).ThroughKmh;
                if (along >= 0f && plusKmh is null)
                {
                    plusKmh = kmh;
                    plusAlong = along;
                }
                else if (along < 0f && minusKmh is null)
                {
                    minusKmh = kmh;
                    minusAlong = along;
                }
            }

            return PostedBoardTelemetry.FormatFiloHead(plusKmh, plusAlong, minusKmh, minusAlong);
        }

        private void LogAhead(float speedKmh)
        {
            var snap = _funnel.ToSnapshot();
            var sticky = snap.Kmh ?? SpeedLimitState.UnrestrictedKmh;
            if (!PostedBoardTelemetry.ShouldLogAhead(
                    sticky,
                    snap.NextKmh,
                    _logSticky,
                    _logNext))
            {
                return;
            }

            _logSticky = sticky;
            _logNext = snap.NextKmh ?? -1f;
            var n = 0;
            for (var i = 0; i < _funnel.Count && n < _aheadNearest.Length; i++)
            {
                var along = _funnel.AlongAt(i);
                if (!PostedLimitFilo.IsVisibleAlong(along))
                {
                    continue;
                }

                _aheadNearest[n++] = new AheadBoard(_funnel.BoardAt(i).ThroughKmh, along, _alongSrc);
            }

            var fp = PostedBoardTelemetry.AheadFingerprint(
                sticky,
                snap.NextKmh,
                snap.NextAlongMeters,
                _aheadNearest,
                n,
                null,
                0f,
                null,
                _alongSrc);
            if (fp == _aheadFp)
            {
                return;
            }

            _aheadFp = fp;
            EmitLog?.Invoke(
                PostedBoardTelemetry.FormatAhead(
                    sticky,
                    speedKmh,
                    snap.NextKmh,
                    snap.NextAlongMeters,
                    _aheadNearest,
                    n,
                    alongSrc: _alongSrc));
        }

        private static string? TryYardId()
        {
            try
            {
                StationOfficeAnchor.TryGet(out _, out _, out _, out var yard);
                return yard;
            }
            catch
            {
                return null;
            }
        }

        private int RefreshRoster(Vector3 origin)
        {
            _roster.Clear();
            var raw = 0;
            try
            {
                var signs = Object.FindObjectsOfType<SignDebug>();
                raw = signs == null ? 0 : signs.Length;
                if (signs != null)
                {
                    for (var i = 0; i < signs.Length; i++)
                    {
                        var sign = signs[i];
                        if (sign == null)
                        {
                            continue;
                        }

                        var p = sign.transform.position;
                        if (!PostedBoardActiveRoster.WithinActiveRadius(
                                p.x,
                                p.y,
                                p.z,
                                origin.x,
                                origin.y,
                                origin.z))
                        {
                            continue;
                        }

                        var dual = SpeedLimitBoardParser.ParseDual(sign.text);
                        if (dual is null)
                        {
                            continue;
                        }

                        var sf = sign.transform.forward;
                        var sr = sign.transform.right;
                        var isDual = dual.Value.IsDual;
                        _roster.Add(
                            new ParsedPostedBoard(
                                sign.GetInstanceID(),
                                p.x,
                                p.y,
                                p.z,
                                sf.x,
                                sf.z,
                                sr.x,
                                sr.z,
                                dual.Value.ThroughKmh,
                                isDual
                                    ? dual.Value.DivergeKmh ?? dual.Value.ThroughKmh
                                    : dual.Value.ThroughKmh,
                                isDual,
                                junctionNearby: isDual));
                    }
                }
            }
            catch
            {
                _roster.Clear();
            }

            return raw;
        }

        private static Vector3 TravelForward(TrainCar loco)
        {
            var fwd = loco.transform.forward;
            try
            {
                if (loco.GetForwardSpeed() < 0f)
                {
                    fwd = -fwd;
                }
            }
            catch
            {
                // keep transform forward
            }

            return fwd;
        }

        private void MarkCorridor(
            TrainCar car,
            Vector3 pos,
            Vector3 travel,
            float speedKmh)
        {
            _funnel.RequireOnPath = false;
            _alongSrc = "chord";
            var track = LocoTrackProbe.ResolveTrack(car);
            var trackId = track == null ? 0 : track.GetInstanceID();
            var hasPath = _pathSegCount > 0;
            var locoOnPath = CorePathfinder.PathContainsTrack(_walkTrackIds, _pathSegCount, trackId);
            var fp = TrackPathAhead.ComputeJunctionFingerprint(
                _livePath,
                _juncScratch,
                out _);
            var pathValid = PostedPathAheadGate.PathStillValid(hasPath, trackId, locoOnPath);
            var rebuildThrow = PostedPathAheadGate.ShouldRebuildForThrow(_pathFp, fp, hasPath);
            var rebuildLoss = PostedPathAheadGate.ShouldRebuildForPathLoss(hasPath, pathValid);
            var retryEmpty = !hasPath
                && PostedPathAheadGate.ShouldRetryPath(
                    _hasWarm,
                    hasPath: false,
                    trackId,
                    _lastRetryTrackId);
            if (rebuildThrow || rebuildLoss || retryEmpty)
            {
                RebuildLivePath(car, pos, travel, speedKmh);
                _lastRetryTrackId = trackId;
            }

            if (!PostedPathAheadGate.ShouldEvaluateMapsAuthority(_pathSegCount))
            {
                ApplyOnPathFlags();
            }
        }

        private void RebuildLivePath(TrainCar car, Vector3 pos, Vector3 travel, float speedKmh)
        {
            _pathSegCount = 0;
            _livePath.Clear();
            var start = LocoTrackProbe.ResolveTrack(car);
            var lookahead = PostedBoardActiveRoster.LookaheadMeters(speedKmh);
            if (!TrackPathAhead.TryBuild(start, pos, travel, lookahead, _livePath))
            {
                _pathFp = 0;
                return;
            }

            _pathSegCount = TrackPathAhead.CopyToAlong(_livePath, _segAlong, _walkTrackIds);
            _pathFp = TrackPathAhead.ComputeJunctionFingerprint(
                _livePath,
                _juncScratch,
                out _);
            StampBoardTrackIds();
            EmitLog?.Invoke(PostedBoardTelemetry.FormatWalkerPath(_pathSegCount, _pathFp));
        }

        private void StampBoardTrackIds()
        {
            if (_livePath.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _roster.Count; i++)
            {
                var board = _roster[i];
                if (board.HasSpan)
                {
                    // Signs are static: hop and span never change once resolved.
                    continue;
                }

                var boardPos = new Vector3(board.X, board.Y, board.Z);
                if (TrackPathAhead.TryResolveBoardSpan(_livePath, boardPos, out var tid, out var span))
                {
                    _roster[i] = board.WithTrackSpan(tid, span);
                }
                else if (board.TrackId != 0)
                {
                    _roster[i] = board.WithTrackId(0);
                }
            }
        }

        private void ApplyOnPathFlags()
        {
            if (!_funnel.RequireOnPath)
            {
                return;
            }

            if (_pathSegCount == 0)
            {
                _alongSrc = "path-miss";
                _funnel.SetAllOnPath(false);
                return;
            }

            _alongSrc = "path";
            for (var i = 0; i < _funnel.Count; i++)
            {
                var board = _funnel.BoardAt(i);
                _funnel.SetOnPath(
                    i,
                    PostedPathAheadGate.IsBoardOnPath(
                        in board,
                        _segAlong,
                        _pathSegCount));
            }
        }

        private void ResetSession()
        {
            _funnel.Reset();
            _roster.Clear();
            _hasWarm = false;
            _filoYard = null;
            _polledYard = null;
            _lastYardPollAt = -999f;
            _lastRefreshAt = -999f;
            _lastWarmX = 0f;
            _lastWarmZ = 0f;
            _hasWarmOrigin = false;
            _travelSinceWarmMeters = 0f;
            _emptyRetriesDone = 0;
            _hasStickyTravel = false;
            _stickyTravelX = 0f;
            _stickyTravelZ = 0f;
            _lockLogged = false;
            _aheadFp = 0;
            _logSticky = float.NaN;
            _logNext = float.NaN;
            _pathFp = 0;
            _pathSegCount = 0;
            _livePath.Clear();
            _lastRetryTrackId = 0;
            _alongSrc = "chord";
            _boardsHarvestWritten = false;
            _graphHarvestWritten = false;
            _graphScanAt = -999f;
            PostedLimitTelemetry.Reset(ref _cache);
        }

        private void MaybeWriteBoardsHarvest(Vector3 pos, Vector3 travel, bool mapsLeg)
        {
            var pathN = _pathSegCount;
            var boardN = _roster.Count;
            if (boardN > _harvestBoardScratch.Length)
            {
                boardN = _harvestBoardScratch.Length;
            }

            if (!PostedBoardHarvestPolicy.ShouldWrite(
                    _boardsHarvestWritten,
                    mapsLeg,
                    pathN,
                    boardN))
            {
                return;
            }

            for (var i = 0; i < boardN; i++)
            {
                _harvestBoardScratch[i] = _roster[i];
            }

            var origin = _polledYard ?? _filoYard;
            if (string.IsNullOrEmpty(origin))
            {
                origin = "path";
            }

            var written = PostedBoardHarvestDump.Write(
                origin,
                pos.x,
                pos.z,
                travel.x,
                travel.z,
                _segAlong,
                pathN,
                _harvestBoardScratch,
                boardN);
            if (written != null)
            {
                _boardsHarvestWritten = true;
            }
        }

        private void MaybeWriteTrackGraph(Vector3 pos, Vector3 travel, float speedKmh, float now)
        {
            var still = TrackGraphHarvestPolicy.IsStill(speedKmh);
            if (!TrackGraphHarvestPolicy.ShouldScan(_graphHarvestWritten, mapsLeg: true, still))
            {
                return;
            }

            if (now - _graphScanAt < TrackGraphDump.FailedScanCooldownSeconds)
            {
                return;
            }

            _graphScanAt = now;
            var origin = _polledYard ?? _filoYard;
            if (string.IsNullOrEmpty(origin))
            {
                origin = "path";
            }

            var written = TrackGraphDump.Write(
                origin,
                pos.x,
                pos.y,
                pos.z,
                travel.x,
                travel.z,
                _roster,
                _roster.Count);
            if (written != null)
            {
                _graphHarvestWritten = true;
            }
        }
    }
}
