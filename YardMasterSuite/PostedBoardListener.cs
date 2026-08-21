using System;
using System.Collections.Generic;
using DV.Signs;
using UnityEngine;
using YardMasterSuite.Core;
using Object = UnityEngine.Object;

namespace YardMasterSuite
{
    /// <summary>
    /// Indexes nearby <see cref="SignDebug"/> boards (rare FoT) and publishes
    /// sticky posted Limit plus Next along the thrown route (6.10). Dual board
    /// numbers stay through-only.
    /// </summary>
    public sealed class PostedBoardListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        internal static Func<bool>? IsWorldSession;

        private const float BoardTrackAttachMeters = 12f;

        private const float PathRebuildMeters = 25f;

        private readonly List<ParsedPostedBoard> _roster = new List<ParsedPostedBoard>(64);

        private readonly HashSet<int> _boardTrackResolved = new HashSet<int>();

        private readonly Dictionary<int, RailTrack> _boardTracks = new Dictionary<int, RailTrack>();

        private readonly WorldSpeedBoardIndex _index = new WorldSpeedBoardIndex();

        private readonly BoardTakeDetector _takes = new BoardTakeDetector();

        private readonly List<AheadBoard> _ahead = new List<AheadBoard>(32);

        private readonly Dictionary<int, TrackPathAhead.Segment> _pathAhead =
            new Dictionary<int, TrackPathAhead.Segment>(64);

        private PostedLimitCache _cache;

        private float? _stickyKmh;

        private float _stickyTravelX;

        private float _stickyTravelZ;

        private bool _hasStickyTravel;

        private float _lastRefreshAt = -999f;

        private float _lastOriginX;

        private float _lastOriginZ;

        private bool _hasLastOrigin;

        private int _emptyRetriesDone;

        private bool _hasPath;

        private int _pathTrackId;

        private float _pathAtX;

        private float _pathAtZ;

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

            if (_hasStickyTravel
                && PostedStickyLimit.ShouldClearForReverse(
                    speedKmh,
                    _stickyTravelX,
                    _stickyTravelZ,
                    travel.x,
                    travel.z))
            {
                ClearSticky();
            }

            var now = Time.unscaledTime;
            if (PostedBoardActiveRoster.NeedsRefresh(
                    now,
                    _lastRefreshAt,
                    pos.x,
                    pos.z,
                    _lastOriginX,
                    _lastOriginZ,
                    _hasLastOrigin,
                    rosterEmpty: _roster.Count == 0,
                    emptyRetriesDone: _emptyRetriesDone))
            {
                RefreshRoster(pos);
                _lastRefreshAt = now;
                _lastOriginX = pos.x;
                _lastOriginZ = pos.z;
                _hasLastOrigin = true;
                if (_roster.Count == 0)
                {
                    _emptyRetriesDone++;
                }
                else
                {
                    _emptyRetriesDone = 0;
                }
            }

            var locoTrack = LocoTrackProbe.ResolveTrack(car);
            var locoTrackId = locoTrack == null ? 0 : locoTrack.GetInstanceID();
            var lookahead = PostedBoardActiveRoster.LookaheadMeters(speedKmh);
            var hasPath = RebuildPath(locoTrack, locoTrackId, pos, travel, lookahead);
            ObserveRoster(
                pos,
                travel,
                locoTrack,
                lookahead,
                hasPath,
                out var takenKmh,
                out var seedBehind);
            seedBehind ??= _index.SeedBehind(
                locoTrackId,
                pos.x,
                pos.y,
                pos.z,
                travel.x,
                travel.z,
                PostedBoardActiveRoster.LookbackMeters);

            var sticky = PostedStickyLimit.Resolve(_stickyKmh, takenKmh, seedBehind, speedKmh);
            if (sticky is not null)
            {
                _stickyKmh = sticky;
                _stickyTravelX = travel.x;
                _stickyTravelZ = travel.z;
                _hasStickyTravel = true;
            }

