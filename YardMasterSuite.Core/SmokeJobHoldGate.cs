namespace YardMasterSuite.Core;

/// <summary>
/// Temporary Epic 13 cab-smoke helper: put an Available job in hand on world
/// start so testing does not require a station-board walk. Flip
/// <see cref="Enabled"/> to <c>false</c> when the ritual no longer needs it.
/// </summary>
public static class SmokeJobHoldGate
{
    /// <summary>Smoke-only. Ship <c>true</c> while 13.4 cab loops; turn off later.</summary>
    public static bool Enabled = true;

    public static string FormatDisabled() => "T2 smoke-job skip: flag off";

    public static string FormatAlreadyHeld(string? jobId) =>
        "T2 smoke-job: already held job=" + (string.IsNullOrEmpty(jobId) ? "?" : jobId);

    /// <summary>Available job loaded into Switch List without TakeJob.</summary>
    public static string FormatHeld(string? jobId) =>
        "T2 smoke-job: hold job=" + (string.IsNullOrEmpty(jobId) ? "?" : jobId);

    public static string FormatTaken(string? jobId) =>
        "T2 smoke-job: taken job=" + (string.IsNullOrEmpty(jobId) ? "?" : jobId);

    public static string FormatWait() => "T2 smoke-job: wait available";

    public static string FormatFail(string reason) =>
        "T2 smoke-job fail: " + reason;

    /// <summary>
    /// Prefer SW-FH*, then any *FH*, then SW-*, else first id. Returns -1 when empty.
    /// </summary>
    public static int PickPreferredIndex(System.Collections.Generic.IReadOnlyList<string?> jobIds)
    {
        if (jobIds == null || jobIds.Count == 0)
        {
            return -1;
        }

        var swFh = -1;
        var fh = -1;
        var sw = -1;
        for (var i = 0; i < jobIds.Count; i++)
        {
            var id = jobIds[i]?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            var hasFh = id!.IndexOf("FH", System.StringComparison.OrdinalIgnoreCase) >= 0;
            var isSw = id.StartsWith("SW-", System.StringComparison.OrdinalIgnoreCase);
            if (swFh < 0 && isSw && hasFh)
            {
                swFh = i;
            }

            if (fh < 0 && hasFh)
            {
                fh = i;
            }

            if (sw < 0 && isSw)
            {
                sw = i;
            }
        }

        if (swFh >= 0)
        {
            return swFh;
        }

        if (fh >= 0)
        {
            return fh;
        }

        if (sw >= 0)
        {
            return sw;
        }

        return 0;
    }
}
