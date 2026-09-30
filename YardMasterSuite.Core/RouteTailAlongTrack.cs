using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Along-corridor tail clearance (sawtooth pull-past). Pure hop lengths — no Unity.
/// Pin sits at the exit end of the approach hop / start of the next hop.
/// </summary>
public static class RouteTailAlongTrack
{
    /// <summary>
    /// Metres the tail is past the pin along the plan. Negative = still approaching.
    /// Unknown hop / length / lead index → false.
    /// </summary>
    public static bool TryTailPastPin(
        IReadOnlyList<string>? hopIds,
        float[]? hopLengthsMeters,
        int leadHopIndex,
        float leadIntoHopMeters,
        float consistLengthMeters,
        int approachHopIndex,
        out float tailPastPinMeters)
    {
        tailPastPinMeters = 0f;
        if (hopIds == null
            || hopLengthsMeters == null
            || hopIds.Count == 0
            || hopLengthsMeters.Length < hopIds.Count
            || leadHopIndex < 0
            || leadHopIndex >= hopIds.Count
            || approachHopIndex < 0
            || approachHopIndex >= hopIds.Count
            || consistLengthMeters <= 0f
            || float.IsNaN(consistLengthMeters)
            || float.IsInfinity(consistLengthMeters)
            || float.IsNaN(leadIntoHopMeters)
            || float.IsInfinity(leadIntoHopMeters))
        {
            return false;
        }

        var hopCount = hopIds.Count;
        for (var i = 0; i < hopCount; i++)
        {
            var len = hopLengthsMeters[i];
            if (len < 0f || float.IsNaN(len) || float.IsInfinity(len))
            {
                return false;
            }
        }

        if (!TryPointPastPin(
                hopIds,
                hopLengthsMeters,
                leadHopIndex,
                leadIntoHopMeters,
                approachHopIndex,
                out var nosePastPin))
        {
            return false;
        }

        tailPastPinMeters = nosePastPin - consistLengthMeters;
        return true;
    }

    /// <summary>
    /// Metres a bogie is past the pin. The nose bogie may already be off the plan;
    /// this reads the bogie that is still on a corridor hop.
    /// </summary>
    public static bool TryPointPastPin(
        IReadOnlyList<string>? hopIds,
        float[]? hopLengthsMeters,
        int hopIndex,
        float intoHopMeters,
        int approachHopIndex,
        out float pointPastPinMeters)
    {
        pointPastPinMeters = 0f;
        if (hopIds == null
            || hopLengthsMeters == null
            || hopIds.Count == 0
            || hopLengthsMeters.Length < hopIds.Count
            || hopIndex < 0
            || hopIndex >= hopIds.Count
            || approachHopIndex < 0
            || approachHopIndex >= hopIds.Count
            || float.IsNaN(intoHopMeters)
            || float.IsInfinity(intoHopMeters))
        {
            return false;
        }

        var hopCount = hopIds.Count;
        for (var i = 0; i < hopCount; i++)
        {
            var len = hopLengthsMeters[i];
            if (len < 0f || float.IsNaN(len) || float.IsInfinity(len))
            {
                return false;
            }
        }

        var hopLen = hopLengthsMeters[hopIndex];
        var into = intoHopMeters;
        if (into < 0f)
        {
            into = 0f;
        }
        else if (into > hopLen)
        {
            into = hopLen;
        }

        var along = into;
        for (var i = 0; i < hopIndex; i++)
        {
            along += hopLengthsMeters[i];
        }

        var pinAlong = 0f;
        for (var i = 0; i <= approachHopIndex; i++)
        {
            pinAlong += hopLengthsMeters[i];
        }

        pointPastPinMeters = along - pinAlong;
        return true;
    }

    /// <summary>
    /// One bogie sample, nose-to-tail order. <see cref="MetersBehindToTail"/> is the
    /// train still behind this bogie (full consist when this is the nose face).
    /// </summary>
    public readonly struct ConsistBogieOnPlan
    {
        public ConsistBogieOnPlan(string? trackId, float intoHopMeters, float metersBehindToTail)
        {
            TrackId = trackId;
            IntoHopMeters = intoHopMeters;
            MetersBehindToTail = metersBehindToTail;
        }

        public string? TrackId { get; }

        public float IntoHopMeters { get; }

        public float MetersBehindToTail { get; }
    }

    /// <summary>
    /// Fail closed: the bogie counts as the nose face of its car, so the metres
    /// behind it are that car plus every car behind it. Nose car (nothing ahead)
    /// returns the full consist. A bad length returns the full consist.
    /// </summary>
    public static float MetersBehindCarFace(float consistLengthMeters, float metersOfCarsAhead)
    {
        if (consistLengthMeters <= 0f
            || float.IsNaN(consistLengthMeters)
            || float.IsInfinity(consistLengthMeters)
            || metersOfCarsAhead < 0f
            || float.IsNaN(metersOfCarsAhead)
            || float.IsInfinity(metersOfCarsAhead))
        {
            return consistLengthMeters > 0f ? consistLengthMeters : 0f;
        }

        var behind = consistLengthMeters - metersOfCarsAhead;
        return behind > 0f ? behind : 0f;
    }

