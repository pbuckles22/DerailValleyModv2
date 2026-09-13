namespace YardMasterSuite.Core;

/// <summary>
/// Read-only remaining Switch List after desk Hide. No buttons.
/// Current step through last (cab: on 4/10 show 4–10).
/// </summary>
public static class SwitchListHudStrip
{
    public const int Capacity = 16;

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
