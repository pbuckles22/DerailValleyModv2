using System;
using System.Collections.Generic;
using DV.Interaction.Inputs;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Cab Throttle / Indy / TrainBrake / Reverser from any car on the consist
    /// go to the front loco. Fail closed off the train. Skips the front cab
    /// (native input). Numpad . turns TM fuse ON only.
    /// </summary>
    public sealed class OnConsistControlListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        internal static string? HudLabel { get; private set; }

        private readonly List<int> _locoIndexScratch = new List<int>(8);

        private OnConsistCache _cache;
        private float _throttleNextFireAt;
        private float _indyNextFireAt;
        private float _brakeNextFireAt;
        private float _reverserNextFireAt;

        private void OnEnable()
        {
            _cache = default;
            HudLabel = null;
            ResetHoldRepeat();
        }

        private void OnDisable()
        {
            HudLabel = null;
            ResetHoldRepeat();
        }

        private void Update()
        {
            Tick();
        }

        private void Tick()
        {
            string? tmLog = null;
            try
            {
                var worldActive = HudWorldSession.IsActive(PlayerManager.PlayerTransform != null);
                var standing = worldActive ? PlayerManager.Car : null;
                var playerOnCar = standing != null;
                var front = TryResolveFrontLoco(standing);
                var standingIsFront = standing != null && front != null && ReferenceEquals(standing, front);
                var redirect = OnConsistControl.ShouldRedirectToFrontLoco(playerOnCar, standingIsFront);
                var armed = worldActive && front != null && redirect;
                HudLabel = armed ? OnConsistControl.HudLegend : null;

                if (worldActive && playerOnCar && front != null && TmFuseKeyDown())
                {
                    tmLog = LocoSimReader.TryForceTmFuseOn(front);
                }

                if (!armed || front == null)
                {
                    ResetHoldRepeat();
                    LogArm(armed, tmLog);
                    return;
                }

                var player = InputManager.NewPlayer;
                if (player == null)
                {
                    ResetHoldRepeat();
                    LogArm(armed, tmLog);
                    return;
                }

                var throttleStep = ReadIncrementalStep(
                    player, InputManager.Actions.ThrottleIncremental, ref _throttleNextFireAt);
                var indyStep = ReadIncrementalStep(
                    player, InputManager.Actions.IndependentBrakeIncremental, ref _indyNextFireAt);
                var brakeStep = ReadIncrementalStep(
                    player, InputManager.Actions.BrakeIncremental, ref _brakeNextFireAt);
                var reverserStep = ReadIncrementalStep(
                    player, InputManager.Actions.ReverserIncremental, ref _reverserNextFireAt);

                var controls = front.SimController?.controlsOverrider;
                var throttle = controls?.Throttle;
                var indy = controls?.IndependentBrake;
                var brake = controls?.Brake;
                var reverser = controls?.Reverser;
                var controlsPresent =
                    throttle != null || indy != null || brake != null || reverser != null;

                if (!OnConsistControl.IsSafeToWrite(
                        worldActive,
                        playerOnCar,
                        hasFrontLoco: true,
                        controlsPresent,
                        controlNotBlocked: true))
                {
                    LogArm(armed, tmLog);
                    return;
                }

                var writeThrottle = throttle != null && throttleStep != 0;
                var writeIndy = indy != null && indyStep != 0;
                var writeBrake = brake != null && brakeStep != 0;
                var writeReverser = reverser != null && reverserStep != 0;
                if (!writeThrottle && !writeIndy && !writeBrake && !writeReverser)
                {
                    LogArm(armed, tmLog);
                    return;
                }

                if (writeThrottle)
                {
                    throttle!.Set(OnConsistControl.StepLever(
                        throttle.Value, throttleStep, throttle.IsNotched, throttle.NotchCount));
                }

                if (writeIndy)
                {
                    indy!.Set(OnConsistControl.StepLever(
                        indy.Value, indyStep, indy.IsNotched, indy.NotchCount));
                }

                if (writeBrake)
                {
                    brake!.Set(OnConsistControl.StepLever(
                        brake.Value, brakeStep, brake.IsNotched, brake.NotchCount));
                }

                if (writeReverser)
                {
                    reverser!.Set(OnConsistControl.StepReverser(reverser.Value, reverserStep));
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

        private static bool TmFuseKeyDown()
        {
            if (Input.GetKeyDown(KeyCode.KeypadPeriod) || Input.GetKeyDown(KeyCode.Period))
            {
                return true;
            }

            try
            {
                var player = InputManager.NewPlayer;
                var id = InputManager.Actions.TractionMotorFuse;
                return player != null && id >= 0 && player.GetButtonDown(id);
            }
            catch
            {
                return false;
            }
        }

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

        private void ResetHoldRepeat()
        {
            _throttleNextFireAt = 0f;
            _indyNextFireAt = 0f;
            _brakeNextFireAt = 0f;
            _reverserNextFireAt = 0f;
        }

        private static int ReadIncrementalStep(Rewired.Player player, int actionId, ref float nextFireAt)
        {
            if (actionId < 0)
            {
                nextFireAt = 0f;
                return 0;
            }

            var posHeld = player.GetButton(actionId);
            var negHeld = player.GetNegativeButton(actionId);
            if (posHeld == negHeld)
            {
                nextFireAt = 0f;
                return 0;
            }

            if (posHeld)
            {
                var fire = HoldRepeat.ShouldFire(
                    player.GetButtonDown(actionId),
                    isHeld: true,
                    (float)player.GetButtonTimePressed(actionId),
                    ref nextFireAt);
                return fire ? +1 : 0;
            }

            var negFire = HoldRepeat.ShouldFire(
                player.GetNegativeButtonDown(actionId),
                isHeld: true,
                (float)player.GetNegativeButtonTimePressed(actionId),
                ref nextFireAt);
            return negFire ? -1 : 0;
        }
    }
}
