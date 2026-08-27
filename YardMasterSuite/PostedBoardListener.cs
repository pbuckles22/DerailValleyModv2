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
    /// only SetTravel + Tick + PublishIfChanged (chord FILO). No path graph.
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
                        "chord"));
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

                _aheadNearest[n++] = new AheadBoard(_funnel.BoardAt(i).ThroughKmh, along, "chord");
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
                "chord");
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
                    alongSrc: "chord"));
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
            PostedLimitTelemetry.Reset(ref _cache);
        }
    }
}
