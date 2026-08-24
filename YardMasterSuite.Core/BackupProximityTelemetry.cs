using System;

namespace YardMasterSuite.Core;

public struct BackupProximityCache
{
    public int CaptionKey;
    public bool Seeded;
}

/// <summary>
/// Integer caption key for Rear/Front (**6.18**). HUD/T2 fire when the key changes,
/// not every 10 Hz sample.
/// </summary>
public static class BackupProximityTelemetry
{
    public const int KeyOmit = 0;
    public const float MinChangeLogSeconds = 2f;

    /// <summary>
    /// Pack direction + tenths + couple-scan into one int. Omit when Neutral/Unknown
    /// or the travel-axis tip is coupled / missing.
    /// </summary>
    public static int CaptionKey(
        bool showChip,
        ProximityTravelDirection direction,
        float? clearanceMeters,
        bool inCoupleRange,
        bool tipActive)
    {
        if (!showChip
            || !tipActive
            || !ProximityTravelDirectionGate.ShouldShowChip(direction))
        {
            return KeyOmit;
        }

        var endCode = ProximityTravelDirectionGate.UseFrontTip(direction) ? 2 : 1;
        var m = BackupProximityDisplay.NormalizeClearance(clearanceMeters);
        if (m is null && inCoupleRange)
        {
            m = 0f;
        }

        var tenths = m is null
            ? -1
            : (int)Math.Round(m.Value * 10f, MidpointRounding.AwayFromZero);
        var couple = inCoupleRange ? 1 : 0;
        return (endCode * 100_000) + ((tenths + 1) * 10) + couple;
    }

    public static bool Observe(int captionKey, ref BackupProximityCache cache)
    {
        if (!cache.Seeded)
        {
            cache.Seeded = true;
            cache.CaptionKey = captionKey;
            return captionKey != KeyOmit;
        }

        if (cache.CaptionKey == captionKey)
        {
            return false;
        }

        cache.CaptionKey = captionKey;
        return true;
    }

    public static string? NextLog(
        int captionKey,
        ProximityTravelDirection direction,
        float? clearanceMeters,
        bool inCoupleRange,
        bool tipActive,
        float nowSeconds,
        ref float lastChangeLogAt)
    {
        if (captionKey == KeyOmit)
        {
            lastChangeLogAt = -1f;
            return "T2 proximity hide";
        }

        var isInit = lastChangeLogAt < 0f;
        if (!isInit && nowSeconds - lastChangeLogAt < MinChangeLogSeconds)
        {
            return null;
        }

        lastChangeLogAt = nowSeconds;
        var end = ProximityTravelDirectionGate.ChipLabel(direction);
        var m = BackupProximityDisplay.NormalizeClearance(clearanceMeters);
        if (m is null && inCoupleRange && tipActive)
        {
            m = 0f;
        }

        var tenths = m is null
            ? -1
            : (int)Math.Round(m.Value * 10f, MidpointRounding.AwayFromZero);
        var prefix = isInit ? "T2 proximity init: " : "T2 proximity: ";
        return prefix
            + "end="
            + end
            + " tenths="
            + tenths.ToString()
            + " couple="
            + (inCoupleRange ? "1" : "0");
    }
}
