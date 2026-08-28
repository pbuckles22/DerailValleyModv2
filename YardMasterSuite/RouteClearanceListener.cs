using System;
using System.Collections.Generic;
using DV;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Golden <c>2.8.7.2</c> pin + reverse virtual nose: when Set Reverse,
    /// sample the butt (rear trainset car) as the leading edge.
    /// </summary>
    public sealed class RouteClearanceListener : MonoBehaviour
    {
        private const float PollSeconds = 0.35f;

        internal static Action<string>? EmitLog;

        private readonly float[] _lengthScratch = new float[64];
        private PathGraphMapper? _graph;
        private RouteClearanceTelemetryCache _log;
        private float _nextPoll;
        private RouteClearancePhase _phase = RouteClearancePhase.Idle;

        private void OnEnable()
        {
            _graph = GetComponent<PathGraphMapper>();
            _log = default;
            _phase = RouteClearancePhase.Idle;
            _nextPoll = 0f;
            RouteClearanceSession.Clear();
            RoutePinLatch.Clear();
            YmsEventBus.OnMapsDestCommand += OnMapsDestCommand;
        }

        private void OnDisable()
        {
            YmsEventBus.OnMapsDestCommand -= OnMapsDestCommand;
            RouteClearanceSession.Clear();
            RoutePinLatch.Clear();
            _phase = RouteClearancePhase.Idle;
            _log = default;
        }

        private void OnMapsDestCommand(MapsDestCommand command)
        {
            if (command.Kind != MapsDestKind.Clear)
            {
                return;
            }

            _phase = RouteClearancePhase.Idle;
            RouteClearanceSession.Clear();
            RoutePinLatch.Clear();
            var line = RouteClearanceTelemetry.Observe(RouteClearancePhase.Idle, null, ref _log);
            if (line != null)
            {
                EmitLog?.Invoke(line);
            }
        }

        private void Update()
        {
            if (!WorldSessionGate.IsActive())
            {
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
                EvaluateOnce();
            }
            catch (Exception ex)
            {
                EmitLog?.Invoke("T2 route-pin: eval " + ex.GetType().Name);
            }
        }

        private void EvaluateOnce()
        {
            var plan = RoutePlanSession.Plan;
            if (plan == null
                || plan.Status == PathCheckStatus.NoPath
                || plan.Status == PathCheckStatus.NoOrigin
                || !RouteDestSession.HasDestination)
            {
                ApplyIdle();
                return;
            }

            var pinId = RoutePinLatch.EffectivePin(plan);
            if (string.IsNullOrEmpty(pinId)
                || _graph == null
                || !_graph.TryGetJunction(pinId!, out var junction)
                || junction == null)
            {
                ApplyIdle();
                return;
            }

            if (!TryMeasure(plan, junction, out var nosePastM, out var lengthM, out var pinX, out var pinY, out var pinZ))
            {
                Commit(
                    RouteClearanceEval.Evaluate(
                        _phase,
                        new RouteClearanceSample(
                            hasPin: true,
                            nosePastJunctionM: 0f,
                            consistLengthM: 0f,
                            frogEnvelopeM: RouteClearanceEval.DefaultFrogEnvelopeM,
                            approachWindowM: RouteClearanceEval.DefaultApproachWindowM)),
                    pinId,
                    pinX,
                    pinY,
                    pinZ);
                return;
            }

            var sample = new RouteClearanceSample(
                hasPin: true,
                nosePastJunctionM: nosePastM,
                consistLengthM: lengthM,
                frogEnvelopeM: RouteClearanceEval.DefaultFrogEnvelopeM,
                approachWindowM: RouteClearanceEval.DefaultApproachWindowM);
            Commit(RouteClearanceEval.Evaluate(_phase, in sample), pinId, pinX, pinY, pinZ);
        }

        private void ApplyIdle()
        {
            Commit(
                RouteClearanceEval.Evaluate(
                    _phase,
                    new RouteClearanceSample(
                        hasPin: false,
                        nosePastJunctionM: 0f,
                        consistLengthM: 0f,
                        frogEnvelopeM: RouteClearanceEval.DefaultFrogEnvelopeM,
                        approachWindowM: RouteClearanceEval.DefaultApproachWindowM)),
                pinJunctionId: null,
                pinX: 0f,
                pinY: 0f,
                pinZ: 0f);
        }

        private void Commit(
            in RouteClearanceDecision decision,
            string? pinJunctionId,
            float pinX,
            float pinY,
            float pinZ)
        {
            _phase = decision.Phase;
            RouteClearanceSession.Apply(in decision, pinJunctionId, pinX, pinY, pinZ);
            var line = RouteClearanceTelemetry.Observe(decision.Phase, decision.Caption, ref _log);
            if (line != null)
            {
                EmitLog?.Invoke(line);
            }
        }

        private bool TryMeasure(
            PathPlanResult plan,
            Junction junction,
            out float nosePastM,
            out float lengthM,
            out float pinX,
            out float pinY,
            out float pinZ)
        {
            nosePastM = 0f;
            lengthM = 0f;
            pinX = pinY = pinZ = 0f;

            if (!JunctionPinWorld.TryGet(junction, out pinX, out pinY, out pinZ))
            {
                return false;
            }

            if (!TryResolveConsist(out var cars, out var solo))
            {
                return false;
            }

            var reverse = RoutePinLatch.HasLatch
                ? RoutePinLatch.TravelUsesReverse
                : RouteFacingResolver.IsTargetBehind(plan, _graph);

            var multi = cars != null && cars.Count > 1;
            var lead = PickLeadCar(cars, solo, travelReverse: reverse && multi);
            if (lead == null)
            {
                return false;
            }

            lengthM = cars != null && cars.Count > 0
                ? MeasureLength(cars)
                : MeasureSingle(lead);

            if (lengthM <= 0f)
            {
                return false;
            }

            Vector3 nose;
            Vector3 fwd;
            try
            {
                var t = lead.transform;
                nose = t.position;
                fwd = t.forward;
            }
            catch
            {
                return false;
            }

            fwd.y = 0f;
            var mag = Mathf.Sqrt((fwd.x * fwd.x) + (fwd.z * fwd.z));
            if (mag < 1e-4f)
            {
                return false;
            }

            fwd.x /= mag;
            fwd.z /= mag;

            var goldenNosePast = ((nose.x - pinX) * fwd.x) + ((nose.z - pinZ) * fwd.z);
            nosePastM = reverse && multi
                ? RouteClearanceTravel.LeadingEdgePastM(goldenNosePast, travelReverse: true)
                : RouteClearanceTravel.TravelPastJunctionM(goldenNosePast, lengthM, reverse);
            return true;
        }

        private static float MeasureSingle(TrainCar car) => ReadCarLength(car);

        private static float ReadCarLength(TrainCar car)
        {
            try
            {
                var len = car.InterCouplerDistance;
                if (len > 0f)
                {
                    return len;
                }
            }
            catch
            {
                // fall through
            }

            try
            {
                return car.Bounds.size.z;
            }
            catch
            {
                return 0f;
            }
        }

        private float MeasureLength(IList<TrainCar> cars)
        {
            var n = 0;
            for (var i = 0; i < cars.Count && n < _lengthScratch.Length; i++)
            {
                var car = cars[i];
                if (car == null)
                {
                    continue;
                }

                _lengthScratch[n++] = ReadCarLength(car);
            }

            if (n == 0)
            {
                return 0f;
            }

            return ConsistLengthMeters.Sum(_lengthScratch, n);
        }

        private static bool TryResolveConsist(out IList<TrainCar>? cars, out TrainCar? solo)
        {
            cars = null;
            solo = null;
            try
            {
                var car = PlayerManager.Car ?? PlayerManager.LastLoco;
                if (car == null)
                {
                    return false;
                }

                cars = car.trainset != null ? car.trainset.cars : null;
                if (cars == null || cars.Count == 0)
                {
                    solo = car;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static TrainCar? PickLeadCar(IList<TrainCar>? cars, TrainCar? solo, bool travelReverse)
        {
            if (cars == null || cars.Count == 0)
            {
                return solo;
            }

            var min = int.MaxValue;
            var max = int.MinValue;
            TrainCar? minCar = null;
            TrainCar? maxCar = null;
            for (var i = 0; i < cars.Count; i++)
            {
                var c = cars[i];
                if (c == null)
                {
                    continue;
                }

                var idx = c.indexInTrainset;
                if (idx < min)
                {
                    min = idx;
                    minCar = c;
                }

                if (idx > max)
                {
                    max = idx;
                    maxCar = c;
                }
            }

            if (minCar == null)
            {
                return solo;
            }

            var want = ConsistTravelLead.LeadingIndex(min, max, travelReverse);
            return want == max ? maxCar : minCar;
        }
    }
}
