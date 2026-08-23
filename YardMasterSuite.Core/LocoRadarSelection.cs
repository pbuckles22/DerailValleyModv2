using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>One spawned loco candidate for radar ranking (6.16). Pure — no Unity.</summary>
public readonly struct LocoRadarCandidate
{
    public LocoRadarCandidate(int id, float distanceSq)
    {
        Id = id;
        DistanceSq = distanceSq;
    }

    public int Id { get; }
    public float DistanceSq { get; }
}

/// <summary>
/// Rank nearest other locomotives for AR radar (6.16 / v1 4.10).
/// Callers supply distance² and exclusion ids (self / my-loco AR target / same consist).
/// Top-N insert uses stack locals (N ≤ 3) — no heap on the rank path.
/// </summary>
public static class LocoRadarSelection
{
    public const int DefaultMaxResults = 3;

    /// <summary>Yard-walk useful range — farther markers are noise.</summary>
    public const float MaxRangeMeters = 600f;

    public static float MaxRangeDistanceSq => MaxRangeMeters * MaxRangeMeters;

    /// <summary>
    /// Writes nearest-first ids into <paramref name="rankedIds"/> (up to its length and
    /// <paramref name="maxResults"/>, capped at <see cref="DefaultMaxResults"/>).
    /// Skips ids in <paramref name="excludeIds"/> and candidates beyond
    /// <see cref="MaxRangeMeters"/>.
    /// </summary>
    public static int RankNearest(
        LocoRadarCandidate[] candidates,
        ICollection<int>? excludeIds,
        int maxResults,
        int[] rankedIds,
        int candidateCount = -1)
    {
        if (rankedIds == null || rankedIds.Length == 0 || maxResults <= 0
            || candidates == null || candidates.Length == 0)
        {
            return 0;
        }

        var limit = candidateCount < 0 ? candidates.Length : candidateCount;
        if (limit > candidates.Length)
        {
            limit = candidates.Length;
        }

        if (limit <= 0)
        {
            return 0;
        }

        var cap = maxResults;
        if (cap > DefaultMaxResults)
        {
            cap = DefaultMaxResults;
        }

        if (cap > rankedIds.Length)
        {
            cap = rankedIds.Length;
        }

        var maxSq = MaxRangeDistanceSq;
        var top = new TopN(cap);
        for (var i = 0; i < limit; i++)
        {
            var c = candidates[i];
            if (excludeIds != null && excludeIds.Contains(c.Id))
            {
                continue;
            }

            if (c.DistanceSq > maxSq || float.IsNaN(c.DistanceSq) || c.DistanceSq < 0f)
            {
                continue;
            }

            top.Insert(c.Id, c.DistanceSq);
        }

        return top.CopyTo(rankedIds);
    }

    private struct TopN
    {
        private readonly int _cap;
        private int _filled;
        private int _id0;
        private int _id1;
        private int _id2;
        private float _d0;
        private float _d1;
        private float _d2;

        public TopN(int cap)
        {
            _cap = cap;
            _filled = 0;
            _id0 = 0;
            _id1 = 0;
            _id2 = 0;
            _d0 = 0f;
            _d1 = 0f;
            _d2 = 0f;
        }

        public void Insert(int id, float distanceSq)
        {
            if (_filled < _cap)
            {
                Set(_filled, id, distanceSq);
                _filled++;
                BubbleLeft(_filled - 1);
                return;
            }

            if (distanceSq >= Dist(_filled - 1))
            {
                return;
            }

            Set(_filled - 1, id, distanceSq);
            BubbleLeft(_filled - 1);
        }

        public int CopyTo(int[] dest)
        {
            if (_filled > 0)
            {
                dest[0] = _id0;
            }

            if (_filled > 1)
            {
                dest[1] = _id1;
            }

            if (_filled > 2)
            {
                dest[2] = _id2;
            }

            return _filled;
        }

        private void BubbleLeft(int index)
        {
            var i = index;
            while (i > 0 && Dist(i) < Dist(i - 1))
            {
                Swap(i, i - 1);
                i--;
            }
        }

        private float Dist(int i)
        {
            switch (i)
            {
                case 0:
                    return _d0;
                case 1:
                    return _d1;
                default:
                    return _d2;
            }
        }

        private void Set(int i, int id, float d)
        {
            switch (i)
            {
                case 0:
                    _id0 = id;
                    _d0 = d;
                    return;
                case 1:
                    _id1 = id;
                    _d1 = d;
                    return;
                default:
                    _id2 = id;
                    _d2 = d;
                    return;
            }
        }

        private void Swap(int a, int b)
        {
            var idA = Id(a);
            var dA = Dist(a);
            Set(a, Id(b), Dist(b));
            Set(b, idA, dA);
        }

        private int Id(int i)
        {
            switch (i)
            {
                case 0:
                    return _id0;
                case 1:
                    return _id1;
                default:
                    return _id2;
            }
        }
    }
}
