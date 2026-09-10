using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Shared yard rem ≤ d_stop math for TT / Prep / CLEARED-style precision stops
/// (Gemini tactical <b>13.4.x</b> — not full <b>9.2</b> grade). Mass scales a
/// like Next-limit; 40 t floor keeps solo-loco goldens.
/// </summary>
public static class YardStopKinematics
{
    /// <summary>Hard-stop deceleration assumed for d_stop = v²/(2a) at reference mass.</summary>
    public const float DecelMetersPerSecSq = 2f;

    /// <summary>Solo DE6-class floor — same 40 t as <see cref="NextLimitReveal"/>.</summary>
    public const float ReferenceMassTonnes = 40f;

    public const float MaxMassTonnes = 400f;

    /// <summary>v²/(2a). Unknown / non-finite speed → ∞ (fail-closed: brake early).</summary>
    public static float StoppingDistanceMeters(
        float speedKmh,
        float massTonnes = ReferenceMassTonnes)
    {
        if (float.IsNaN(speedKmh) || float.IsInfinity(speedKmh))
        {
            return float.PositiveInfinity;
        }

        var abs = speedKmh < 0f ? -speedKmh : speedKmh;
        var v = abs / 3.6f;
        var a = DecelForMass(massTonnes);
        return (v * v) / (2f * a);
    }

    /// <summary>
    /// True when remaining meters to aim is within band, or rem ≤ d_stop at this speed.
    /// </summary>
    public static bool ShouldStartStop(
        float remToAimMeters,
        float speedKmh,
        float aimToleranceMeters,
        float massTonnes = ReferenceMassTonnes)
    {
        if (float.IsNaN(remToAimMeters) || float.IsInfinity(remToAimMeters))
        {
            return false;
        }

        if (remToAimMeters <= aimToleranceMeters)
        {
            return true;
        }

        var dStop = StoppingDistanceMeters(speedKmh, massTonnes);
        return float.IsInfinity(dStop) || remToAimMeters <= dStop;
    }

    internal static float DecelForMass(float massTonnes)
    {
        var mass = massTonnes > 0f && !float.IsNaN(massTonnes) && !float.IsInfinity(massTonnes)
            ? massTonnes
            : ReferenceMassTonnes;
        var clamped = Math.Max(ReferenceMassTonnes, Math.Min(mass, MaxMassTonnes));
        return DecelMetersPerSecSq * (ReferenceMassTonnes / clamped);
    }
}
