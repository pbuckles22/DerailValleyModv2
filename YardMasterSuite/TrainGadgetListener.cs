using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Loco gadget telemetry for the train bar (**6.5–6.7**).
    /// Mass + Grade publish on display-bucket change only (not 10 Hz).
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
                    ref _cache))
            {
                return;
            }

            YmsEventBus.RaiseTrainGadgetsChanged(known ? snap : default);

            var kind = ResolveLogKind(known, wasSeeded, wasKnown);
            var msg = TrainGadgetTelemetry.NextLog(
                snap.GradePercent,
                snap.MassTonnes,
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
            var loco = PlayerManager.Car;
            if (loco == null || !loco.IsLoco)
            {
                loco = PlayerManager.LastLoco;
            }

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

                return new TrainGadgetSnapshot(
                    gradePercent: grade,
                    massTonnes: tonnes,
                    handbrakeApplied: handbrakes);
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
    }
}
