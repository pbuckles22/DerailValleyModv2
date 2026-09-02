using System;

namespace YardMasterSuite.Core;

/// <summary>Last emitted job-car pin identity (count + label hash).</summary>
public struct JobCarPinLogCache
{
    public bool Seeded;
    public int Count;
    public int Hash;
}

/// <summary>Discrete Player.log lines for job-car AR (6.21). Not per frame.</summary>
public static class JobCarTelemetry
{
    public static string FormatScan(string? jobId, bool jobTaken, int count)
    {
        var sb = StringBuilderPool.Shared.Rent();
        sb.Append("T2 job-car-ar: scan job=");
        if (string.IsNullOrWhiteSpace(jobId))
        {
            sb.Append('—');
        }
        else
        {
            sb.Append(jobId!.Trim());
        }

        sb.Append(" taken=");
        sb.Append(jobTaken ? 1 : 0);
        sb.Append(" n=");
        sb.Append(count < 0 ? 0 : count);
        var text = sb.ToString();
        StringBuilderPool.Shared.Return(sb);
        return text;
    }

    public static string FormatClear() => "T2 job-car-ar: clear (no job in hand)";

    public static string FormatHide(string? jobId)
    {
        var sb = StringBuilderPool.Shared.Rent();
        sb.Append("T2 job-car-ar: hide job=");
        if (string.IsNullOrWhiteSpace(jobId))
        {
            sb.Append('—');
        }
        else
        {
            sb.Append(jobId!.Trim());
        }

        sb.Append(" reason=ready");
        var text = sb.ToString();
        StringBuilderPool.Shared.Return(sb);
        return text;
    }

    /// <summary>
    /// <b>13.1.15</b> pin identity: <c>T2 job-car-ar: n=K ids=…</c> on count/id change.
    /// </summary>
    public static string FormatPins(int n, string?[]? labels)
    {
        var count = n < 0 ? 0 : n;
        var sb = StringBuilderPool.Shared.Rent();
        sb.Append("T2 job-car-ar: n=");
        sb.Append(count);
        sb.Append(" ids=");
        if (count == 0 || labels == null || labels.Length == 0)
        {
            sb.Append('—');
        }
        else
        {
            var cap = count < labels.Length ? count : labels.Length;
            for (var i = 0; i < cap; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                var id = labels[i];
                sb.Append(string.IsNullOrWhiteSpace(id) ? "—" : id!.Trim());
            }
        }

        var text = sb.ToString();
        StringBuilderPool.Shared.Return(sb);
        return text;
    }

    public static string? NextPins(int n, string?[]? labels, ref JobCarPinLogCache cache)
    {
        var count = n < 0 ? 0 : n;
        var hash = PinsHash(count, labels);
        if (cache.Seeded && cache.Count == count && cache.Hash == hash)
        {
            return null;
        }

        cache.Seeded = true;
        cache.Count = count;
        cache.Hash = hash;
        return FormatPins(count, labels);
    }

    public static int PinsHash(int n, string?[]? labels)
    {
        var h = n * 397;
        if (labels == null)
        {
            return h;
        }

        var cap = n < labels.Length ? n : labels.Length;
        for (var i = 0; i < cap; i++)
        {
            var s = labels[i];
            h = (h * 31) + (s == null ? 0 : StringComparer.Ordinal.GetHashCode(s));
        }

        return h;
    }
}
