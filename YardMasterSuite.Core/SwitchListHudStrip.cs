namespace YardMasterSuite.Core;

/// <summary>
/// Read-only Switch List after desk Hide. No buttons. First line is the
/// current step; following lines are the rest (not a fixed 4–10 window).
/// </summary>
public static class SwitchListHudStrip
{
    public const int Capacity = 24;
    public const int HudMinWidthPx = 380;
    public const int HudCharPx = 9;
    public const int HudInnerPadPx = 32;
    public const string NowHeader = "Now";
    public const string RestHeader = "Rest";

    public static int OverlayWidthPx(int longestChars)
    {
        var w = (longestChars * HudCharPx) + HudInnerPadPx;
        if (w < HudMinWidthPx)
        {
            w = HudMinWidthPx;
        }

        const int max = 920;
        return w > max ? max : w;
    }

    public static bool ShowsRestSection(int lineCount) => lineCount > 1;

    /// <summary>
    /// Now/Rest sits under the Fuel/Heading ticker <b>and</b> the sticky-row
    /// icons, not on that row.
    /// </summary>
    public static float OverlayTopGuiY(float hudStackBottomGuiY)
    {
        var bottom = hudStackBottomGuiY;
        if (bottom <= MonitorHudStackLayout.Pad)
        {
            bottom = MonitorHudStackLayout.StackBottomGuiY(
                hasTrainBar: true,
                hasLocalBar: false,
                hasJobBar: false);
        }

        return ArStickyRowPlacement.BelowStickyRowGuiY(bottom);
    }

    public static bool ShouldDraw(bool deskOpen, bool hasActiveList, bool listComplete) =>
        !deskOpen && hasActiveList && !listComplete;

    public static int RemainingStart(int currentIndex, int stepCount)
    {
        if (stepCount <= 0 || currentIndex >= stepCount)
        {
            return stepCount;
        }

        return currentIndex < 0 ? 0 : currentIndex;
    }

    public static int FillRemaining(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        string[]? dest)
    {
        if (dest == null || dest.Length == 0 || steps == null || steps.Count == 0)
        {
            return 0;
        }

        var count = steps.Count;
        var start = RemainingStart(currentIndex, count);
        var n = 0;
        var cap = dest.Length < Capacity ? dest.Length : Capacity;
        for (var i = start; i < count && n < cap; i++)
        {
            dest[n] = SwitchListStepDisplay.FormatDeskLine(
                steps[i],
                i,
                count,
                isActive: i == currentIndex);
            n++;
        }

        return n;
    }
}
