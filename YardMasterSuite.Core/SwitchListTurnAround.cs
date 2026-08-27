using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Switch List orientation inject (**8.5** / v1 3.7): face-into-Exit → turntable before Prep;
/// reverse-into when last hop requires reverse (or job intermediate spur).
/// </summary>
public static class SwitchListTurnAround
{
    private const float MinLenSq = 1e-8f;

    /// <summary>
    /// True when loco forward points into the path Exit hop (same hemisphere, dot ≥ 0).
    /// Opposite Exit = Prep-ready (no table). Missing vectors fail closed (false).
    /// </summary>
    public static bool NeedsTurntableBeforePrep(
        float locoFwdX,
        float locoFwdZ,
        float exitDx,
        float exitDz)
    {
        var fwdLenSq = (locoFwdX * locoFwdX) + (locoFwdZ * locoFwdZ);
        var exitLenSq = (exitDx * exitDx) + (exitDz * exitDz);
        if (fwdLenSq < MinLenSq || exitLenSq < MinLenSq)
        {
            return false;
        }

        return (locoFwdX * exitDx) + (locoFwdZ * exitDz) >= 0f;
    }

    /// <summary>Stub reverse-into when the path's last hop requires reverse gear.</summary>
    public static bool NeedsReverseInto(bool lastHopRequiresReverse) => lastHopRequiresReverse;

    /// <summary>
    /// Prefer origin-yard pathable table, else dest yard, else any pathable (nearest in band).
    /// </summary>
    public static string? ResolveTurntable(
        string? originYardId,
        string? destYardId,
        IReadOnlyList<TurntableCandidate>? candidates,
        Func<string, bool>? bothLegsPathable)
    {
        if (candidates == null || candidates.Count == 0 || bothLegsPathable == null)
        {
            return null;
        }

        var origin = string.IsNullOrWhiteSpace(originYardId) ? null : originYardId!.Trim();
        var dest = string.IsNullOrWhiteSpace(destYardId) ? null : destYardId!.Trim();

        var pick = PreferYard(origin, candidates, bothLegsPathable);
        if (pick != null)
        {
            return pick;
        }

        if (dest != null
            && !string.Equals(dest, origin, StringComparison.OrdinalIgnoreCase))
        {
            pick = PreferYard(dest, candidates, bothLegsPathable);
            if (pick != null)
            {
                return pick;
            }
        }

        return PreferYard(yardFilter: null, candidates, bothLegsPathable);
    }

    private static string? PreferYard(
        string? yardFilter,
        IReadOnlyList<TurntableCandidate> candidates,
        Func<string, bool> bothLegsPathable)
    {
        string? bestId = null;
        var bestDist = float.PositiveInfinity;
        for (var i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            var id = c.TrackId?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            if (yardFilter != null
                && !string.Equals(c.YardId, yardFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!bothLegsPathable(id!))
            {
                continue;
            }

            if (c.DistanceMeters < bestDist)
            {
                bestDist = c.DistanceMeters;
                bestId = id;
            }
        }

        return bestId;
    }
}
