namespace YardMasterSuite.Core;

/// <summary>Desk-friendly Switch List lines (**8.7** route multi-leg).</summary>
public static class SwitchListStepDisplay
{
    public static string FormatDeskLine(
        SwitchListStep step,
        int stepIndex,
        int stepCount,
        bool isActive) =>
        FormatDeskLine(step, stepIndex, stepCount, isActive, destNeedsReverse: null);

    public static bool UsesLiveDestFacing(SwitchListStepKind kind) =>
        kind == SwitchListStepKind.ReverseInto;

    /// <summary>
    /// Dest Set word follows <see cref="RouteDestFacingPolicy"/> (pin-reverse
    /// ⇒ dest ahead). Do not pass origin crow-flies <c>IsDestBehind</c>.
    /// </summary>
    public static string LiveLabel(SwitchListStep step, bool? destNeedsReverse)
    {
        if (destNeedsReverse is bool dest && UsesLiveDestFacing(step.Kind))
        {
            var action = dest ? "Reverse into" : "into";
            return SwitchListDriveFacing.FormatDriveLabel(dest, action, step.DestTrackId);
        }

        return step.Label ?? "";
    }

    public static string FormatDeskLine(
        SwitchListStep step,
        int stepIndex,
        int stepCount,
        bool isActive,
        bool? destNeedsReverse)
    {
        var mark = isActive ? "▶ " : "  ";
        var head = stepCount > 0
            ? mark + (stepIndex + 1) + "/" + stepCount + " · "
            : mark;
        return head + CompactLabel(LiveLabel(step, destNeedsReverse));
    }

    public static string CompactLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return "—";
        }

        var t = label!.Trim();
        const string until = " until CLEARED";
        if (t.Length >= until.Length
            && t.EndsWith(until, System.StringComparison.OrdinalIgnoreCase))
        {
            return t.Substring(0, t.Length - until.Length).Trim();
        }

        return t;
    }
}
