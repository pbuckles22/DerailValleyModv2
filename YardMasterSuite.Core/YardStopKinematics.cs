namespace YardMasterSuite.Core;

/// <summary>
/// Shared yard rem ≤ d_stop math for TT / Prep / CLEARED-style precision stops
/// (Gemini tactical <b>13.4.x</b> — not full <b>9.2</b> mass/grade).
/// </summary>
public static class YardStopKinematics
{
    /// <summary>Hard-stop deceleration assumed for d_stop = v²/(2a).</summary>
    public const float DecelMetersPerSecSq = 2f;

    /// <summary>v²/(2a). Unknown / non-finite speed → ∞ (fail-closed: brake early).</summary>
    public static float StoppingDistanceMeters(float speedKmh)
    {
        if (float.IsNaN(speedKmh) || float.IsInfinity(speedKmh))
        {
            return float.PositiveInfinity;
        }

        var abs = speedKmh < 0f ? -speedKmh : speedKmh;
        var v = abs / 3.6f;
        return (v * v) / (2f * DecelMetersPerSecSq);
    }

    /// <summary>
    /// True when remaining meters to aim is within band, or rem ≤ d_stop at this speed.
    /// </summary>
    public static bool ShouldStartStop(float remToAimMeters, float speedKmh, float aimToleranceMeters)
    {
        if (float.IsNaN(remToAimMeters) || float.IsInfinity(remToAimMeters))
        {
            return false;
        }

        if (remToAimMeters <= aimToleranceMeters)
        {
            return true;
        }

        var dStop = StoppingDistanceMeters(speedKmh);
        return float.IsInfinity(dStop) || remToAimMeters <= dStop;
    }
}
