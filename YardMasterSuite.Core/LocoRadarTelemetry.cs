namespace YardMasterSuite.Core;

/// <summary>Discrete Player.log lines for loco radar FoT scans (6.16).</summary>
public static class LocoRadarTelemetry
{
    public static string FormatScan(
        LocoRadarScanReason reason,
        string? city,
        int? leftLocoId,
        int excluded,
        int unlicensed,
        int candidates,
        int count,
        long fotMs)
    {
        var sb = StringBuilderPool.Shared.Rent();
        sb.Append("T2 loco-radar: scan reason=");
        AppendReason(sb, reason);
        sb.Append(" city=");
        if (string.IsNullOrWhiteSpace(city))
        {
            sb.Append('—');
        }
        else
        {
            sb.Append(city!.Trim());
        }

        if (leftLocoId.HasValue)
        {
            sb.Append(" left=");
            sb.Append(leftLocoId.Value);
        }

        sb.Append(" excl=");
        sb.Append(excluded < 0 ? 0 : excluded);
        sb.Append(" unlic=");
        sb.Append(unlicensed < 0 ? 0 : unlicensed);
        sb.Append(" cands=");
        sb.Append(candidates < 0 ? 0 : candidates);
        sb.Append(" n=");
        sb.Append(count < 0 ? 0 : count);
        sb.Append(" fotMs=");
        sb.Append(fotMs < 0 ? 0 : fotMs);
        var text = sb.ToString();
        StringBuilderPool.Shared.Return(sb);
        return text;
    }

    private static void AppendReason(System.Text.StringBuilder sb, LocoRadarScanReason reason)
    {
        switch (reason)
        {
            case LocoRadarScanReason.CityEntered:
                sb.Append("CityEntered");
                return;
            case LocoRadarScanReason.LeftLoco:
                sb.Append("LeftLoco");
                return;
            case LocoRadarScanReason.Forced:
                sb.Append("Forced");
                return;
            default:
                sb.Append("None");
                return;
        }
    }
}
