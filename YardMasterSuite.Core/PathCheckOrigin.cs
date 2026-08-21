namespace YardMasterSuite.Core;

/// <summary>
/// Origin track for Path check. Live look-at/loco wins; otherwise keep the last
/// known origin so looking at the sky does not drop to <c>Path —</c> (**6.11**).
/// </summary>
public static class PathCheckOrigin
{
    public static string? Sticky(string? liveOrigin, string? lastOrigin)
    {
        var live = liveOrigin?.Trim();
        if (!string.IsNullOrEmpty(live))
        {
            return live;
        }

        var last = lastOrigin?.Trim();
        return string.IsNullOrEmpty(last) ? null : last;
    }
}
