using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// 16.2 ship 2 Route GO: ticks <see cref="RouteExecSession"/>, performs its Throw /
    /// Align through <see cref="MapsRouteListener"/>, and keeps the pin latch on the
    /// leg's frog. <see cref="PidSpeedGovernorListener"/> does the driving and braking.
    /// </summary>
    public sealed class RouteCommandExecutorListener : MonoBehaviour
    {
        private const float PollSeconds = 0.1f;
        private const float LicenseSeconds = 2f;

        internal static Action<string>? EmitLog;

        internal static RouteCommandExecutorListener? Instance { get; private set; }

        private float _nextPoll;
        private float _nextLicenseAt;
        private bool _dispatcherOk;
        private int _lastLogKey = int.MinValue;
        private bool _finalLegDismissed;

        private void OnEnable()
        {
            Instance = this;
            ResetLocal();
            RouteExecSession.Stop();
            YmsEventBus.OnMapsDestCommand += OnMapsDestCommand;
        }

        private void OnDisable()
        {
            YmsEventBus.OnMapsDestCommand -= OnMapsDestCommand;
            RouteExecSession.Stop();
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        private void ResetLocal()
        {
            _nextPoll = 0f;
            _nextLicenseAt = 0f;
            _lastLogKey = int.MinValue;
            _finalLegDismissed = false;
        }

        private void OnMapsDestCommand(MapsDestCommand command)
        {
            if (RouteExecSession.Active
                && (command.Kind == MapsDestKind.Clear || command.Kind == MapsDestKind.Set))
            {
                StopRoute(command.Kind == MapsDestKind.Clear ? "dest cleared" : "new dest");
            }
        }

        /// <summary>Desk Route GO. Aligns the whole corridor once, then captures the queue.</summary>
        internal string TryStartRouteGo()
        {
            var jobList = SwitchListSession.HasActive
                && !SwitchListSession.IsComplete
                && !RouteSwitchListBinder.IsRouteBound;
            var gate = RouteExecSession.CanStart(
                RoutePlanSession.Commands,
                MapsDeskPanel.HasDispatcherLicense(),
                SwitchListRunnerSession.IsGo,
                jobList);
            if (gate != RouteExecStartResult.Ok)
            {
                return Refuse(gate);
            }

            var router = MapsRouteListener.Instance;
            if (router == null)
            {
                return Refuse(RouteExecStartResult.AlignFailed);
            }

            router.TryAlignRoute(out var aligned, routeGoStart: true);
            if (!aligned)
            {
                return Refuse(RouteExecStartResult.AlignFailed);
            }

            router.RefreezeRouteCommands("route-go");
            var cmds = RoutePlanSession.Commands;
            if (cmds.Count == 0)
            {
                return Refuse(RouteExecStartResult.NoQueue);
            }

            // The latch reverse bit picks the lead car for CLEARED; it must match leg 1.
            var firstPin = RouteCommandExecutor.LegPinAfter(cmds, 0);
            if (firstPin != null)
            {
                RoutePinLatch.Relatch(firstPin, cmds[0].TravelReverse);
                EmitLog?.Invoke(RoutePinLatch.FormatLatchLog()!);
            }

            if (PidCruiseSession.Enabled)
            {
                PidCruiseSession.SetEnabled(false);
                EmitLog?.Invoke(PidSpeedTelemetry.FormatCruise(false));
            }

            PidGoStopSession.Clear();
            ResetLocal();
            RouteExecSession.Start(cmds);
            EmitLog?.Invoke(RouteExecTelemetry.FormatStart(cmds));
            return "Route GO";
        }

        internal void StopRoute(string why)
        {
            if (!RouteExecSession.Active)
            {
                return;
            }

            RouteExecSession.Stop();
            PidGoStopSession.Arm();
            ResetLocal();
            EmitLog?.Invoke(RouteExecTelemetry.FormatStop(why));
        }

        private string Refuse(RouteExecStartResult result)
        {
            EmitLog?.Invoke(RouteExecTelemetry.FormatRefuse(result));
            return RouteExecSession.FormatStartRefusal(result);
        }

        private void Update()
        {
            if (!RouteExecSession.Active)
            {
                return;
            }

            if (!WorldSessionGate.IsActive())
            {
                StopRoute("world");
                return;
            }

            var now = Time.unscaledTime;
            if (now < _nextPoll)
            {
                return;
            }

            _nextPoll = now + PollSeconds;
            try
            {
                TickOnce(now);
            }
            catch (Exception ex)
            {
                EmitLog?.Invoke(RouteExecTelemetry.Prefix + "tick " + ex.GetType().Name);
                StopRoute("error");
            }
        }

        private void TickOnce(float now)
        {
            if (now >= _nextLicenseAt)
            {
                _dispatcherOk = MapsDeskPanel.HasDispatcherLicense();
                _nextLicenseAt = now + LicenseSeconds;
            }

            var loco = UsableTrainProbe.TryGetUsableLoco();
            var speedKmh = loco != null ? SpeedDisplay.ToKilometersPerHour(loco.GetAbsSpeed()) : 0f;
            var input = new RouteExecInput(
                RoutePlanSession.HasPlan,
                _dispatcherOk,
                RouteClearanceSession.Phase,
                RouteClearanceSession.PinJunctionId,
                RouteClearanceSession.RemToClearedMeters,
                RoutePlanSession.RemainingMeters,
                speedKmh,
                ConsistLengthSession.Meters,
                BackupProximitySession.ClearanceMeters);

            var d = RouteExecSession.Tick(in input);
            var state = RouteExecSession.State;
            var count = RouteExecSession.Commands.Count;
            LogIfChanged(in state, count, in d);
            KeepPinOnLeg(in state, count, in d);

            switch (d.Action)
            {
                case RouteExecAction.Throw:
                    RunThrow(d.TargetId, d.Branch);
                    break;
                case RouteExecAction.Align:
                    RunAlign();
                    break;
                case RouteExecAction.Done:
                    Finish();
                    break;
            }
        }

        private void RunThrow(string? junctionId, int branch)
        {
            var router = MapsRouteListener.Instance;
            var ok = false;
            var line = "T2 align: unavailable";
            if (router != null && !string.IsNullOrEmpty(junctionId))
            {
                ok = router.TryThrowJunction(junctionId!, branch, out line);
            }

            EmitLog?.Invoke(line);
            RouteExecSession.ReportThrow(ok);
        }

        private void RunAlign()
        {
            var router = MapsRouteListener.Instance;
            var ok = false;
            if (router != null)
            {
                router.TryAlignRoute(out ok);
            }

            RouteExecSession.ReportThrow(ok);
        }

        private void Finish()
        {
            RouteExecSession.Stop();
            PidGoStopSession.Arm();
            ResetLocal();
            EmitLog?.Invoke(RouteExecTelemetry.FormatStop("done"));
            if (RouteSwitchListBinder.IsRouteBound)
            {
                SwitchListSession.Clear();
                EmitLog?.Invoke(RouteExecTelemetry.Prefix + "route list cleared");
            }
        }

        /// <summary>
        /// Leg with a Stop: the latch must name that leg's frog so CLEARED is measured on
        /// it. Final leg: hide the spent pin so the clearance coach goes idle.
        /// </summary>
        private void KeepPinOnLeg(in RouteExecState state, int count, in RouteExecDecision d)
        {
            if (d.LegPinId != null)
            {
                if (!string.Equals(RoutePinLatch.Id, d.LegPinId, StringComparison.Ordinal))
                {
                    RoutePinLatch.Relatch(d.LegPinId, d.TravelReverse);
                    RouteClearanceSession.ResetSawAtSwitchThisLeg();
                    EmitLog?.Invoke(RouteExecTelemetry.Prefix + "relatch " + d.LegPinId);
                }

                return;
            }

            if (!_finalLegDismissed
                && count > 1
                && state.Index == count - 1
                && d.Action == RouteExecAction.Drive)
            {
                _finalLegDismissed = true;
                RoutePinLatch.DismissDisplay();
                EmitLog?.Invoke(RouteExecTelemetry.Prefix + "final leg · pin dismissed");
            }
        }

        private void LogIfChanged(in RouteExecState state, int count, in RouteExecDecision d)
        {
            var key = RouteExecTelemetry.Key(in state, in d);
            if (key == _lastLogKey)
            {
                return;
            }

            _lastLogKey = key;
            EmitLog?.Invoke(RouteExecTelemetry.Format(in state, count, in d));
        }
    }
}
