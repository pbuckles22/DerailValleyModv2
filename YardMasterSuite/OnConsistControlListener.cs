using System;
using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Numpad + (or Enter) cycles reverser and Numpad . turns TM fuse ON from
    /// any car. Cab lever Incremental is not written (chatter walked all three
    /// levers). Fail closed off the train.
    /// </summary>
    public sealed class OnConsistControlListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        internal static string? HudLabel { get; private set; }

        private readonly List<int> _locoIndexScratch = new List<int>(8);

        private OnConsistCache _cache;
        private ThreeGateLogCache _gateLog;
        private float _reverserCycleAcceptedAt = -1f;
        private float _reverserHoldWrittenAt = -1f;
        private float _reverserHoldValue;
        private bool _reverserSawKeyUp = true;

        private void OnEnable()
        {
            _cache = default;
            _gateLog = default;
            HudLabel = null;
            ResetReverserCycle();
        }

        private void OnDisable()
        {
            HudLabel = null;
            ResetReverserCycle();
        }

        private void Update()
        {
            Tick();
        }

        private void LateUpdate()
        {
            TryHoldReverser();
        }

        private void Tick()
        {
            string? tmLog = null;
            try
            {
                var worldReady = ScreenOverlayGate.WorldReady();
                var overlay = !worldReady || ScreenOverlayGate.IsBlocking();
                var worldActive = HudWorldSession.IsActive(
                    PlayerManager.PlayerTransform != null,
                    worldReady);
                var standing = worldActive ? PlayerManager.Car : null;
                var playerOnCar = standing != null;
                var front = TryResolveFrontLoco(standing);
                var standingIsLoco = standing != null && standing.IsLoco;
                var redirect = OnConsistControl.ShouldRedirectToFrontLoco(playerOnCar, standingIsLoco);
                var armed = worldActive && front != null && redirect && !overlay;
                HudLabel = armed ? OnConsistControl.HudLegend : null;

                // Never poll keys until the world (and Rewired) is up — premature
                // input queries during bootstrap corrupt ControlBindings.json.
                if (OnConsistControl.ShouldPollInput(worldActive))
                {
                    if (YmsHotkeyPolicy.IsReverserCycleKey(
                            Input.GetKeyUp(KeyCode.KeypadEnter),
                            Input.GetKeyUp(KeyCode.KeypadPlus)))
                    {
                        _reverserSawKeyUp = true;
                    }

                    if (CycleReverserKeyDown())
                    {
                        if (OnConsistControl.ShouldCycleReverserFromOnConsist(
                                playerOnCar,
                                standingIsLoco))
                        {
                            TryCycleReverser(
                                worldActive,
                                playerOnCar,
                                standing,
                                front,
                                overlayClear: !overlay);
                        }
                    }

                    if (TmFuseKeyDown())
                    {
                        tmLog = TryWriteTmFuse(worldActive, playerOnCar, front, overlayClear: !overlay);
                    }
                }
            }
            catch
            {
                // fail closed
            }

            LogArm(HudLabel != null, tmLog);
        }

        private void LogArm(bool armed, string? tmLog)
        {
            if (tmLog != null)
            {
                EmitLog?.Invoke(tmLog);
            }

            var wasSeeded = _cache.Seeded;
            var wasArmed = _cache.Armed;
            if (!OnConsistTelemetry.Observe(armed, ref _cache))
            {
                return;
            }

            var msg = OnConsistTelemetry.NextLog(wasSeeded, wasArmed, armed);
            if (msg != null)
            {
                EmitLog?.Invoke(msg);
            }
        }

        private void TryCycleReverser(
            bool worldActive,
            bool playerOnCar,
            TrainCar? standing,
            TrainCar? front,
            bool overlayClear)
        {
            var now = Time.unscaledTime;
            if (!ReverserCyclePressGate.ShouldAcceptPress(
                    now,
                    _reverserCycleAcceptedAt,
                    sawKeyUpSinceLastAccept: _reverserSawKeyUp))
            {
                return;
            }

            var cycleTarget = standing != null && standing.IsLoco ? standing : front;
            var cycleRev = cycleTarget?.SimController?.controlsOverrider?.Reverser;
            var result = ThreeGate.TryApply(
                ThreeGateWrite.Integrity(worldActive, playerOnCar),
                ThreeGateWrite.StateRegistry(cycleRev != null),
                ThreeGateWrite.Safety(overlayClear, controlNotBlocked: true),
                () =>
                {
                    var current = cycleRev!.Value;
                    var next = OnConsistControl.CycleReverser(current);
                    if (ReverserCyclePressGate.ShouldPassThroughNeutral(current, next))
                    {
                        cycleRev.Set(ProximityTravelDirectionGate.NeutralValue);
                    }

                    cycleRev.Set(next);
                    _reverserCycleAcceptedAt = now;
                    _reverserHoldWrittenAt = now;
                    _reverserHoldValue = next;
                    _reverserSawKeyUp = false;
                    return true;
                });
            EmitGate(result, ThreeGateTelemetry.WriteReverser, logApply: true);
        }

        private string? TryWriteTmFuse(
            bool worldActive,
            bool playerOnCar,
            TrainCar? front,
            bool overlayClear)
        {
            string? tmLog = null;
            var result = ThreeGate.TryApply(
                ThreeGateWrite.Integrity(worldActive, playerOnCar),
                ThreeGateWrite.StateRegistry(front != null),
                ThreeGateWrite.Safety(overlayClear, controlNotBlocked: true),
                () =>
                {
                    tmLog = LocoSimReader.TryForceTmFuseOn(front!);
                    return TmFuseWriteOk(tmLog);
                });
            EmitGate(result, ThreeGateTelemetry.WriteTmFuse, logApply: true);
            return tmLog;
        }

        private static bool TmFuseWriteOk(string? line) =>
            line != null
            && (line.IndexOf("already ON", StringComparison.Ordinal) >= 0
                || line.EndsWith("TM fuse ON", StringComparison.Ordinal));

        private void EmitGate(ThreeGateResult result, string writeId, bool logApply)
        {
            var line = ThreeGateTelemetry.NextLog(result, writeId, logApply, ref _gateLog);
            if (line != null)
            {
                EmitLog?.Invoke(line);
            }
        }

        private static bool CycleReverserKeyDown() =>
            YmsHotkeyPolicy.IsReverserCycleKey(
                Input.GetKeyDown(KeyCode.KeypadEnter),
                Input.GetKeyDown(KeyCode.KeypadPlus));

        // Numpad only (vanilla DV leaves numpad free). No main-keyboard Period.
        private static bool TmFuseKeyDown() =>
            Input.GetKeyDown(KeyCode.KeypadPeriod);

        private TrainCar? TryResolveFrontLoco(TrainCar? standing)
        {
            if (standing == null)
            {
                return null;
            }

            _locoIndexScratch.Clear();
            List<TrainCar>? cars;
            try
            {
                cars = standing.trainset != null ? standing.trainset.cars : null;
            }
            catch
            {
                return null;
            }

            if (cars == null || cars.Count == 0)
            {
                return null;
            }

            for (var i = 0; i < cars.Count; i++)
            {
                var c = cars[i];
                if (c != null && c.IsLoco)
                {
                    _locoIndexScratch.Add(c.indexInTrainset);
                }
            }

            var frontIndex = OnConsistControl.ResolveFrontLocoIndex(playerOnCar: true, _locoIndexScratch);
            if (frontIndex is null)
            {
                return null;
            }

            for (var i = 0; i < cars.Count; i++)
            {
                var c = cars[i];
                if (c != null && c.IsLoco && c.indexInTrainset == frontIndex.Value)
                {
                    return c;
                }
            }

            return null;
        }

        private void ResetReverserCycle()
        {
            _reverserCycleAcceptedAt = -1f;
            _reverserHoldWrittenAt = -1f;
            _reverserHoldValue = ProximityTravelDirectionGate.NeutralValue;
            _reverserSawKeyUp = true;
        }

        private void TryHoldReverser()
        {
            if (!ReverserCyclePressGate.ShouldHoldWrittenValue(
                    Time.unscaledTime,
                    _reverserHoldWrittenAt))
            {
                return;
            }

            try
            {
                var worldReady = ScreenOverlayGate.WorldReady();
                if (!HudWorldSession.IsActive(PlayerManager.PlayerTransform != null, worldReady))
                {
                    return;
                }

                var overlayClear = worldReady && !ScreenOverlayGate.IsBlocking();
                var standing = PlayerManager.Car;
                var target = standing != null && standing.IsLoco ? standing : TryResolveFrontLoco(standing);
                var rev = target?.SimController?.controlsOverrider?.Reverser;
                var result = ThreeGate.TryApply(
                    ThreeGateWrite.Integrity(worldActive: true, actorPresent: standing != null),
                    ThreeGateWrite.StateRegistry(rev != null),
                    ThreeGateWrite.Safety(overlayClear, controlNotBlocked: true),
                    () =>
                    {
                        rev!.Set(_reverserHoldValue);
                        return true;
                    });
                EmitGate(result, ThreeGateTelemetry.WriteReverser, logApply: false);
            }
            catch
            {
                // fail closed
            }
        }

    }
}
