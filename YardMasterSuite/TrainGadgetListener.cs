using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Loco gadget telemetry for the train bar (**6.5–6.7**).
    /// </summary>
    public sealed class TrainGadgetListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private int? _lastHandbrakes;
        private float _nextAt;

        private void OnEnable()
        {
            _lastHandbrakes = null;
            _nextAt = 0f;
            Publish(force: true);
        }

        private void LateUpdate()
        {
            if (PlayerManager.PlayerTransform == null || !UsableTrainProbe.HasUsableLocoTrain())
            {
                return;
            }

            if (Time.unscaledTime < _nextAt)
            {
                return;
            }

            _nextAt = Time.unscaledTime + 0.1f;
            Publish(force: false);
        }

        private void Publish(bool force)
        {
            var snap = BuildSnapshot();
            if (!force && snap.HandbrakeApplied == _lastHandbrakes)
            {
                return;
            }

            _lastHandbrakes = snap.HandbrakeApplied;
            YmsEventBus.RaiseTrainGadgetsChanged(snap);
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
