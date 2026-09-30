using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Meters from the lead to a car further along the frozen plan.
/// Into-hop meters are already oriented so 0 is the plan-entry end of that hop.
/// </summary>
public static class RouteCarAlongTrack
{
    /// <summary>Ignore a bogie sitting on the lead point.</summary>
    public const float MinGapMeters = 0.5f;

    /// <summary>
    /// Car around a curve is still ahead when it sits on a later hop.
    /// <paramref name="towardPlanEnd"/> false searches back toward the plan start.
    /// </summary>
    public static bool TryMetersAhead(
        float[]? hopLengths,
        int hopCount,
        int leadHop,
        float leadInto,
        int carHop,
        float carInto,
        bool towardPlanEnd,
        out float meters)
    {
        meters = 0f;
        if (hopLengths == null
            || hopCount <= 0
            || hopLengths.Length < hopCount
            || leadHop < 0
            || carHop < 0
            || leadHop >= hopCount
            || carHop >= hopCount
            || float.IsNaN(leadInto)
            || float.IsNaN(carInto)
            || float.IsInfinity(leadInto)
            || float.IsInfinity(carInto))
        {
            return false;
        }

        var lo = leadHop < carHop ? leadHop : carHop;
        var hi = leadHop > carHop ? leadHop : carHop;
        for (var i = lo; i <= hi; i++)
        {
            var len = hopLengths[i];
            if (len <= 0f || float.IsNaN(len) || float.IsInfinity(len))
            {
                return false;
            }
        }

        var leadAlong = Along(hopLengths, leadHop, leadInto);
        var carAlong = Along(hopLengths, carHop, carInto);
        var gap = towardPlanEnd ? carAlong - leadAlong : leadAlong - carAlong;
        if (gap <= MinGapMeters || float.IsNaN(gap) || float.IsInfinity(gap))
        {
            return false;
        }

        meters = gap;
        return true;
    }

    private static float Along(float[] hopLengths, int hop, float into)
    {
        var along = 0f;
        for (var i = 0; i < hop; i++)
        {
            along += hopLengths[i];
        }

        var len = hopLengths[hop];
        if (into < 0f)
        {
            into = 0f;
        }
        else if (into > len)
        {
            into = len;
        }

        return along + into;
    }
}
