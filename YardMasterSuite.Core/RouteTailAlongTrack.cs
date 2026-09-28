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

        var leadLen = hopLengthsMeters[leadHopIndex];
        var into = leadIntoHopMeters;
        if (into < 0f)
        {
            into = 0f;
        }
        else if (into > leadLen)
        {
            into = leadLen;
        }

        var noseAlong = into;
        for (var i = 0; i < leadHopIndex; i++)
        {
            noseAlong += hopLengthsMeters[i];
        }

        // Pin = exit of approach hop (= start of approachHopIndex + 1).
        var pinAlong = 0f;
        for (var i = 0; i <= approachHopIndex; i++)
        {
            pinAlong += hopLengthsMeters[i];
        }

        var tailAlong = noseAlong - consistLengthMeters;
        tailPastPinMeters = tailAlong - pinAlong;
        return true;
    }

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
