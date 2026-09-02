using System;
using System.Collections.Generic;
using System.Threading;
using DV;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Maps Set dest / Recheck → PathPlan; Align Route → ThreeGate switch throws (**8.2**).
    /// Pathfind runs on a worker; results drain on the main thread.
    /// </summary>
    public sealed class MapsRouteListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private PathGraphMapper? _graph;
        private RouteTelemetryCache _cache;
        private int _generation;

        internal static MapsRouteListener? Instance { get; private set; }

        internal PathGraphMapper? Graph => _graph;

        private void OnEnable()
        {
            Instance = this;
            _cache = default;
            _graph = GetComponent<PathGraphMapper>();
            YmsEventBus.OnMapsDestCommand += OnMapsDestCommand;
            YmsEventBus.OnRoutePlanReady += OnRoutePlanReady;
        }

        private void OnDisable()
        {
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }

            YmsEventBus.OnMapsDestCommand -= OnMapsDestCommand;
            YmsEventBus.OnRoutePlanReady -= OnRoutePlanReady;
            Interlocked.Increment(ref _generation);
            RoutePlanSession.Clear();
            RouteMemo.Clear();
            RoutePinLatch.Clear();
            _cache = default;
        }

        private void OnMapsDestCommand(MapsDestCommand command)
        {
            switch (command.Kind)
            {
                case MapsDestKind.Clear:
                    Interlocked.Increment(ref _generation);
                    RoutePlanSession.Clear();
                    RouteMemo.Clear();
                    RouteClearanceSession.Clear();
                    RoutePinLatch.Clear();
                    RouteSwitchListBinder.Disarm();
                    PublishRouteTelemetry(force: true, RouteTelemetryLogKind.Cleared);
                    break;
                case MapsDestKind.RejectEmpty:
                    break;
                case MapsDestKind.Set:
                    QueueCompute("set-dest");
                    break;
                case MapsDestKind.Recheck:
                    QueueCompute("recheck");
                    break;
            }
        }

        private void OnRoutePlanReady(RoutePlanReady ready)
        {
            if (ready.Generation != Volatile.Read(ref _generation))
            {
                return;
            }

            if (ready.Plan == null)
            {
                var multiLine = MapsTurntableMultiLeg.TryBindOnNoPath(_graph, ready.LogLine);
                if (SwitchListSession.HasActive && multiLine != null)
                {
                    RouteSwitchListBinder.Disarm();
                    PublishRouteTelemetry(force: true, RouteTelemetryLogKind.Change);
                    EmitLog?.Invoke(multiLine);
                    return;
                }

                RoutePlanSession.Clear();
                PublishRouteTelemetry(force: true, RouteTelemetryLogKind.Cleared);
                if (ready.LogLine != null)
                {
                    EmitLog?.Invoke(ready.LogLine);
                }

                return;
            }

            MapsTurntableMultiLeg.Disarm();

            var plan = ready.Plan;
            var exit = RouteFacingResolver.TryGetExitCue(plan, _graph);
            RoutePlanSession.SetPlan(
                plan,
                ready.OriginTrackId,
                exit ?? ready.ExitCue,
                ready.TravelEtaSeconds ?? plan.TotalCost);
            var reverse = RouteFacingResolver.IsTargetBehind(plan, _graph);
            RoutePinLatch.Observe(ready.ComputeReason, plan, reverse);
            if (RoutePinLatch.IsSetDest(ready.ComputeReason))
            {
                var latchLine = RoutePinLatch.FormatLatchLog();
                if (latchLine != null)
                {
                    EmitLog?.Invoke(latchLine);
                }
            }
            if (RouteHarvestPolicy.ShouldWriteCorridor(ready.ComputeReason))
            {
                RouteHarvestDump.WriteCorridor(
                    _graph,
                    plan,
                    RouteDestSession.YardId,
                    ready.OriginTrackId,
                    RouteDestSession.TrackId);
            }
            if (ready.JunctionSnapshot != null)
            {
                RoutePlanSession.SetJunctionSnapshot(ready.JunctionSnapshot);
            }

            if (ready.OriginTrackId != null && RouteDestSession.TrackId != null)
            {
                RouteMemo.Put(ready.OriginTrackId, RouteDestSession.TrackId, plan);
            }

            if (RouteSwitchListBinder.TryBindIfArmed(
                    plan,
                    RouteDestSession.YardId,
                    RouteDestSession.TrackId,
                    RoutePinLatch.EffectiveReverse(RouteFacingResolver.IsPinBehind(plan, _graph)),
                    RouteFacingResolver.IsDestBehind(plan, _graph),
                    out var bindLine))
            {
                PublishRouteTelemetry(force: true, RouteTelemetryLogKind.Change);
                EmitLog?.Invoke(bindLine);
                MapsDeskPanel.Instance?.ApplyRouteListStepDest("route-bind");
                if (ready.LogLine != null)
                {
                    EmitLog?.Invoke(ready.LogLine);
                }

                return;
            }

            PublishRouteTelemetry(force: true, RouteTelemetryLogKind.Change);
            if (ready.LogLine != null)
            {
                EmitLog?.Invoke(ready.LogLine);
            }
        }

        internal string TryAlignRoute()
        {
            if (!RouteAlignAccess.CanAlign(MapsDeskPanel.HasDispatcherLicense()))
            {
                var deny = "T2 align: need Dispatcher";
                EmitLog?.Invoke(deny);
                return deny;
            }

            if (!RouteDestSession.HasDestination)
            {
                var noDest = "T2 align: no destination";
                EmitLog?.Invoke(noDest);
                return noDest;
            }

            if (_graph == null || !_graph.HasFrozenPathCheck)
            {
                var wait = "T2 align: graph mapping… (retry when ready)";
                EmitLog?.Invoke(wait);
                return wait;
            }

            var plan = RoutePlanSession.Plan;
            var liveOrigin = RouteOriginProbe.TryGet();
            if (plan == null
                || plan.Status == PathCheckStatus.NoPath
                || plan.Status == PathCheckStatus.NoOrigin
                || RoutePinLatch.DisplayDismissed
                || RouteAlignOrigin.NeedsRecompute(RoutePlanSession.PlannedOriginTrackId, liveOrigin))
            {
                TryComputeSync("align", out plan, out var computeLine);
                if (computeLine != null)
                {
                    EmitLog?.Invoke(computeLine);
                }
            }

            if (plan == null || plan.Status == PathCheckStatus.NoPath || plan.Status == PathCheckStatus.NoOrigin)
            {
                var noPath = "T2 align: no path";
                EmitLog?.Invoke(noPath);
                return noPath;
            }

            var flips = PathPlan.RequiredFlips(plan);
            var pinArmed = SwitchListRunner.PinBlocksAlignOrNext(
                SwitchListSession.CurrentStep,
                RoutePinLatch.IsArmedForClearance(plan),
                RouteClearanceSession.HasPin);
            // 8.7: throw only after consist clears the latched pin frog (Transit/Pivot only).
            if (RouteClearanceGate.Align(
                    pinArmed,
                    RouteClearanceSession.Phase) == RouteClearanceGateReason.NeedCleared)
            {
                var need = RouteClearanceGate.DenyAlignLog;
                EmitLog?.Invoke(need);
                return need;
            }

            if (flips.Count == 0)
            {
                var clear = "T2 align: already clear";
                EmitLog?.Invoke(clear);
                return clear;
            }

            var thrown = 0;
            foreach (var flip in flips)
            {
                if (!_graph.TryGetJunction(flip.JunctionId, out var junction) || junction == null)
                {
                    var msg = "T2 align: abort unknown junction " + flip.JunctionId;
                    EmitLog?.Invoke(msg);
                    return msg;
                }

                var branch = flip.RequiredBranch;
                if (branch < 0 || branch > 255)
                {
                    var msg = "T2 align: abort bad branch";
                    EmitLog?.Invoke(msg);
                    return msg;
                }

                var result = ThreeGate.TryApply(
                    integrityOk: true,
                    stateRegistryOk: junction.outBranches != null,
                    safetyOk: true,
                    softWrite: () =>
                    {
                        junction.Switch(Junction.SwitchMode.REGULAR, (byte)branch);
                        return true;
                    });

                if (!result.Applied)
                {
                    var msg = "T2 align: abort " + result.AbortReason + " @ " + flip.JunctionId;
                    EmitLog?.Invoke(msg);
                    return msg;
                }

                thrown++;
            }

            RouteMemo.Clear();
            var selected = new Dictionary<string, int>(64);
            _graph.CopyJunctionSelected(selected);
            var refreshed = PathPlan.ReevaluateAlong(plan.TrackIds, _graph.PathCheckEdges, selected, _graph.ClassFor);
            RoutePlanSession.SetPlan(refreshed, RoutePlanSession.PlannedOriginTrackId, RoutePlanSession.ExitCue);
            RoutePlanSession.SetJunctionSnapshot(selected);
            PublishRouteTelemetry(force: true, RouteTelemetryLogKind.Change);

            var ok = RouteTelemetry.FormatAlign(applied: true, thrown);
            EmitLog?.Invoke(ok);
            return ok;
        }

        /// <summary>Sync compute for Switch List / TT multi-leg (**8.5**).</summary>
        internal bool TryComputeSyncPublic(string reason, out PathPlanResult? plan, out string? logLine) =>
            TryComputeSync(reason, out plan, out logLine);

        private bool TryComputeSync(string reason, out PathPlanResult? plan, out string? logLine)
        {
            plan = null;
            logLine = null;
            if (_graph == null || !_graph.HasFrozenPathCheck || !RouteDestSession.HasDestination)
            {
                return false;
            }

            var dest = RouteDestSession.TrackId!;
            var origin = RouteOriginProbe.TryGet();
            if (origin == null)
            {
                logLine = "T2 route: no origin (stand on a track, or sit in a loco/car)";
                RoutePlanSession.Clear();
                return false;
            }

            if (RouteMemo.TryGet(origin, dest, out plan) && plan != null)
            {
                ApplyMemoPlan(plan, origin, reason);
                plan = RoutePlanSession.Plan;
                return plan != null;
            }

            var selected = new Dictionary<string, int>(64);
            _graph.CopyJunctionSelected(selected);
            var destYard = PathRouteConstraints.EffectiveDestYardId(dest, RouteDestSession.YardId, PathRouteConstraints.YardIdOf);
            var filtered = PathRouteConstraints.FilterEdges(
                _graph.PathCheckEdges,
                _graph.ClassFor,
                occupied: null,
                origin,
                dest,
                PathRouteConstraints.YardIdOf,
                destYard);
            var mode = PathPlanModeSelect.ForTrip(origin, dest, RouteDestSession.YardId, PathRouteConstraints.YardIdOf);
            var result = PathPlan.Find(
                filtered,
                selected,
                origin,
                dest,
                _graph.ClassFor,
                destYardId: destYard,
                yardFor: PathRouteConstraints.YardIdOf,
                mode: mode);

            if (result.Status == PathCheckStatus.NoPath || result.Status == PathCheckStatus.NoOrigin)
            {
                logLine = "T2 route: no path (" + origin + " → " + dest + ")";
                RoutePlanSession.Clear();
                return false;
            }

            var exit = RouteFacingResolver.TryGetExitCue(result, _graph);
            RoutePlanSession.SetPlan(result, origin, exit, result.TotalCost);
            RoutePlanSession.SetJunctionSnapshot(selected);
            RouteMemo.Put(origin, dest, result);
            plan = result;
            PublishRouteTelemetry(force: true, RouteTelemetryLogKind.Change);
            logLine = "T2 route: " + reason + " " + RoutePlanDisplay.FormatPathChip(result)
                + " cost=" + result.TotalCost.ToString("0") + "s";
            return true;
        }

        private void QueueCompute(string reason)
        {
            if (_graph == null || !_graph.HasFrozenPathCheck)
            {
                EmitLog?.Invoke("T2 route: graph mapping… (retry when ready)");
                return;
            }

            if (!RouteDestSession.HasDestination)
            {
                RoutePlanSession.Clear();
                PublishRouteTelemetry(force: true, RouteTelemetryLogKind.Cleared);
                return;
            }

            var dest = RouteDestSession.TrackId!;
            var origin = RouteOriginProbe.TryGet();
            if (origin == null)
            {
                RoutePlanSession.Clear();
                EmitLog?.Invoke("T2 route: no origin (stand on a track, or sit in a loco/car)");
                return;
            }

            if (RouteMemo.TryGet(origin, dest, out var memo) && memo != null)
            {
                ApplyMemoPlan(memo, origin, reason);
                return;
            }

            var gen = Volatile.Read(ref _generation);
            var edges = _graph.PathCheckEdges;
            var selected = new Dictionary<string, int>(64);
            _graph.CopyJunctionSelected(selected);
            var destYard = PathRouteConstraints.EffectiveDestYardId(dest, RouteDestSession.YardId, PathRouteConstraints.YardIdOf);
            var filtered = PathRouteConstraints.FilterEdges(
                edges,
                _graph.ClassFor,
                occupied: null,
                origin,
                dest,
                PathRouteConstraints.YardIdOf,
                destYard);
            var mode = PathPlanModeSelect.ForTrip(origin, dest, RouteDestSession.YardId, PathRouteConstraints.YardIdOf);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (Volatile.Read(ref _generation) != gen)
                {
                    return;
                }

                var plan = PathPlan.Find(
                    filtered,
                    selected,
                    origin,
                    dest,
                    _graph.ClassFor,
                    destYardId: destYard,
                    yardFor: PathRouteConstraints.YardIdOf,
                    mode: mode);

                if (Volatile.Read(ref _generation) != gen)
                {
                    return;
                }

                string? logLine;
                PathPlanResult? sessionPlan = null;
                string? exit = null;
                if (plan.Status == PathCheckStatus.NoPath || plan.Status == PathCheckStatus.NoOrigin)
                {
                    logLine = "T2 route: no path (" + origin + " → " + dest + ")";
                }
                else
                {
                    sessionPlan = plan;
                    logLine = "T2 route: " + reason + " " + RoutePlanDisplay.FormatPathChip(plan)
                        + " cost=" + plan.TotalCost.ToString("0") + "s";
                }

                YmsEventBus.RoutePlan.Enqueue(new RoutePlanReady(
                    gen,
                    sessionPlan,
                    origin,
                    exit,
                    sessionPlan?.TotalCost,
                    selected,
                    logLine,
                    reason));
            });
        }

        private void ApplyMemoPlan(PathPlanResult plan, string origin, string reason)
        {
            var exit = RouteFacingResolver.TryGetExitCue(plan, _graph);
            RoutePlanSession.SetPlan(plan, origin, exit, plan.TotalCost);
            var selected = new Dictionary<string, int>(64);
            _graph!.CopyJunctionSelected(selected);
            RoutePlanSession.SetJunctionSnapshot(selected);
            RouteMemo.Put(origin, RouteDestSession.TrackId!, plan);
            var reverse = RouteFacingResolver.IsTargetBehind(plan, _graph);
            RoutePinLatch.Observe(reason, plan, reverse);
            if (RoutePinLatch.IsSetDest(reason))
            {
                var latchLine = RoutePinLatch.FormatLatchLog();
                if (latchLine != null)
                {
                    EmitLog?.Invoke(latchLine);
                }
            }
            if (RouteHarvestPolicy.ShouldWriteCorridor(reason))
            {
                RouteHarvestDump.WriteCorridor(
                    _graph,
                    plan,
                    RouteDestSession.YardId,
                    origin,
                    RouteDestSession.TrackId);
            }

            PublishRouteTelemetry(force: true, RouteTelemetryLogKind.Init);
            EmitLog?.Invoke(
                "T2 route: " + reason + " memo " + RoutePlanDisplay.FormatPathChip(plan));
        }

        private void PublishRouteTelemetry(bool force, RouteTelemetryLogKind kind)
        {
            var hasPlan = RoutePlanSession.HasPlan;
            var plan = RoutePlanSession.Plan;
            var status = plan?.Status ?? PathCheckStatus.NoDestination;
            var misaligned = plan?.MisalignedCount ?? 0;
            var eta = RoutePlanSession.EtaCostSeconds ?? 0f;
            var wasSeeded = _cache.Seeded;
            var wasPlan = _cache.HasPlan;
            var changed = force || RouteTelemetry.Observe(hasPlan, status, misaligned, eta, ref _cache);
            if (!changed)
            {
                return;
            }

            var facing = RouteFacingDisplay.Format(
                plan,
                RouteFacingResolver.DeskFacingNeedsReverse(plan, _graph));
            var logKind = kind == RouteTelemetryLogKind.Cleared
                ? RouteTelemetryLogKind.Cleared
                : RouteTelemetry.ResolveLogKind(wasSeeded, wasPlan, hasPlan);
            var msg = RouteTelemetry.NextLog(logKind, plan, eta, facing);
            if (msg != null)
            {
                EmitLog?.Invoke(msg);
            }
        }
    }

    /// <summary>Player origin track for Maps route (**8.2**).</summary>
    internal static class RouteOriginProbe
    {
        internal static string? TryGet()
        {
            try
            {
                return LogicTrackKey.FromCar(PlayerManager.Car)
                    ?? LogicTrackKey.FromCar(PlayerManager.LastLoco)
                    ?? LogicTrackKey.FromCar(UsableTrainProbe.TryGetTargetCar());
            }
            catch
            {
                return null;
            }
        }
    }
}