    /// <summary>
    /// Tail past the pin from the first bogie that still sits on a plan hop.
    /// Nose-to-tail order: a nose that is on the plan wins, same as
    /// <see cref="TryTailPastPin"/>. Lead car off the hops and a later car on
    /// them still returns a distance (cab 2.16.39 cars=3, past=?). No bogie on
    /// the hops → false. Does not invent a travel-axis clear.
    /// </summary>
    public static bool TryTailPastFromConsistBogies(
        IReadOnlyList<string>? hopIds,
        float[]? hopLengthsMeters,
        ConsistBogieOnPlan[]? bogiesNoseToTail,
        int bogieCount,
        int approachHopIndex,
        out float tailPastPinMeters,
        out string? hopId)
    {
        tailPastPinMeters = 0f;
        hopId = null;
        if (bogiesNoseToTail == null || bogieCount <= 0)
        {
            return false;
        }

        var n = bogieCount < bogiesNoseToTail.Length ? bogieCount : bogiesNoseToTail.Length;
        for (var i = 0; i < n; i++)
        {
            var bogie = bogiesNoseToTail[i];
            var hopIndex = IndexOfHop(hopIds, bogie.TrackId);
            if (hopIndex < 0)
            {
                continue;
            }

            if (!TryPointPastPin(
                    hopIds,
                    hopLengthsMeters,
                    hopIndex,
                    bogie.IntoHopMeters,
                    approachHopIndex,
                    out var pointPast))
            {
                continue;
            }

            var behind = bogie.MetersBehindToTail;
            if (behind < 0f || float.IsNaN(behind) || float.IsInfinity(behind))
            {
                continue;
            }

            tailPastPinMeters = pointPast - behind;
            hopId = hopIds![hopIndex];
            return true;
        }

        return false;
    }

    /// <summary>
    /// Front bogie wins while it is still on the plan. Cab 2.16.34.3 C4S: the nose
    /// bogie left onto a hop that is not in the plan while the other bogie was still
    /// on the stem. Both off the plan → false.
    /// </summary>
    public static bool TryOnPlanBogieHop(
        IReadOnlyList<string>? hopIds,
        string? frontTrackId,
        string? rearTrackId,
        out int hopIndex,
        out bool frontOnPlan)
    {
        hopIndex = -1;
        frontOnPlan = false;
        var front = IndexOfHop(hopIds, frontTrackId);
        if (front >= 0)
        {
            hopIndex = front;
            frontOnPlan = true;
            return true;
        }

        var rear = IndexOfHop(hopIds, rearTrackId);
        if (rear < 0)
        {
            return false;
        }

        hopIndex = rear;
        return true;
    }

    /// <summary>
    /// Off-plan tail track is not "still approaching". Holding that erases a pull-past
    /// that already reached the stem. On-plan and still before the pin exit → hold.
    /// </summary>
    public static bool ShouldHoldApproachForTail(bool tailTrackIsOnPlan, bool tailStillBeforePinExit) =>
        tailTrackIsOnPlan && tailStillBeforePinExit;

    /// <summary>
    /// Feed <see cref="RouteClearanceEval"/>: nosePast − length = tailPast.
    /// </summary>
    public static float NosePastFromTail(float tailPastPinMeters, float consistLengthMeters) =>
        tailPastPinMeters + (consistLengthMeters > 0f ? consistLengthMeters : 0f);

    /// <summary>
    /// Exit-side hops after the approach hop must hold consist + frog envelope.
    /// </summary>
    public static bool PullPastRoomFits(
        float[]? hopLengthsMeters,
        int hopCount,
        int approachHopIndex,
        float consistLengthMeters,
        float frogEnvelopeMeters = RouteClearanceEval.DefaultFrogEnvelopeM)
    {
        if (hopLengthsMeters == null
            || hopCount <= 0
            || hopLengthsMeters.Length < hopCount
            || approachHopIndex < 0
            || approachHopIndex >= hopCount
            || consistLengthMeters <= 0f)
        {
            return false;
        }

        var frog = frogEnvelopeMeters > 0f
            ? frogEnvelopeMeters
            : RouteClearanceEval.DefaultFrogEnvelopeM;
        var need = consistLengthMeters + frog;
        var room = 0f;
        for (var i = approachHopIndex + 1; i < hopCount; i++)
        {
            var len = hopLengthsMeters[i];
            if (len < 0f || float.IsNaN(len) || float.IsInfinity(len))
            {
                return false;
            }

            room += len;
            if (room >= need)
            {
                return true;
            }
        }

        return room >= need;
    }

    /// <summary>Index of <paramref name="trackId"/> in hops, or -1.</summary>
    public static int IndexOfHop(IReadOnlyList<string>? hopIds, string? trackId)
    {
        var id = trackId?.Trim();
        if (hopIds == null || string.IsNullOrEmpty(id))
        {
            return -1;
        }

        for (var i = 0; i < hopIds.Count; i++)
        {
            if (string.Equals(hopIds[i], id, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// First hop on the corridor that enters <paramref name="pinJunctionId"/>.
    /// That hop's exit end is the pin.
    /// </summary>
    public static bool TryFindApproachHopIndex(
        IReadOnlyList<string>? hopIds,
        IReadOnlyList<PathEdge>? edges,
        string? pinJunctionId,
        out int approachHopIndex)
    {
        approachHopIndex = -1;
        var pin = pinJunctionId?.Trim();
        if (hopIds == null || edges == null || string.IsNullOrEmpty(pin) || hopIds.Count < 2)
        {
            return false;
        }

        for (var i = 0; i < hopIds.Count - 1; i++)
        {
            var from = hopIds[i];
            var to = hopIds[i + 1];
            for (var e = 0; e < edges.Count; e++)
            {
                var edge = edges[e];
                if (!edge.HasJunction
                    || !string.Equals(edge.JunctionId, pin, StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(edge.FromTrackId, from, StringComparison.Ordinal)
                    && string.Equals(edge.ToTrackId, to, StringComparison.Ordinal))
                {
                    approachHopIndex = i;
                    return true;
                }
            }
        }

        return false;
    }
}
