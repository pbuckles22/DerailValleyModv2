using System;

namespace YardMasterSuite.Core;

/// <summary>Why loco turn / re-rail place was refused (**8.6**).</summary>
public enum LocoRerailAbort
{
    None = 0,
    NoLoco,
    NoType,
    NoMatch,
    Moving,
    BusyTeleporting,
    NoTarget,
    Coupled,
    Derailed,
}

/// <summary>
/// Pure fail-closed policy for loco-only turn-in-place and re-rail place (**8.6**).
/// Job cars / consist teleport are out of scope.
/// Turn uses <c>TrainCar.MoveToTrack</c> at the same world position with reversed forward
/// (nose↔toes). <c>Rerail</c> is derail-recovery only (no-op on rails). TeleportTrainset is
/// Bring-only — it seeks clear space and must not Turn.
/// </summary>
public static class LocoRerailPolicy
{
    public const float MaxAbsSpeedKmh = 0.5f;

    public static LocoRerailAbort EvaluateTurn(
        bool hasLoco,
        int consistCarCount,
        float? maxAbsSpeedKmh,
        bool isTeleporting,
        bool isDerailed)
    {
        if (!hasLoco)
        {
            return LocoRerailAbort.NoLoco;
        }

        if (consistCarCount != 1)
        {
            return LocoRerailAbort.Coupled;
        }

        if (isDerailed)
        {
            return LocoRerailAbort.Derailed;
        }

        if (isTeleporting)
        {
            return LocoRerailAbort.BusyTeleporting;
        }

        if (maxAbsSpeedKmh is float speed && speed > MaxAbsSpeedKmh)
        {
            return LocoRerailAbort.Moving;
        }

        return LocoRerailAbort.None;
    }

    public static LocoRerailAbort EvaluatePlace(
        bool hasTypeSelected,
        int matchCount,
        float? selectedAbsSpeedKmh,
        bool isTeleporting,
        bool hasTargetTrack,
        bool selectedCoupled,
        bool selectedDerailed)
    {
        if (!hasTypeSelected)
        {
            return LocoRerailAbort.NoType;
        }

        if (matchCount <= 0)
        {
            return LocoRerailAbort.NoMatch;
        }

        if (selectedCoupled)
        {
            return LocoRerailAbort.Coupled;
        }

        // TeleportTrainset aborts when any car is derailed — require on-rails source.
        if (selectedDerailed)
        {
            return LocoRerailAbort.Derailed;
        }

        if (isTeleporting)
        {
            return LocoRerailAbort.BusyTeleporting;
        }

        if (selectedAbsSpeedKmh is float speed && speed > MaxAbsSpeedKmh)
        {
            return LocoRerailAbort.Moving;
        }

        if (!hasTargetTrack)
        {
            return LocoRerailAbort.NoTarget;
        }

        return LocoRerailAbort.None;
    }

    public static bool CanApply(LocoRerailAbort abort) => abort == LocoRerailAbort.None;

    public static string FormatAbort(LocoRerailAbort abort) => abort switch
    {
        LocoRerailAbort.None => "OK",
        LocoRerailAbort.NoLoco => "no loco",
        LocoRerailAbort.NoType => "pick loco type",
        LocoRerailAbort.NoMatch => "none of that type",
        LocoRerailAbort.Moving => "loco moving",
        LocoRerailAbort.BusyTeleporting => "teleport busy",
        LocoRerailAbort.NoTarget => "look at a track",
        LocoRerailAbort.Coupled => "uncouple first",
        LocoRerailAbort.Derailed => "need on-rails loco",
        _ => "blocked",
    };

    public static string FormatPlaceChip(
        bool placeActive,
        string? typeLabel,
        string? trackId,
        LocoRerailAbort abort)
    {
        if (!placeActive)
        {
            return string.Empty;
        }

        if (abort != LocoRerailAbort.None)
        {
            return "PLACE BLOCKED · " + FormatAbort(abort);
        }

        var type = string.IsNullOrWhiteSpace(typeLabel) ? "—" : typeLabel!.Trim();
        var track = string.IsNullOrWhiteSpace(trackId) ? "—" : trackId!.Trim();
        return "PLACE OK · " + type + " · " + track;
    }

    /// <summary>
    /// Prefer matching uncoupled on-rails loco outside player yard, then farthest.
    /// Returns -1 when none eligible.
    /// </summary>
    public static int SelectSourceIndex(
        int count,
        Func<int, bool> typeMatches,
        Func<int, bool> isCoupled,
        Func<int, bool> isDerailed,
        Func<int, bool> sameYardAsPlayer,
        Func<int, float> distanceSqFromPlayer)
    {
        if (count <= 0 || typeMatches is null || isCoupled is null || isDerailed is null
            || sameYardAsPlayer is null || distanceSqFromPlayer is null)
        {
            return -1;
        }

        var best = -1;
        var bestOnRails = false;
        var bestOutside = false;
        var bestDist = -1f;

        for (var i = 0; i < count; i++)
        {
            if (!typeMatches(i) || isCoupled(i))
            {
                continue;
            }

            var onRails = !isDerailed(i);
            var outside = !sameYardAsPlayer(i);
            var dist = distanceSqFromPlayer(i);
            if (best < 0
                || (onRails && !bestOnRails)
                || (onRails == bestOnRails && outside && !bestOutside)
                || (onRails == bestOnRails && outside == bestOutside && dist > bestDist))
            {
                best = i;
                bestOnRails = onRails;
                bestOutside = outside;
                bestDist = dist;
            }
        }

        // TeleportTrainset cannot move derailed cars — refuse derailed-only picks.
        if (best >= 0 && isDerailed(best))
        {
            return -1;
        }

        return best;
    }
}