            var fromKmh = _stickyKmh ?? SpeedLimitState.UnrestrictedKmh;
            var next = AheadBoards.NextDifferent(fromKmh, _ahead);
            Publish(new PostedLimitSnapshot(
                _stickyKmh,
                _roster.Count,
                next?.Kmh,
                next?.AlongMeters));
        }

        private void ObserveRoster(
            Vector3 pos,
            Vector3 travel,
            RailTrack? locoTrack,
            float lookaheadMeters,
            bool hasPath,
            out float? takenKmh,
            out float? seedBehind)
        {
            takenKmh = null;
            seedBehind = null;
            _ahead.Clear();
            var bestBehindAlong = float.NegativeInfinity;
            var search = Math.Max(
                PostedBoardActiveRoster.LookbackMeters,
                lookaheadMeters);
            var searchSq = search * search;

            for (var i = 0; i < _roster.Count; i++)
            {
                var board = _roster[i];
                var dx = board.X - pos.x;
                var dy = board.Y - pos.y;
                var dz = board.Z - pos.z;
                if ((dx * dx) + (dy * dy) + (dz * dz) > searchSq)
                {
                    continue;
                }

                var boardTrack = ResolveBoardTrack(board.InstanceId, board.X, board.Y, board.Z);
                var onPath = hasPath
                    && boardTrack != null
                    && _pathAhead.ContainsKey(boardTrack.GetInstanceID());
                if (PostedBoardRoute.IsOffRoute(hasPath, boardTrack != null, onPath))
                {
                    continue;
                }

                var boardPos = new Vector3(board.X, board.Y, board.Z);
                float pathAlong = 0f;
                var routeTravel = travel;
                var sampled = onPath
                    && TrackPathAhead.TrySample(
                        _pathAhead,
                        boardTrack,
                        boardPos,
                        out pathAlong,
                        out routeTravel);
                var along = sampled
                    ? pathAlong
                    : (dx * travel.x) + (dy * travel.y) + (dz * travel.z);
                var faceTravel = sampled ? routeTravel : travel;
                if (along > lookaheadMeters
                    || along < -PostedBoardActiveRoster.LookbackMeters)
                {
                    continue;
                }

                var trackKnown = boardTrack != null && locoTrack != null;
                var onOurTrack = onPath || (trackKnown && boardTrack == locoTrack);
                var eval = SpeedLimitBoardFacing.Evaluate(
                    board.ForwardX,
                    board.ForwardZ,
                    board.RightX,
                    board.RightZ,
                    faceTravel.x,
                    faceTravel.z,
                    dx,
                    dz,
                    board.IsDual,
                    board.JunctionNearby,
                    onOurTrack,
                    trackKnown || onPath);
                if (!eval.Governs)
                {
                    continue;
                }

                var kmh = PostedBoardActiveRoster.PickKmh(board, diverging: false);
                var take = _takes.Observe(board.InstanceId, kmh, along);
                if (take is float taken)
                {
                    takenKmh = taken;
                }

                if (boardTrack != null)
                {
                    _index.Remember(
                        boardTrack.GetInstanceID(),
                        kmh,
                        board.X,
                        board.Y,
                        board.Z,
                        faceTravel.x,
                        faceTravel.z);
                }

                if (along > 0f && along <= lookaheadMeters)
                {
                    _ahead.Add(new AheadBoard(kmh, along));
                }

                if (along < 0f && along >= -PostedBoardActiveRoster.LookbackMeters && along > bestBehindAlong)
                {
                    bestBehindAlong = along;
                    seedBehind = kmh;
                }
            }
        }

        private void RefreshRoster(Vector3 origin)
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
                                junctionNearby: false));
                    }
                }
            }
            catch
            {
                _roster.Clear();
            }

            EmitLog?.Invoke(PostedBoardTelemetry.FormatFot(raw, _roster.Count));
        }

        private bool RebuildPath(
            RailTrack? locoTrack,
            int locoTrackId,
            Vector3 pos,
            Vector3 travel,
            float lookaheadMeters)
        {
            if (_hasPath && locoTrackId != 0 && locoTrackId == _pathTrackId)
            {
                var dx = pos.x - _pathAtX;
                var dz = pos.z - _pathAtZ;
                if ((dx * dx) + (dz * dz) < PathRebuildMeters * PathRebuildMeters)
                {
                    return true;
                }
            }

            _hasPath = TrackPathAhead.TryBuild(locoTrack, pos, travel, lookaheadMeters, _pathAhead);
            _pathTrackId = locoTrackId;
            _pathAtX = pos.x;
            _pathAtZ = pos.z;
            return _hasPath;
        }

        private RailTrack? ResolveBoardTrack(int instanceId, float x, float y, float z)
        {
            if (_boardTrackResolved.Contains(instanceId))
            {
                return _boardTracks.TryGetValue(instanceId, out var cached) ? cached : null;
            }

            _boardTrackResolved.Add(instanceId);
            var track = TryResolveBoardTrackFallback(x, y, z);
            if (track != null)
            {
                _boardTracks[instanceId] = track;
            }

            return track;
        }

        private static RailTrack? TryResolveBoardTrackFallback(float x, float y, float z)
        {
            try
            {
                var tracks = RailTrackRegistry.RailTracks;
                if (tracks == null || tracks.Length == 0)
                {
                    return null;
                }

                var pos = new Vector3(x, y, z);
                var rail = RailTrack.GetClosest(pos, 0f, tracks).Item1;
                if (rail != null
                    && RailTrack.GetClosestPoint(rail, pos, 0f).Item2 <= BoardTrackAttachMeters)
                {
                    return rail;
                }
            }
            catch
            {
                // fail closed — facing falls back to corridor
            }

            return null;
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

        private void Publish(PostedLimitSnapshot snapshot)
        {
            if (!PostedLimitTelemetry.Observe(in snapshot, ref _cache, out var published))
            {
                return;
            }

            YmsEventBus.RaisePostedLimitChanged(published);
        }

        private void ClearSticky()
        {
            _stickyKmh = null;
            _hasStickyTravel = false;
            _stickyTravelX = 0f;
            _stickyTravelZ = 0f;
            _index.Clear();
            _takes.Reset();
        }

        private void ResetSession()
        {
            ClearSticky();
            _roster.Clear();
            _ahead.Clear();
            _pathAhead.Clear();
            _hasPath = false;
            _pathTrackId = 0;
            _boardTracks.Clear();
            _boardTrackResolved.Clear();
            _hasLastOrigin = false;
            _lastRefreshAt = -999f;
            _emptyRetriesDone = 0;
            PostedLimitTelemetry.Reset(ref _cache);
        }
    }
}
