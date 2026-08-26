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
        "On-consist: Numpad Enter cycles N/R/F | Numpad . TM fuse";

    /// <summary>
    /// Wagon Incremental writes are off. Rewired <c>GetButtonDown</c> chatters
    /// on look/analog and walked throttle, indy, and train brake (2.6.21.3).
    /// Cab native input still notches in the seat (Harmony rising-edge).
    /// </summary>
    public const bool ShouldWriteCabLevers = false;

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
    /// Numpad Enter is a dedicated Unity key (not cab Incremental). Allowed on
    /// any car: loco writes self, wagon writes front loco. Cab Incremental
    /// redirect stays wagon-only via <see cref="ShouldRedirectToFrontLoco"/>.
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
