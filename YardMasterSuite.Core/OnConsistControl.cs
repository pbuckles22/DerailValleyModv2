using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// While standing on any car of the active trainset, the front loco (lowest
/// trainset index) is the control target. Fail closed off-consist — not
/// off-train remote.
/// </summary>
public static class OnConsistControl
{
    public const string HudLegend =
        "On-consist: cab Throttle / Indy / TrainBrake → front loco | Numpad + cycles N/R/F | Numpad . TM fuse";

    public const float DefaultUnnotchedStep = 0.1f;
    public const float LeverRepeatSeconds = 0.15f;

    /// <summary>
    /// Wagon writes of vanilla Throttle / Indy / Train Incremental, after
    /// world-ready. Rising-edge + hold-repeat so look-chatter cannot walk
    /// all three levers (2.6.21.3).
    /// </summary>
    public const bool ShouldWriteCabLevers = true;

    /// <summary>
    /// Poll Numpad keys only when the world session is active. Querying input
    /// during bootstrap (before Rewired) poisons ControlBindings.json.
    /// </summary>
    public static bool ShouldPollInput(bool worldActive) => worldActive;

    /// <summary>
    /// Redirect only from a non-loco car. Standing on any loco (front or MU mate)
    /// keeps native cab + MU stepping — a second write double-notches (9% then 18%).
    /// </summary>
    public static bool ShouldRedirectToFrontLoco(bool playerOnCar, bool standingIsLoco) =>
        playerOnCar && !standingIsLoco;

    /// <summary>
    /// Numpad + (or Enter) is a dedicated Unity key (not cab Incremental).
    /// Allowed on any car: loco writes self, wagon writes front loco. Cab
    /// Incremental redirect stays wagon-only via <see cref="ShouldRedirectToFrontLoco"/>.
    /// </summary>
    public static bool ShouldCycleReverserFromOnConsist(bool playerOnCar, bool standingIsLoco) =>
        playerOnCar;

    /// <summary>One-key cycle: N → R → F → N (DV 0.5 / 0 / 1).</summary>
    public static float CycleReverser(float current)
    {
        var v = Clamp01(current);
        var dir = ProximityTravelDirectionGate.FromReverser(v);
        switch (dir)
        {
            case ProximityTravelDirection.Neutral:
                return 0f;
            case ProximityTravelDirection.Reverse:
                return 1f;
            case ProximityTravelDirection.Forward:
                return ProximityTravelDirectionGate.NeutralValue;
            default:
                return ProximityTravelDirectionGate.NeutralValue;
        }
    }

    public static float StepLever(
        float current,
        int direction,
        bool isNotched,
        float notchCount,
        float unnotchedStep = DefaultUnnotchedStep)
    {
        if (direction == 0)
        {
            return Clamp01(current);
        }

        var sign = direction < 0 ? -1f : 1f;
        float delta;
        if (isNotched && notchCount > 1f && !float.IsNaN(notchCount))
        {
            delta = sign / (notchCount - 1f);
        }
        else
        {
            var step = unnotchedStep > 0f && !float.IsNaN(unnotchedStep)
                ? unnotchedStep
                : DefaultUnnotchedStep;
            delta = sign * step;
        }

        return Clamp01(Clamp01(current) + delta);
    }

    /// <summary>
    /// First press notches once. Hold repeats on <see cref="LeverRepeatSeconds"/>.
    /// Chatter Down every frame while held does not walk extra notches.
    /// </summary>
    public static bool ShouldFireLeverStep(
        bool buttonDown,
        bool held,
        float now,
        ref float nextFireAt)
    {
        if (!held)
        {
            nextFireAt = 0f;
            return false;
        }

        if (nextFireAt <= 0f)
        {
            if (!buttonDown)
            {
                return false;
            }

            nextFireAt = now + LeverRepeatSeconds;
            return true;
        }

        if (now < nextFireAt)
        {
            return false;
        }

        nextFireAt = now + LeverRepeatSeconds;
        return true;
    }

    public static int? ResolveFrontLocoIndex(bool playerOnCar, IReadOnlyList<int>? locoIndices)
    {
        if (!playerOnCar || locoIndices == null || locoIndices.Count == 0)
        {
            return null;
        }

        var best = locoIndices[0];
        for (var i = 1; i < locoIndices.Count; i++)
        {
            var idx = locoIndices[i];
            if (idx < best)
            {
                best = idx;
            }
        }

        return best;
    }

    private static float Clamp01(float value)
    {
        if (float.IsNaN(value) || value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }
}
