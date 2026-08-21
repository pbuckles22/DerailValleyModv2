using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 Active Job HUD (**6.13** / v1 4.8).
/// Logs on appear / hide / id / GO-HOLD / bonus-minute — not every second.
/// </summary>
public readonly struct ActiveJobDebugSnapshot
{
    public const int BonusUnknown = int.MinValue;

    public ActiveJobDebugSnapshot(
        bool visible,
        string? jobId,
        int extraCount,
        string? statusChip,
        int bonusMinute)
    {
        Visible = visible;
        JobId = jobId;
        ExtraCount = extraCount;
        StatusChip = statusChip;
        BonusMinute = bonusMinute;
    }

    public bool Visible { get; }
    public string? JobId { get; }
    public int ExtraCount { get; }
    public string? StatusChip { get; }
    public int BonusMinute { get; }

    public string FormatFragment()
    {
        if (!Visible)
        {
            return "— Job";
        }

        var id = string.IsNullOrEmpty(JobId) ? "—" : JobId;
        var status = string.IsNullOrEmpty(StatusChip) ? "—" : StatusChip;
        var bonus = BonusMinute == BonusUnknown ? "—" : BonusMinute.ToString();
        return "job=" + id + " extra=" + ExtraCount + " status=" + status + " bonus=" + bonus;
    }
}

public struct ActiveJobCache
{
    public bool Seeded;
    public bool Visible;
    public string JobId;
    public int ExtraCount;
    public int Status;
    public int BonusMinute;
}

/// <summary>
/// Unity-free job-bar gate. HUD may tick bonus seconds; T2 is init / appear /
/// hide / id / status / bonus-minute.
/// </summary>
public static class ActiveJobTelemetry
{
    public static bool Observe(
        bool visible,
        string? jobId,
        int extraCount,
        JobConsistStatus status,
        float? remainingSeconds,
        ref ActiveJobCache cache)
    {
        if (!visible)
        {
            jobId = string.Empty;
            extraCount = 0;
            status = JobConsistStatus.Missing;
        }
        else
        {
            jobId = jobId ?? string.Empty;
            if (extraCount < 0)
            {
                extraCount = 0;
            }
        }

        var minute = BonusMinuteBucket(remainingSeconds);
        var statusInt = (int)status;

        if (!cache.Seeded)
        {
            cache.Seeded = true;
            cache.Visible = visible;
            cache.JobId = jobId;
            cache.ExtraCount = extraCount;
            cache.Status = statusInt;
            cache.BonusMinute = minute;
            return true;
        }

        if (cache.Visible == visible
            && string.Equals(cache.JobId, jobId, StringComparison.Ordinal)
            && cache.ExtraCount == extraCount
            && cache.Status == statusInt
            && cache.BonusMinute == minute)
        {
            return false;
        }

        cache.Visible = visible;
        cache.JobId = jobId;
        cache.ExtraCount = extraCount;
        cache.Status = statusInt;
        cache.BonusMinute = minute;
        return true;
    }

    public static ActiveJobDebugSnapshot Snapshot(ref ActiveJobCache cache)
    {
        if (!cache.Visible)
        {
            return new ActiveJobDebugSnapshot(false, null, 0, null, ActiveJobDebugSnapshot.BonusUnknown);
        }

        return new ActiveJobDebugSnapshot(
            true,
            string.IsNullOrEmpty(cache.JobId) ? null : cache.JobId,
            cache.ExtraCount,
            JobConsistStatusDisplay.Format((JobConsistStatus)cache.Status),
            cache.BonusMinute);
    }

    public static string? NextLog(ActiveJobDebugSnapshot? previous, ActiveJobDebugSnapshot current)
    {
        if (previous is null)
        {
            return current.Visible
                ? "T2 job init: " + current.FormatFragment()
                : "T2 job init (hidden)";
        }

        var prior = previous.Value;
        if (!prior.Visible && current.Visible)
        {
            return "T2 job appear: " + current.FormatFragment();
        }

        if (prior.Visible && !current.Visible)
        {
            return "T2 job hide";
        }

        if (prior.Visible == current.Visible
            && string.Equals(prior.JobId, current.JobId, StringComparison.Ordinal)
            && prior.ExtraCount == current.ExtraCount
            && string.Equals(prior.StatusChip, current.StatusChip, StringComparison.Ordinal)
            && prior.BonusMinute == current.BonusMinute)
        {
            return null;
        }

        return current.Visible ? "T2 job change: " + current.FormatFragment() : "T2 job hide";
    }

    public static int BonusMinuteBucket(float? remainingSeconds)
    {
        if (remainingSeconds is null)
        {
            return ActiveJobDebugSnapshot.BonusUnknown;
        }

        if (remainingSeconds.Value <= 0f)
        {
            return 0;
        }

        return (int)Math.Floor(remainingSeconds.Value / 60.0);
    }
}
