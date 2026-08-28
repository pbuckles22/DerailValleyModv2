namespace YardMasterSuite.Core;

/// <summary>Sum of measured car lengths (Unity: InterCouplerDistance). No DE2 hardcode.</summary>
public static class ConsistLengthMeters
{
    public static float Sum(float[]? carLengthsMeters) =>
        Sum(carLengthsMeters, carLengthsMeters == null ? 0 : carLengthsMeters.Length);

    public static float Sum(float[]? carLengthsMeters, int count)
    {
        if (carLengthsMeters == null || count <= 0)
        {
            return 0f;
        }

        var n = count < carLengthsMeters.Length ? count : carLengthsMeters.Length;
        var total = 0f;
        for (var i = 0; i < n; i++)
        {
            var L = carLengthsMeters[i];
            if (L > 0f && !float.IsNaN(L) && !float.IsInfinity(L))
            {
                total += L;
            }
        }

        return total;
    }
}
