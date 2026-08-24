using System;

namespace YardMasterSuite.Core;

public enum JobBarKind
{
    Hidden = 0,
    Taken = 1,
    Prep = 2,
    Cancelled = 3,
}

/// <summary>
/// Discrete Player.log lines for Tier 2 Active Job HUD (**6.13** / **6.20**).
/// Logs on appear / hide / id / GO-HOLD / bonus-minute / preview bucket / license —
/// not every second or meter.
/// </summary>
public readonly struct ActiveJobDebugSnapshot
{
    public const int BonusUnknown = int.MinValue;
    public const int PreviewNone = int.MinValue;
    public const int PreviewOut = -1;

    public ActiveJobDebugSnapshot(
        bool visible,
        string? jobId,
        int extraCount,
        string? statusChip,
        int bonusMinute,
        JobBarKind kind = JobBarKind.Taken,
        int previewBucket = PreviewNone,
        string? licenseCodes = null,
        string? originYard = null)
    {
        Visible = visible;
        JobId = jobId;
        ExtraCount = extraCount;
        StatusChip = statusChip;
        BonusMinute = bonusMinute;
        Kind = visible ? kind : JobBarKind.Hidden;
        PreviewBucket = previewBucket;
        LicenseCodes = licenseCodes;
        OriginYard = originYard;
    }

    public bool Visible { get; }
    public string? JobId { get; }
    public int ExtraCount { get; }
    public string? StatusChip { get; }
    public int BonusMinute { get; }
    public JobBarKind Kind { get; }
    public int PreviewBucket { get; }
    public string? LicenseCodes { get; }
    public string? OriginYard { get; }

    public string FormatFragment()
    {
        if (!Visible)
        {
            return "— Job";
        }

        if (Kind == JobBarKind.Prep)
        {
            return "preview=" + FormatPreviewBucket(PreviewBucket)
                + " license=" + (string.IsNullOrEmpty(LicenseCodes) ? "—" : LicenseCodes)
                + " yard=" + (string.IsNullOrEmpty(OriginYard) ? "—" : OriginYard);
        }

        var id = string.IsNullOrEmpty(JobId) ? "—" : JobId;
        var status = string.IsNullOrEmpty(StatusChip) ? "—" : StatusChip;
        var bonus = BonusMinute == BonusUnknown ? "—" : BonusMinute.ToString();
        return "job=" + id + " extra=" + ExtraCount + " status=" + status + " bonus=" + bonus;
    }

    public static string FormatPreviewBucket(int bucket)
    {
        if (bucket == PreviewNone)
        {
            return "—";
        }

        if (bucket == PreviewOut)
        {
            return "OUT";
        }

        return bucket.ToString();
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
    public int Kind;
    public int PreviewBucket;
    public string LicenseCodes;
    public string OriginYard;
}

/// <summary>
/// Unity-free job-bar gate. HUD may tick bonus seconds and preview meters;
/// T2 is init / appear / hide / id / status / bonus-minute / preview-10 m / license.
/// </summary>
public static class ActiveJobTelemetry
{
    public static bool Observe(
        bool visible,
        string? jobId,
        int extraCount,
        JobConsistStatus status,
        float? remainingSeconds,
        ref ActiveJobCache cache) =>
        ObserveCore(
            visible ? JobBarKind.Taken : JobBarKind.Hidden,
            visible,
            jobId,
            extraCount,
            status,
            remainingSeconds,
            previewMeters: null,
            licenseCodes: null,
            originYard: null,
            ref cache);

    public static bool ObservePrep(
        float? previewMeters,
        string? licenseCodes,
        ref ActiveJobCache cache) =>
        ObservePrep(previewMeters, licenseCodes, originYard: null, ref cache);

    public static bool ObservePrep(
        float? previewMeters,
        string? licenseCodes,
        string? originYard,
        ref ActiveJobCache cache) =>
        ObserveCore(
            JobBarKind.Prep,
            visible: true,
            jobId: null,
            extraCount: 0,
            JobConsistStatus.Missing,
            remainingSeconds: null,
            previewMeters,
            licenseCodes,
            originYard,
            ref cache);

    public static bool ObserveCancelled(string? jobId, ref ActiveJobCache cache) =>
        ObserveCore(
            JobBarKind.Cancelled,
            visible: true,
            jobId,
            extraCount: 0,
            JobConsistStatus.Missing,
            remainingSeconds: null,
            previewMeters: null,
            licenseCodes: null,
            originYard: null,
            ref cache);

    private static bool ObserveCore(
        JobBarKind kind,
        bool visible,
        string? jobId,
        int extraCount,
        JobConsistStatus status,
        float? remainingSeconds,
        float? previewMeters,
        string? licenseCodes,
        string? originYard,
        ref ActiveJobCache cache)
    {
        if (!visible)
        {
            kind = JobBarKind.Hidden;
            jobId = string.Empty;
            extraCount = 0;
            status = JobConsistStatus.Missing;
            remainingSeconds = null;
            previewMeters = null;
            licenseCodes = string.Empty;
            originYard = string.Empty;
        }
        else
        {
            jobId = jobId ?? string.Empty;
            if (extraCount < 0)
            {
                extraCount = 0;
            }

            licenseCodes = licenseCodes ?? string.Empty;
            originYard = originYard ?? string.Empty;
        }

        var minute = BonusMinuteBucket(remainingSeconds);
        var statusInt = kind == JobBarKind.Cancelled ? -1 : (int)status;
        var preview = PreviewMeterBucket(previewMeters);

        if (!cache.Seeded)
        {
            cache.Seeded = true;
            Write(
                ref cache,
                visible,
                jobId,
                extraCount,
                statusInt,
                minute,
                (int)kind,
                preview,
                licenseCodes,
                originYard);
            return true;
        }

        if (cache.Visible == visible
            && cache.Kind == (int)kind
            && string.Equals(cache.JobId, jobId, StringComparison.Ordinal)
            && cache.ExtraCount == extraCount
            && cache.Status == statusInt
            && cache.BonusMinute == minute
            && cache.PreviewBucket == preview
            && string.Equals(cache.LicenseCodes, licenseCodes, StringComparison.Ordinal)
            && string.Equals(cache.OriginYard, originYard, StringComparison.Ordinal))
        {
            return false;
        }

        Write(
            ref cache,
            visible,
            jobId,
            extraCount,
            statusInt,
            minute,
            (int)kind,
            preview,
            licenseCodes,
            originYard);
        return true;
    }

    private static void Write(
        ref ActiveJobCache cache,
        bool visible,
        string jobId,
        int extraCount,
        int statusInt,
        int minute,
        int kind,
        int preview,
        string licenseCodes,
        string originYard)
    {
        cache.Visible = visible;
        cache.JobId = jobId;
        cache.ExtraCount = extraCount;
        cache.Status = statusInt;
        cache.BonusMinute = minute;
        cache.Kind = kind;
        cache.PreviewBucket = preview;
        cache.LicenseCodes = licenseCodes;
        cache.OriginYard = originYard;
    }

    public static ActiveJobDebugSnapshot Snapshot(ref ActiveJobCache cache)
    {
        if (!cache.Visible)
        {
            return new ActiveJobDebugSnapshot(
                false, null, 0, null, ActiveJobDebugSnapshot.BonusUnknown, JobBarKind.Hidden);
        }

        var kind = (JobBarKind)cache.Kind;
        if (kind == JobBarKind.Prep)
        {
            return new ActiveJobDebugSnapshot(
                true,
                null,
                0,
                null,
                ActiveJobDebugSnapshot.BonusUnknown,
                JobBarKind.Prep,
                cache.PreviewBucket,
                string.IsNullOrEmpty(cache.LicenseCodes) ? null : cache.LicenseCodes,
                string.IsNullOrEmpty(cache.OriginYard) ? null : cache.OriginYard);
        }

        if (kind == JobBarKind.Cancelled)
        {
            return new ActiveJobDebugSnapshot(
                true,
                string.IsNullOrEmpty(cache.JobId) ? null : cache.JobId,
                0,
                "Cancelled",
                ActiveJobDebugSnapshot.BonusUnknown,
                JobBarKind.Cancelled);
        }

        return new ActiveJobDebugSnapshot(
            true,
            string.IsNullOrEmpty(cache.JobId) ? null : cache.JobId,
            cache.ExtraCount,
            JobConsistStatusDisplay.Format((JobConsistStatus)cache.Status),
            cache.BonusMinute,
            JobBarKind.Taken);
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
            && prior.Kind == current.Kind
            && string.Equals(prior.JobId, current.JobId, StringComparison.Ordinal)
            && prior.ExtraCount == current.ExtraCount
            && string.Equals(prior.StatusChip, current.StatusChip, StringComparison.Ordinal)
            && prior.BonusMinute == current.BonusMinute
            && prior.PreviewBucket == current.PreviewBucket
            && string.Equals(prior.LicenseCodes, current.LicenseCodes, StringComparison.Ordinal)
            && string.Equals(prior.OriginYard, current.OriginYard, StringComparison.Ordinal))
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

    public static int PreviewMeterBucket(float? metersRemaining)
    {
        if (metersRemaining is null)
        {
            return ActiveJobDebugSnapshot.PreviewNone;
        }

        if (metersRemaining.Value < 0f)
        {
            return ActiveJobDebugSnapshot.PreviewOut;
        }

        return (int)Math.Floor(metersRemaining.Value / 10.0) * 10;
    }
}
