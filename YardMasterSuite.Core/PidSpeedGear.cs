using System;

namespace YardMasterSuite.Core;

/// <summary>
/// <b>9.1</b> gear for the active Maps / Switch List step. Throttle only after
/// the reverser matches that step (never power in Neutral). Smoke: Neutral
/// loco, step 1 Set Reverse, Next → Set Forward.
/// </summary>
public static class PidSpeedGear
{
    public const float ReverseValue = 0f;
    public const float ForwardValue = 1f;

    public static bool LabelNeedsReverse(string? stepLabel)
    {
        if (string.IsNullOrEmpty(stepLabel))
        {
            return false;
        }

        return stepLabel!.StartsWith(SwitchListDriveFacing.Reverse, StringComparison.Ordinal);
    }

    /// <summary>
    /// Switch List current-step label wins. Dest-only (no list) uses the 8.7
    /// pin reverse latch. Missing both → Forward.
    /// </summary>
    public static bool LegNeedsReverse(string? currentStepLabel, bool destOnlyPinReverse)
    {
        if (!string.IsNullOrEmpty(currentStepLabel))
        {
            return LabelNeedsReverse(currentStepLabel);
        }

        return destOnlyPinReverse;
    }

    public static float TargetReverser(bool needsReverse) =>
        needsReverse ? ReverseValue : ForwardValue;

    public static bool Matches(float reverser, bool needsReverse)
    {
        var dir = ProximityTravelDirectionGate.FromReverser(reverser);
        if (dir is ProximityTravelDirection.Neutral or ProximityTravelDirection.Unknown)
        {
            return false;
        }

        return needsReverse
            ? dir == ProximityTravelDirection.Reverse
            : dir == ProximityTravelDirection.Forward;
    }
}
