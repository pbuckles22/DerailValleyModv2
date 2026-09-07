using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Distance-governed yard approach. Piecewise taper into PID.
/// Rem = synthesized aim (HUD / pin-to-CLEARED / TT mid + corridor), not min-of-all.
/// </summary>
public static class YardApproachKinematics
{
    public const float CruiseSpeedKmh = 25f;
    public const float IntermediateSpeedKmh = 18f;
    public const float ApproachSpeedKmh = 10f;
    public const float CreepSpeedKmh = 5f;
    public const float TouchdownSpeedKmh = 3f;

    public const float CruiseBeyondM = 60f;
    public const float IntermediateBeyondM = 30f;
    public const float ApproachBeyondM = 10f;
    public const float CreepBeyondM = 2f;

    /// <summary>Inside spur / on TT but laser not yet locked — synthetic rem.</summary>
    public const float AtDestBlindRemMeters = 15f;

    /// <summary>Half of a 25 m table — off-rail TT rem = corridor + this.</summary>
    public const float DefaultHalfTurntableMeters = 12.5f;

    /// <summary>
    /// One rem-to-aim from GO until Stop. Corridor is the far sensor; local
    /// HUD / pin-CLEARED / TT-mid take over when they exist (Gemini 4.9).
    /// </summary>
    public static float? SynthesizeRemToAim(
        SwitchListStep? step,
        float? corridorRemMeters,
        float? hudProximityMeters,
        float? pinRemToClearedMeters,
        float? ttRemToMidMeters)
    {
        if (step == null)
        {
            return null;
        }

        if (step.Kind == SwitchListStepKind.Prep
            || step.Kind == SwitchListStepKind.ReverseInto)
        {
            if (hudProximityMeters is float hud && hud >= 0f)
            {
                return hud;
            }

            if (corridorRemMeters is float corr && corr >= 0f)
            {
                var padded = corr - PrepTrackArrivalGate.AimPadMeters;
                return padded < 0f ? 0f : padded;
            }

            return null;
        }

        if (SwitchListRunner.StepNeedsPinClearance(step.Kind))
        {
            if (pinRemToClearedMeters is float pin && pin >= 0f)
            {
                return pin;
            }

            return null;
        }

        if (SwitchListDriveFacing.IsDriveToTurntable(step.Label))
        {
            if (ttRemToMidMeters is float mid && mid >= 0f)
            {
                return mid;
            }

            if (corridorRemMeters is float corr && corr >= 0f)
            {
                return corr + DefaultHalfTurntableMeters;
            }

            return null;
        }

        return NonNeg(corridorRemMeters);
    }

    /// <summary>Live sensors already latched on Core sessions.</summary>
    public static float? FromLiveSessions(SwitchListStep? step) =>
        SynthesizeRemToAim(
            step,
            RoutePlanSession.RemainingMeters,
            BackupProximitySession.ClearanceMeters ?? PrepCreepSession.TipClearanceMeters,
            RouteClearanceSession.RemToClearedMeters,
            TurntableArrivalSession.RemToMidMeters);

    /// <summary>
    /// Target speed from remaining meters to spur / cars / frog / TT.
    /// </summary>
    public static float ResolveTargetSpeedKmh(
        float distanceMeters,
        float fallbackLimitKmh = CruiseSpeedKmh)
    {
        var cap = PositiveOr(fallbackLimitKmh, CruiseSpeedKmh);
        if (float.IsNaN(distanceMeters) || float.IsInfinity(distanceMeters) || distanceMeters < 0f)
        {
            return cap;
        }

        float band;
        if (distanceMeters > CruiseBeyondM)
        {
            band = CruiseSpeedKmh;
        }
        else if (distanceMeters > IntermediateBeyondM)
        {
            band = IntermediateSpeedKmh;
        }
        else if (distanceMeters > ApproachBeyondM)
        {
            band = ApproachSpeedKmh;
        }
        else if (distanceMeters > CreepBeyondM)
        {
            band = CreepSpeedKmh;
        }
        else
        {
            band = TouchdownSpeedKmh;
        }

        return Math.Min(band, cap);
    }

    /// <summary>
    /// Meters until consist is CLEARED of frog: (frog + length) − nosePast, floored at 0.
    /// </summary>
    public static float? RemToClearedMeters(
        float? nosePastJunctionM,
        float consistLengthM,
        float frogEnvelopeM = RouteClearanceEval.DefaultFrogEnvelopeM)
    {
        if (nosePastJunctionM is not float n || float.IsNaN(n) || float.IsInfinity(n))
        {
            return null;
        }

        var frog = frogEnvelopeM > 0f ? frogEnvelopeM : RouteClearanceEval.DefaultFrogEnvelopeM;
        var len = consistLengthM > 0f ? consistLengthM : 0f;
        var rem = (frog + len) - n;
        if (float.IsNaN(rem) || float.IsInfinity(rem))
        {
            return null;
        }

        return rem < 0f ? 0f : rem;
    }

    /// <summary>Approaching frog: nosePast &lt; 0 → rem = −nosePast. At/past frog → 0.</summary>
    public static float? PinApproachRemFromNosePast(float? nosePastJunctionM)
    {
        if (nosePastJunctionM is not float n || float.IsNaN(n) || float.IsInfinity(n))
        {
            return null;
        }

        return n < 0f ? -n : 0f;
    }

    private static float? NonNeg(float? value)
    {
        if (value is not float v || float.IsNaN(v) || float.IsInfinity(v) || v < 0f)
        {
            return null;
        }

        return v;
    }

    private static float PositiveOr(float value, float fallback) =>
        float.IsNaN(value) || value <= 0f ? fallback : value;
}
