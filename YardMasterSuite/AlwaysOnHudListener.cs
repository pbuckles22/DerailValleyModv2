using System;
using System.Collections.Generic;
using DV;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Always-on extras: Clock (**6.1**), Marked + Path (**6.11**). Station is **6.12**.
    /// </summary>
    public sealed class AlwaysOnHudListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private const KeyCode ParkMarkKey = KeyCode.Home;
        private const KeyCode PathDestKey = KeyCode.End;
        private const float PathSampleSeconds = 0.1f;

        private static readonly PathEdge[] NoEdges = Array.Empty<PathEdge>();

        private ClockCache _clock;
        private ParkMarkCache _mark;
        private PathCheckCache _path;
        private ParkDebugSnapshot? _lastMarkLog;
        private PathGraphMapper? _graph;
        private readonly Dictionary<string, int> _selected = new Dictionary<string, int>(64);
        private bool _clockKnown;
        private int _hour;
        private int _minute;
        private string? _pathChip;
        private PathCheckStatus _pathStatus;
        private int _pathMisaligned;
        private bool _pathHasDest;
        private float _nextPathAt;
        private bool _pathDirty;
        private string? _pathOriginKey;
        private string? _pathDestKey;
        private string? _stickyOrigin;
        private int _pathFp;
        private bool _pathFrozen;

        private void OnEnable()
        {
            _clock = default;
            _mark = default;
            _path = default;
            _lastMarkLog = null;
            _clockKnown = false;
            _hour = 0;
            _minute = 0;
            _pathChip = null;
            _pathStatus = PathCheckStatus.NoDestination;
            _pathMisaligned = 0;
            _pathHasDest = false;
            _pathDirty = true;
            _stickyOrigin = null;
            _graph = GetComponent<PathGraphMapper>();
            PublishIfChanged();
        }

        private void OnDisable()
        {
            ParkMarkSession.Clear();
            PathCheckSession.Clear();
            _clock = default;
            _mark = default;
            _path = default;
            _lastMarkLog = null;
            _graph = null;
            _selected.Clear();
        }

        private void Update()
        {
            if (PlayerManager.PlayerTransform == null)
            {
                return;
            }

            PollHotkeys();
        }

        private void LateUpdate()
        {
            if (PlayerManager.PlayerTransform == null)
            {
                return;
            }

            PublishIfChanged();
        }

        private void PollHotkeys()
        {
            var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Input.GetKeyDown(ParkMarkKey))
            {
                if (shift)
                {
                    ParkMarkSession.Clear();
                }
                else
                {
                    var t = PlayerManager.PlayerTransform;
                    var p = t.position;
                    ParkMarkSession.Set(p.x, p.y, p.z);
                }
            }

            if (!Input.GetKeyDown(PathDestKey))
            {
                return;
            }

            if (shift)
            {
                PathCheckSession.Clear();
                _pathDirty = true;
                return;
            }

            var key = LogicTrackKey.FromCar(UsableTrainProbe.TryGetTargetCar());
            if (key == null)
            {
                EmitLog?.Invoke("T2 path: fail (no track)");
                return;
            }

            PathCheckSession.SetDestination(key);
            _pathDirty = true;
        }

        private void PublishIfChanged()
        {
            if (PlayerManager.PlayerTransform == null)
            {
                return;
            }

            var known = TryGetGameTime(out var hour, out var minute);
            var wasClockSeeded = _clock.Seeded;
            var wasClockKnown = _clock.Known;
            var clockChanged = ClockTelemetry.Observe(known, hour, minute, ref _clock);
            if (clockChanged && known)
            {
                _clockKnown = true;
                _hour = hour;
                _minute = minute;
            }
            else if (clockChanged)
            {
                _clockKnown = false;
            }

            TryGetPlayerXz(out var playerX, out var playerZ);
            float? markX = null;
            float? markZ = null;
            var hasMark = ParkMarkSession.TryGet(out var mx, out var mz);
            if (hasMark)
            {
                markX = mx;
                markZ = mz;
            }

            var markChanged = ParkMarkTelemetry.Observe(
                hasMark,
                markX,
                markZ,
                playerX,
                playerZ,
                ref _mark);

            RefreshPathIfDue();
            var wasPathSeeded = _path.Seeded;
            var wasPathDest = _path.HasDest;
            var pathChanged = PathCheckTelemetry.Observe(
                _pathHasDest,
                _pathStatus,
                _pathMisaligned,
                ref _path);

            if (!clockChanged && !markChanged && !pathChanged)
            {
                return;
            }

            var marked = hasMark
                ? ParkMarkDisplay.FormatReturn(markX, markZ, playerX, playerZ)
                : null;
            var clock = _clockKnown ? ClockDisplay.Format(_hour, _minute) : string.Empty;
            var extras = AlwaysOnExtras.Join(marked, _pathChip, clock);
            YmsEventBus.RaiseAlwaysOnExtrasChanged(
                new HudBarSnapshot(extras, visible: extras.Length > 0));

            if (clockChanged)
            {
                var kind = ResolveLogKind(known, wasClockSeeded, wasClockKnown);
                var msg = ClockTelemetry.NextLog(hour, minute, kind);
                if (msg != null)
                {
                    EmitLog?.Invoke(msg);
                }
            }

            if (markChanged)
            {
                var snap = ParkMarkTelemetry.Snapshot(ref _mark);
                var msg = ParkMarkTelemetry.NextLog(_lastMarkLog, snap);
                _lastMarkLog = snap;
                if (msg != null)
                {
                    EmitLog?.Invoke(msg);
                }
            }

            if (pathChanged)
            {
                var kind = PathCheckTelemetry.ResolveLogKind(
                    wasPathSeeded,
                    wasPathDest,
                    _pathHasDest);
                var msg = PathCheckTelemetry.NextLog(kind, _pathStatus, _pathMisaligned);
                if (msg != null)
                {
                    EmitLog?.Invoke(msg);
                }
            }
        }

        private void RefreshPathIfDue()
        {
            var now = Time.unscaledTime;
            var force = _pathDirty;
            if (!force && now < _nextPathAt)
            {
                return;
            }

            _pathDirty = false;
            _nextPathAt = now + PathSampleSeconds;

            _pathHasDest = PathCheckSession.HasDestination;
            if (!_pathHasDest)
            {
                _pathStatus = PathCheckStatus.NoDestination;
                _pathMisaligned = 0;
                _pathChip = null;
                _pathOriginKey = null;
                _pathDestKey = null;
                _stickyOrigin = null;
                _pathFp = 0;
                _pathFrozen = false;
                return;
            }

            var dest = PathCheckSession.DestinationTrackId;
            var live = LogicTrackKey.FromCar(UsableTrainProbe.TryGetUsableLoco())
                ?? LogicTrackKey.FromCar(UsableTrainProbe.TryGetTargetCar());
            var origin = PathCheckOrigin.Sticky(live, _stickyOrigin);
            _stickyOrigin = origin;
            var frozen = _graph != null && _graph.HasFrozenPathCheck;
            var fp = frozen ? _graph!.JunctionFingerprint() : 0;
            if (!force
                && string.Equals(origin, _pathOriginKey, StringComparison.Ordinal)
                && string.Equals(dest, _pathDestKey, StringComparison.Ordinal)
                && fp == _pathFp
                && frozen == _pathFrozen)
            {
                return;
            }

            IReadOnlyList<PathEdge> edges = NoEdges;
            if (frozen)
            {
                edges = _graph!.PathCheckEdges;
                _graph.CopyJunctionSelected(_selected);
            }
            else
            {
                _selected.Clear();
            }

            var result = PathCheck.Evaluate(edges, _selected, origin, dest);
            _pathStatus = result.Status;
            _pathMisaligned = result.MisalignedCount;
            _pathChip = PathCheckDisplay.Format(result);
            _pathOriginKey = origin;
            _pathDestKey = dest;
            _pathFp = fp;
            _pathFrozen = frozen;
        }

        private static ClockLogKind ResolveLogKind(bool known, bool wasSeeded, bool wasKnown)
        {
            if (!known)
            {
                return ClockLogKind.Hide;
            }

            return !wasSeeded || !wasKnown ? ClockLogKind.Init : ClockLogKind.Change;
        }

        private static void TryGetPlayerXz(out float? x, out float? z)
        {
            x = null;
            z = null;
            var t = PlayerManager.PlayerTransform;
            if (t == null)
            {
                return;
            }

            var p = t.position;
            x = p.x;
            z = p.z;
        }

        private static bool TryGetGameTime(out int hour, out int minute)
        {
            hour = 0;
            minute = 0;
            try
            {
                var wrapper = DateTimeWrapper.Instance;
                if (wrapper == null)
                {
                    return false;
                }

                var t = wrapper.DateTime;
                hour = t.Hour;
                minute = t.Minute;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
