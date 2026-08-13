namespace YardMasterSuite.Core
{
    /// <summary>Look heading as a 16-point index. Type A payload. -1 = unknown.</summary>
    public readonly struct CompassHeading
    {
        public readonly int PointIndex;

        public CompassHeading(int pointIndex)
        {
            PointIndex = pointIndex;
        }
    }

    public struct HeadingCache
    {
        public int PointIndex;
        public bool Seeded;
    }

    public enum HeadingLogKind
    {
        Init = 0,
        Change = 1,
    }

    /// <summary>
    /// Unity-free compass gate. HUD updates on every 16-point change; T2 change
    /// lines are throttled so look-around does not spam Player.log.
    /// </summary>
    public static class HeadingTelemetry
    {
        public const float MinChangeLogSeconds = 2f;

        public static bool Observe(int pointIndex, ref HeadingCache cache)
        {
            if (cache.Seeded && cache.PointIndex == pointIndex)
            {
                return false;
            }

            cache.Seeded = true;
            cache.PointIndex = pointIndex;
            return true;
        }

        public static string? NextLog(
            int pointIndex,
            HeadingLogKind kind,
            float nowSeconds,
            ref float lastChangeLogAt)
        {
            if (kind == HeadingLogKind.Change
                && nowSeconds - lastChangeLogAt < MinChangeLogSeconds)
            {
                return null;
            }

            lastChangeLogAt = nowSeconds;
            var name = HeadingDisplay.PointName(pointIndex) ?? "—";
            if (kind == HeadingLogKind.Init)
            {
                return "T2 heading init: " + name;
            }

            return "T2 heading change: " + name;
        }
    }
}
