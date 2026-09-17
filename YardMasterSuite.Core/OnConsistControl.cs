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
        "Wagon: Numpad + N/R/F | 8/2 throttle | 5 idle | 7/1 indy | 9/3 train | . TM fuse";

    /// <summary>
    /// Wagon Incremental writes are off. Rewired <c>GetButtonDown</c> chatters
    /// on look/analog and walked throttle, indy, and train brake (2.6.21.3).
    /// Cab native input still notches in the seat (Harmony rising-edge).
    /// </summary>
    public const bool ShouldWriteCabLevers = false;

    /// <summary>
    /// Unity Keypad 8/2/5 write throttle from a <b>wagon</b> only. In the
    /// seat, Rewired already owns those KeyCodes (22.21 cab: Indy Open +
    /// throttle notch on the same Numpad 8).
    /// </summary>
    public static bool ShouldWriteThrottleFromOnConsist(bool playerOnCar, bool standingIsLoco) =>
        ShouldWriteOnConsistHotkeys(playerOnCar, standingIsLoco);

    public static bool ShouldWriteTmFuseFromOnConsist(bool playerOnCar, bool standingIsLoco) =>
        ShouldWriteOnConsistHotkeys(playerOnCar, standingIsLoco);

    public static bool ShouldWriteBrakesFromOnConsist(bool playerOnCar, bool standingIsLoco) =>
        ShouldWriteOnConsistHotkeys(playerOnCar, standingIsLoco);

    /// <summary>
    /// Wagon-only Unity KeyCode writes. Native cab Rewired must be the only
    /// consumer while <paramref name="standingIsLoco"/>.
    /// </summary>
    public static bool ShouldWriteOnConsistHotkeys(bool playerOnCar, bool standingIsLoco) =>
        playerOnCar && !standingIsLoco;

    public static bool ShouldShowHud(bool playerOnCar, bool hasFrontLoco) =>
        playerOnCar && hasFrontLoco;

    public static float NotchThrottleUp(float current)
    {
        var n = (int)System.Math.Round(
            PidSpeedNotch.Snap(current) / PidSpeedNotch.Step,
            System.MidpointRounding.AwayFromZero);
        return PidSpeedNotch.FromNotch(n + 1);
    }

    public static float NotchThrottleDown(float current)
    {
        var n = (int)System.Math.Round(
            PidSpeedNotch.Snap(current) / PidSpeedNotch.Step,
            System.MidpointRounding.AwayFromZero);
        return PidSpeedNotch.FromNotch(n - 1);
    }

    public static float IdleThrottle() => 0f;

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
    /// Numpad + / Enter cycle reverser from a <b>wagon</b> only. Same
    /// Rewired collision as throttle if Indy is bound to + or Enter.
    /// </summary>
    public static bool ShouldCycleReverserFromOnConsist(bool playerOnCar, bool standingIsLoco) =>
        ShouldWriteOnConsistHotkeys(playerOnCar, standingIsLoco);

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
