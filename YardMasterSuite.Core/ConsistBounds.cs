using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Head + tail placement along a corridor (Gemini Step 5 ConsistBounds).
/// Walks backward from the nose by consist length — pure hop meters, no Unity.
/// </summary>
public readonly struct ConsistBounds
{
    public ConsistBounds(
        string headTrackId,
        int headHopIndex,
        float headIntoHopMeters,
        string tailTrackId,
        int tailHopIndex,
        float tailIntoHopMeters,
        float consistLengthMeters)
    {
        HeadTrackId = headTrackId ?? string.Empty;
        HeadHopIndex = headHopIndex;
        HeadIntoHopMeters = headIntoHopMeters;
        TailTrackId = tailTrackId ?? string.Empty;
        TailHopIndex = tailHopIndex;
        TailIntoHopMeters = tailIntoHopMeters;
        ConsistLengthMeters = consistLengthMeters;
    }

    public string HeadTrackId { get; }
    public int HeadHopIndex { get; }
    public float HeadIntoHopMeters { get; }
    public string TailTrackId { get; }
    public int TailHopIndex { get; }
    public float TailIntoHopMeters { get; }
    public float ConsistLengthMeters { get; }

    /// <summary>
    /// Locate head and tail on <paramref name="hopIds"/> / <paramref name="hopLengthsMeters"/>.
    /// Tail = nose along-corridor minus consist length (clamped to corridor start).
    /// </summary>
    public static bool TryCompute(
        IReadOnlyList<string>? hopIds,
        float[]? hopLengthsMeters,
        int headHopIndex,
        float headIntoHopMeters,
        float consistLengthMeters,
        out ConsistBounds bounds)
    {
        bounds = default;
        if (hopIds == null
            || hopLengthsMeters == null
            || hopIds.Count == 0
            || hopLengthsMeters.Length < hopIds.Count
            || headHopIndex < 0
            || headHopIndex >= hopIds.Count
            || consistLengthMeters <= 0f
            || float.IsNaN(consistLengthMeters)
            || float.IsInfinity(consistLengthMeters)
            || float.IsNaN(headIntoHopMeters)
            || float.IsInfinity(headIntoHopMeters))
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

        var headLen = hopLengthsMeters[headHopIndex];
        var into = headIntoHopMeters;
        if (into < 0f)
        {
            into = 0f;
        }
        else if (into > headLen)
        {
            into = headLen;
        }

        var noseAlong = into;
        for (var i = 0; i < headHopIndex; i++)
        {
            noseAlong += hopLengthsMeters[i];
        }

        var tailAlong = noseAlong - consistLengthMeters;
        if (tailAlong < 0f)
        {
            tailAlong = 0f;
        }

        if (!TryLocateAlong(hopIds, hopLengthsMeters, hopCount, noseAlong, out var headIdx, out var headInto)
            || !TryLocateAlong(hopIds, hopLengthsMeters, hopCount, tailAlong, out var tailIdx, out var tailInto))
        {
            return false;
        }

        bounds = new ConsistBounds(
            hopIds[headIdx],
            headIdx,
            headInto,
            hopIds[tailIdx],
            tailIdx,
            tailInto,
            consistLengthMeters);
        return true;
    }

    private static bool TryLocateAlong(
        IReadOnlyList<string> hopIds,
        float[] hopLengthsMeters,
        int hopCount,
        float alongMeters,
        out int hopIndex,
        out float intoHopMeters)
    {
        hopIndex = 0;
        intoHopMeters = 0f;
        var remaining = alongMeters;
        for (var i = 0; i < hopCount; i++)
        {
            var len = hopLengthsMeters[i];
            if (i == hopCount - 1 || remaining <= len)
            {
                hopIndex = i;
                intoHopMeters = remaining <= len ? remaining : len;
                return hopIds[i] != null;
            }

            remaining -= len;
        }

        return false;
    }
}
