namespace YardMasterSuite.Core;

/// <summary>
/// Preview wipe station is the job's <b>origin</b> yard (ticket office), not dest.
/// DV ids are <c>ORIGIN-DEST-N</c> (e.g. SW-SU-72). <c>chainOriginYardId</c> can be dest.
/// </summary>
public static class JobOriginYard
{
    public static string? FromJobId(string? jobId)
    {
        var id = jobId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var dash = id!.IndexOf('-');
        if (dash <= 0)
        {
            return null;
        }

        var origin = id.Substring(0, dash).Trim();
        return string.IsNullOrEmpty(origin) ? null : origin;
    }

    public static string? Resolve(string? jobId, string? chainOriginYardId)
    {
        var fromId = FromJobId(jobId);
        if (!string.IsNullOrEmpty(fromId))
        {
            return fromId;
        }

        var chain = chainOriginYardId?.Trim();
        return string.IsNullOrEmpty(chain) ? null : chain;
    }
}
