namespace YardMasterSuite.Core;

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
}
