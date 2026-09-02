using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// <b>13.1.15</b> change-only harvest: dest remaining, dest-yard behind,
    /// throttle writer. No HUD/AR/PID behavior.
    /// After governors so <c>thr=</c> matches the write.
    /// </summary>
    [DefaultExecutionOrder(32200)]
    public sealed class HarvestLogListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private const float RemainSampleSeconds = 0.25f;

        private RouteRemainLogCache _remain;
        private RouteDestYardBehindCache _yardBehind;
        private ThrottleWriterLogCache _writer;
        private float? _postedKmh;
        private float _nextRemainAt;

        private void OnEnable()
        {
            _remain = default;
            _yardBehind = default;
            _writer = default;
            _postedKmh = null;
            _nextRemainAt = 0f;
            ThrottleWriterNote.Reset();
            YmsEventBus.OnPostedLimitChanged += OnPosted;
        }

        private void OnDisable()
        {
            YmsEventBus.OnPostedLimitChanged -= OnPosted;
            _remain = default;
            _yardBehind = default;
            _writer = default;
            _postedKmh = null;
            ThrottleWriterNote.Reset();
        }

        private void OnPosted(PostedLimitSnapshot snapshot) => _postedKmh = snapshot.Kmh;

        private void Update()
        {
            try
            {
                TickRemain();
            }
            catch
            {
                // harvest only
            }
        }

        private void FixedUpdate()
        {
            try
            {
                TickWriter();
            }
            catch
            {
                // harvest only
            }
        }

        private void TickRemain()
        {
            var now = Time.unscaledTime;
            if (now < _nextRemainAt)
            {
                return;
            }

            _nextRemainAt = now + RemainSampleSeconds;
            var dest = RouteDestSession.TrackId;
            if (!RouteDestSession.HasDestination || string.IsNullOrEmpty(dest))
            {
                RouteHarvestTelemetry.NextRemain(null, null, ref _remain);
                var clearBehind = RouteHarvestTelemetry.NextDestYardBehind(false, ref _yardBehind);
                if (clearBehind != null)
                {
                    EmitLog?.Invoke(clearBehind);
                }

                return;
            }

            var graph = MapsRouteListener.Instance?.Graph;
            float? rem = null;
            if (TryCrowFliesRemain(graph, dest!, out var meters))
            {
                rem = meters;
            }

            var remainLine = RouteHarvestTelemetry.NextRemain(rem, dest, ref _remain);
            if (remainLine != null)
            {
                EmitLog?.Invoke(remainLine);
            }

            var destYard = RouteDestSession.YardId ?? PathRouteConstraints.YardIdOf(dest);
            var consistYard = PathRouteConstraints.YardIdOf(RouteOriginProbe.TryGet());
            var trackBehind = RouteFacingResolver.IsTrackBehind(graph, dest);
            var yardBehind = RouteHarvestTelemetry.IsDestYardBehind(
                destYard,
                consistYard,
                trackBehind);
            var behindLine = RouteHarvestTelemetry.NextDestYardBehind(yardBehind, ref _yardBehind);
            if (behindLine != null)
            {
                EmitLog?.Invoke(behindLine);
            }
        }

        private void TickWriter()
        {
            var worldReady = ScreenOverlayGate.WorldReady();
            var worldActive = HudWorldSession.IsActive(
                PlayerManager.PlayerTransform != null,
                worldReady);
            if (!worldActive)
            {
                return;
            }

            var loco = UsableTrainProbe.TryGetUsableLoco();
            if (loco == null || !loco.IsLoco)
            {
                return;
            }

            if (!ControlTelemetryListener.TryReadLevers(
                    loco,
                    out var throttle,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                return;
            }

            var wrote = ThrottleWriterNote.Peek(Time.frameCount);
            var spd = (int)Math.Round(
                SpeedDisplay.ToKilometersPerHour(loco.GetAbsSpeed()),
                MidpointRounding.AwayFromZero);
            int? limit = _postedKmh is float kmh
                ? (int)Math.Round(kmh, MidpointRounding.AwayFromZero)
                : (int?)null;
            var risk = DerailRiskReader.ReadConsist(loco).MaxPercent;
            var riskPct = risk is float r
                ? (int)Math.Round(r, MidpointRounding.AwayFromZero)
                : 0;
            var go = SwitchListRunner.PidGoActive(
                SwitchListRunnerSession.Mode,
                SwitchListSession.CurrentStep);
            var cruiseOrGo = PidCruiseSession.Enabled || go;
            var line = ThrottleWriterTelemetry.NextLog(
                wrote,
                ControlTelemetry.ToPct(throttle),
                spd,
                limit,
                riskPct,
                cruiseOrGo,
                ref _writer);
            if (line != null)
            {
                EmitLog?.Invoke(line);
            }
        }

        private static bool TryCrowFliesRemain(
            PathGraphMapper? graph,
            string destId,
            out float meters)
        {
            meters = 0f;
            if (graph == null || !graph.TryGetRailTrack(destId, out var rail) || rail == null)
            {
                return false;
            }

            try
            {
                var loco = PlayerManager.Car ?? PlayerManager.LastLoco;
                if (loco == null)
                {
                    return false;
                }

                var a = loco.transform.position;
                var b = rail.transform.position;
                var dx = b.x - a.x;
                var dz = b.z - a.z;
                meters = Mathf.Sqrt((dx * dx) + (dz * dz));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
