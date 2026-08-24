using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Loco gadget telemetry for the train bar (**6.5–6.7**, **6.19** Derail Risk).
    /// Mass + Grade + Load + Fluids + Motors + Derail Risk + MU publish on display-bucket change only (not 10 Hz).
    /// </summary>
    public sealed class TrainGadgetListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private TrainGadgetCache _cache;
        private float _nextAt;
        private float _lastChangeLogAt;

        private void OnEnable()
        {
            _cache = default;
            _nextAt = 0f;
            _lastChangeLogAt = -TrainGadgetTelemetry.MinChangeLogSeconds;
            PublishIfChanged();
        }

        private void OnDisable()
        {
            _cache = default;
        }

        private void LateUpdate()
        {
            if (PlayerManager.PlayerTransform == null)
            {
                return;
            }

            if (Time.unscaledTime < _nextAt)
            {
                return;
            }

            _nextAt = Time.unscaledTime + 0.1f;
            PublishIfChanged();
        }

        private void PublishIfChanged()
        {
            if (PlayerManager.PlayerTransform == null)
            {
                return;
            }

            var usable = UsableTrainProbe.HasUsableLocoTrain();
            var snap = usable ? BuildSnapshot() : default;
            var known = snap.GradePercent.HasValue || snap.MassTonnes.HasValue;
            var wasSeeded = _cache.Seeded;
            var wasKnown = _cache.Known;
            if (!TrainGadgetTelemetry.Observe(
                    known,
                    snap.GradePercent,
                    snap.MassTonnes,
                    snap.HandbrakeApplied,
                    snap.FuelPercent,
                    snap.OilPercent,
                    snap.LoadPercent,
                    snap.Motors,
                    snap.DerailRiskPercent,
                    snap.DerailLeadPercent,
                    snap.Mu,
                    ref _cache))
            {
                return;
            }

            YmsEventBus.RaiseTrainGadgetsChanged(known ? snap : default);

            var kind = ResolveLogKind(known, wasSeeded, wasKnown);
            var msg = TrainGadgetTelemetry.NextLog(
                snap.GradePercent,
                snap.MassTonnes,
                snap.FuelPercent,
                snap.OilPercent,
                snap.LoadPercent,
                snap.Motors,
                snap.DerailRiskPercent,
                snap.DerailLeadPercent,
                snap.Mu,
                kind,
                Time.unscaledTime,
                ref _lastChangeLogAt);
            if (msg != null)
            {
                EmitLog?.Invoke(msg);
            }
        }

        private static TrainGadgetLogKind ResolveLogKind(bool known, bool wasSeeded, bool wasKnown)
        {
            if (!known)
            {
                return TrainGadgetLogKind.Hide;
            }

            return !wasSeeded || !wasKnown ? TrainGadgetLogKind.Init : TrainGadgetLogKind.Change;
        }

        private static TrainGadgetSnapshot BuildSnapshot()
        {
            var loco = UsableTrainProbe.TryGetUsableLoco();
            if (loco == null || !loco.IsLoco)
            {
                return default;
            }

            try
            {
                var grade = ReadGradePercent(loco);
                var handbrakes = TryGetConsistHandbrakes(loco);
                ConsistTopologyListener.ReadConsist(loco, out _, out var kg);
                var tonnes = TonnageDisplay.KilogramsToTonnes(kg);
                LocoSimReader.ReadPower(loco, out var fuel, out var oil, out var load, out var motors);
                var derail = DerailRiskReader.ReadConsist(loco);
                var mu = ReadFreeMotion(loco);

                return new TrainGadgetSnapshot(
                    fuelPercent: fuel,
                    oilPercent: oil,
                    massTonnes: tonnes,
                    gradePercent: grade,
                    loadPercent: load,
                    motors: motors,
                    derailRiskPercent: derail.MaxPercent,
                    derailLeadPercent: derail.LeadPercent,
                    handbrakeApplied: handbrakes,
                    mu: mu);
            }
            catch
            {
                return default;
            }
        }

        private static float? ReadGradePercent(TrainCar loco)
        {
            try
            {
                var f = loco.transform.forward;
                return GradeDisplay.PercentFromDirection(f.x, f.y, f.z);
            }
            catch
            {
                return null;
            }
        }

        private static int? TryGetConsistHandbrakes(TrainCar loco)
        {
            try
            {
                var set = loco.trainset;
                if (set == null)
                {
                    return null;
                }

                var count = 0;
                foreach (var car in set.cars)
                {
                    if (car?.brakeSystem != null
                        && HandbrakeDisplay.IsApplied(car.brakeSystem.handbrakePosition))
                    {
                        count++;
                    }
                }

                return count;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Other locos vs the lead. Quiet when none/synced. Unreadable units are skipped
        /// (fail-closed — do not paint desync from a missing sim).
        /// </summary>
        private static FreeMotionSeverity ReadFreeMotion(TrainCar lead)
        {
            if (!TryReadLocoControls(lead, out var leadSnap))
            {
                return FreeMotionSeverity.None;
            }

            var cars = lead.trainset?.cars;
            if (cars == null || cars.Count <= 1)
            {
                return FreeMotionSeverity.None;
            }

            var worst = FreeMotionSeverity.None;
            for (var i = 0; i < cars.Count; i++)
            {
                var car = cars[i];
                if (car == null || !car.IsLoco || ReferenceEquals(car, lead))
                {
                    continue;
                }

                if (!TryReadLocoControls(car, out var otherSnap))
                {
                    continue;
                }

                worst = ConsistFreeMotion.Aggregate(worst, ConsistFreeMotion.CompareUnit(leadSnap, otherSnap));
                if (worst == FreeMotionSeverity.Red)
                {
                    return worst;
                }
            }

            return worst;
        }

        private static bool TryReadLocoControls(TrainCar loco, out LocoControlSnapshot snapshot)
        {
            snapshot = default;
            try
            {
                var controls = loco?.SimController?.controlsOverrider;
                if (controls == null)
                {
                    return false;
                }

                var engineOn = controls.EngineOnReader != null && controls.EngineOnReader.IsOn;
                var reverser = controls.Reverser != null
                    ? controls.Reverser.Value
                    : ConsistFreeMotion.NeutralReverser;
                var throttle = controls.Throttle != null ? controls.Throttle.Value : 0f;
                var brake = controls.Brake != null ? controls.Brake.Value : 0f;
                var indBrake = controls.IndependentBrake != null
                    ? controls.IndependentBrake.Value
                    : 0f;
                snapshot = new LocoControlSnapshot(engineOn, reverser, throttle, brake, indBrake);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
