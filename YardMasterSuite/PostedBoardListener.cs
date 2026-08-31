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
    /// SetTravel + corridor mark + Tick + Publish. Maps/Switch List legs
    /// require path-corridor Next (no Euclidean ghost 50).
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

        private readonly Dictionary<int, TrackPathAhead.Segment> _path =
            new Dictionary<int, TrackPathAhead.Segment>(TrackPathAhead.PathDictionaryCapacity);

        private readonly PathSegmentAlong[] _segAlong =
            new PathSegmentAlong[TrackPathAhead.MaxHops];

        private readonly JunctionBranchState[] _juncScratch =
            new JunctionBranchState[TrackPathAhead.MaxHops];

        private int _pathFp;

        private int _lastRetryTrackId;

        private string _alongSrc = "chord";

        private PostedLimitCache _cache;

        private bool _hasWarm;

        private string? _filoYard;

        private float _lastYardPollAt = -999f;

        private string? _polledYard;

        private float _lastRefreshAt = -999f;

        private int _emptyRetriesDone;

        private float _stickyTravelX;

        private float _stickyTravelZ;

        private bool _hasStickyTravel;

        private bool _lockLogged;

        private bool _boardsHarvestWritten;

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

            MaybeWarm(pos, travel, speedKmh, now);
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
            MarkCorridor(car, pos, travel, speedKmh, mapsLeg);
            if (!mapsLeg)
            {
                _boardsHarvestWritten = false;
            }
            else
            {
                MaybeWriteBoardsHarvest(pos, travel, mapsLeg: true);
            }
            if (_funnel.DirectionLocked && !_lockLogged)
            {
                _lockLogged = true;
                EmitLog?.Invoke(PostedBoardTelemetry.FormatFiloLock(_funnel.Count));
            }

            var countBefore = _funnel.Count;
            var stickyBefore = _funnel.StickyKmh;
            _funnel.Tick(
                pos.x,
                pos.y,
                pos.z,
                travel.x,
                travel.y,
                travel.z,
                speedKmh);
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

            if (PostedLimitFilo.ShouldRefillAfterPop(
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

        private void MaybeWarm(Vector3 pos, Vector3 travel, float speedKmh, float now)
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
            _emptyRetriesDone = all.Length == 0 ? _emptyRetriesDone + 1 : 0;
            PostedLimitTelemetry.Reset(ref _cache);
            _aheadFp = 0;
            _logSticky = float.NaN;
            _logNext = float.NaN;
            _path.Clear();
            _pathFp = 0;
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
            float speedKmh,
            bool mapsLeg)
        {
            _funnel.RequireOnPath = mapsLeg;
            _alongSrc = "chord";
            if (!mapsLeg)
            {
                return;
            }

            var track = LocoTrackProbe.ResolveTrack(car);
            var trackId = track == null ? 0 : track.GetInstanceID();
            var locoOnPath = trackId != 0 && _path.ContainsKey(trackId);
            var fp = _path.Count == 0
                ? 0
                : TrackPathAhead.ComputeJunctionFingerprint(_path, _juncScratch, out _);
            var pathValid = PostedPathAheadGate.PathStillValid(_path.Count > 0, trackId, locoOnPath);
            var rebuildThrow = PostedPathAheadGate.ShouldRebuildForThrow(
                _pathFp,
                fp,
                _path.Count > 0);
            var rebuildLoss = PostedPathAheadGate.ShouldRebuildForPathLoss(
                _path.Count > 0,
                pathValid);
            var retryEmpty = _path.Count == 0
                && PostedPathAheadGate.ShouldRetryPath(
                    _hasWarm,
                    hasPath: false,
                    trackId,
                    _lastRetryTrackId);
            if (rebuildThrow || rebuildLoss || retryEmpty)
            {
                _path.Clear();
                if (track != null)
                {
                    TrackPathAhead.TryBuild(
                        track,
                        pos,
                        travel,
                        PostedBoardActiveRoster.LookaheadMeters(speedKmh),
                        _path);
                }

                _pathFp = TrackPathAhead.ComputeJunctionFingerprint(_path, _juncScratch, out _);
                _lastRetryTrackId = trackId;
            }

            ApplyOnPathFlags();
        }

        private void ApplyOnPathFlags()
        {
            if (!_funnel.RequireOnPath)
            {
                return;
            }

            if (_path.Count == 0)
            {
                _alongSrc = "path-miss";
                _funnel.SetAllOnPath(false);
                return;
            }

            _alongSrc = "path";
            var n = 0;
            foreach (var seg in _path.Values)
            {
                if (n >= _segAlong.Length)
                {
                    break;
                }

                _segAlong[n++] = TrackPathAhead.ToAlong(seg);
            }

            for (var i = 0; i < _funnel.Count; i++)
            {
                var board = _funnel.BoardAt(i);
                _funnel.SetOnPath(
                    i,
                    PostedPathAheadGate.IsOnAnyCorridor(board.X, board.Z, _segAlong, n));
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
            _emptyRetriesDone = 0;
            _hasStickyTravel = false;
            _stickyTravelX = 0f;
            _stickyTravelZ = 0f;
            _lockLogged = false;
            _aheadFp = 0;
            _logSticky = float.NaN;
            _logNext = float.NaN;
            _path.Clear();
            _pathFp = 0;
            _lastRetryTrackId = 0;
            _alongSrc = "chord";
            _boardsHarvestWritten = false;
            PostedLimitTelemetry.Reset(ref _cache);
        }

        private void MaybeWriteBoardsHarvest(Vector3 pos, Vector3 travel, bool mapsLeg)
        {
            var pathN = 0;
            if (_path.Count > 0)
            {
                foreach (var seg in _path.Values)
                {
                    if (pathN >= _segAlong.Length)
                    {
                        break;
                    }

                    _segAlong[pathN++] = TrackPathAhead.ToAlong(seg);
                }
            }

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
    }
}
