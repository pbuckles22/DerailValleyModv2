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

    /// <summary>World-start Switch List default while <see cref="Enabled"/>.</summary>
    public const string PreferredJobId = "SW-SL-55";

    public static string FormatDisabled() => "T2 smoke-job skip: flag off";

    public static string FormatAlreadyHeld(string? jobId) =>
        "T2 smoke-job: already held job=" + (string.IsNullOrEmpty(jobId) ? "?" : jobId);

    /// <summary>Available job loaded into Switch List without TakeJob.</summary>
    public static string FormatHeld(string? jobId) =>
        "T2 smoke-job: hold job=" + (string.IsNullOrEmpty(jobId) ? "?" : jobId);

    public static string FormatTaken(string? jobId) =>
        "T2 smoke-job: taken job=" + (string.IsNullOrEmpty(jobId) ? "?" : jobId);

    public static string FormatWait() => "T2 smoke-job: wait available";

    public static string FormatWaitGraph() => "T2 smoke-job: wait graph";

    public static string FormatWaitInject() => "T2 smoke-job: wait inject TurnAround";

    /// <summary>
    /// Cab 22.27: smoke bind before frozen graph skipped TT inject (6-step list).
    /// </summary>
    public static bool ShouldDeferBindUntilGraphReady(bool graphReady) => !graphReady;

    /// <summary>
    /// Graph-not-ready SL-55 shape. Smoke hold must not bind this.
    /// </summary>
    public static bool ShouldRejectShortListWithoutTurnAround(
        bool injectedTurnAround,
        int stepCount) =>
        !injectedTurnAround && stepCount > 0 && stepCount < 10;

    public static bool ShouldBindSmokeHoldList(
        bool graphReady,
        bool injectedTurnAround,
        int stepCount) =>
        !ShouldDeferBindUntilGraphReady(graphReady)
        && !ShouldRejectShortListWithoutTurnAround(injectedTurnAround, stepCount);

    public static string FormatFail(string reason) =>
        "T2 smoke-job fail: " + reason;

    /// <summary>
    /// Desk job chip. When the dropdown and the bound Switch List disagree,
    /// print both so Pin board / GO is not run against a silent other job.
    /// </summary>
    public static string FormatDeskJobButton(string? pickerId, string? boundJobId, bool listActive)
    {
        if (string.IsNullOrEmpty(pickerId))
        {
            return "— no jobs (taken / held) —";
        }

        if (!listActive
            || string.IsNullOrEmpty(boundJobId)
            || string.Equals(pickerId, boundJobId, System.StringComparison.OrdinalIgnoreCase))
        {
            return pickerId + " ▼";
        }

        return pickerId + " ▼ list " + boundJobId;
    }

    /// <summary>
    /// Dropdown index is the source of truth. No FH / highest-SL ranking.
    /// Returns -1 when the list is empty.
    /// </summary>
    public static int ResolveSelectedIndex(int jobCount, int selectedIndex)
    {
        if (jobCount <= 0)
        {
            return -1;
        }

        if (selectedIndex < 0 || selectedIndex >= jobCount)
        {
            return 0;
        }

        return selectedIndex;
    }

    /// <summary>
    /// Cab smoke: bind SL-55 when it is on the board so save/load does not
    /// auto-paint the first Available haul (FH-82). Missing SL-55 → selected
    /// index, else first row.
    /// </summary>
    public static int IndexOfPreferredOrSelected(
        System.Collections.Generic.IReadOnlyList<string?>? jobIds,
        int selectedIndex)
    {
        var preferred = IndexOfId(jobIds, PreferredJobId);
        if (preferred >= 0)
        {
            return preferred;
        }

        return ResolveSelectedIndex(jobIds == null ? 0 : jobIds.Count, selectedIndex);
    }

    public static int IndexOfId(
        System.Collections.Generic.IReadOnlyList<string?>? jobIds,
        string? wantId)
    {
        var want = wantId?.Trim();
        if (jobIds == null || string.IsNullOrEmpty(want))
        {
            return -1;
        }

        for (var i = 0; i < jobIds.Count; i++)
        {
            var id = jobIds[i]?.Trim();
            if (!string.IsNullOrEmpty(id)
                && string.Equals(id, want, System.StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Keep the selected job id across a Refresh. If that id is gone, keep the
    /// prior index when it still lands, else first row.
    /// </summary>
    public static int IndexAfterRefresh(
        System.Collections.Generic.IReadOnlyList<string?>? newIds,
        string? keepId,
        int previousIndex)
    {
        if (newIds == null || newIds.Count <= 0)
        {
            return -1;
        }

        var byId = IndexOfId(newIds, keepId);
        if (byId >= 0)
        {
            return byId;
        }

        return ResolveSelectedIndex(newIds.Count, previousIndex);
    }
}
