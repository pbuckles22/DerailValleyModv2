namespace YardMasterSuite.Core;

/// <summary>Change-worthy <c>T2 job-take</c> lines (**13.6.1**). Not per-frame.</summary>
public static class RemoteTakeTelemetry
{
    public const string RefuseOfficeRequired = "T2 job-take: refuse office-required";
    public const string RefuseNotOnList = "T2 job-take: refuse not-on-list";
    public const string Fail = "T2 job-take: fail";

    public static string FormatRequest(string? jobId, RemoteTakeSource source)
    {
        var sb = StringBuilderPool.Shared.Rent();
        sb.Append("T2 job-take: request job=");
        AppendJobId(sb, jobId);
        sb.Append(" src=");
        sb.Append(source == RemoteTakeSource.Go ? "go" : "desk");
        var text = sb.ToString();
        StringBuilderPool.Shared.Return(sb);
        return text;
    }

    public static string FormatTaken(string? jobId)
    {
        var sb = StringBuilderPool.Shared.Rent();
        sb.Append("T2 job-take: taken=1 job=");
        AppendJobId(sb, jobId);
        var text = sb.ToString();
        StringBuilderPool.Shared.Return(sb);
        return text;
    }

    public static string? FormatRefuse(RemoteTakeDecision decision) =>
        decision switch
        {
            RemoteTakeDecision.RefuseOfficeRequired => RefuseOfficeRequired,
            RemoteTakeDecision.RefuseNotOnList => RefuseNotOnList,
            _ => null,
        };

    private static void AppendJobId(System.Text.StringBuilder sb, string? jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            sb.Append('—');
            return;
        }

        sb.Append(jobId!.Trim());
    }
}
