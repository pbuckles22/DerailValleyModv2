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
        UsesLiveDriveFacing(kind);

    public static bool UsesLiveDriveFacing(SwitchListStepKind kind) =>
        kind is SwitchListStepKind.TurnAround
            or SwitchListStepKind.Prep
            or SwitchListStepKind.Transit
            or SwitchListStepKind.Pivot
            or SwitchListStepKind.ReverseInto
            or SwitchListStepKind.Delivery;

    /// <summary>Live cab Set word for the active Switch List leg.</summary>
    public static bool? ResolveDriveNeedsReverse(
        SwitchListStep step,
        RouteClearancePhase clearancePhase,
        bool planPinArmed,
        bool sessionHasPin,
        bool pinLatched,
        bool pinTravelReverse,
        bool pinBehindLive,
        bool destBehindLive)
    {
        if (!UsesLiveDriveFacing(step.Kind))
        {
            return null;
        }

        if (step.BindNeedsReverse is bool bind)
        {
            // HTP: frog approach bind holds until CLEARED, then opposite
            // (Forward past #4 → Reverse Prep). Do not freeze Set Forward.
            if (SwitchListPinFacing.IsClearedFrogPin(step)
                && clearancePhase == RouteClearancePhase.Cleared)
            {
                return SwitchListPinFacing.NeedsReverseAtPin(bind);
            }

            return bind;
        }

        var pinLeg = SwitchListRunner.StepUsesApproachPinFacing(step.Kind)
            && (planPinArmed || sessionHasPin);

        return RouteFacingPhasePolicy.FacingNeedsReverse(
            clearancePhase,
            pinLeg,
            pinLatched,
            pinTravelReverse,
            pinBehindLive,
            destBehindLive);
    }

    /// <summary>
    /// Dest Set word follows <see cref="RouteDestFacingPolicy"/> (pin-reverse
    /// ⇒ dest ahead). Do not pass origin crow-flies <c>IsDestBehind</c>.
    /// </summary>
    public static string LiveLabel(SwitchListStep step, bool? driveNeedsReverse) =>
        LiveLabel(step, driveNeedsReverse, showPassPin: true);

    public static string LiveLabel(SwitchListStep step, bool? driveNeedsReverse, bool showPassPin)
    {
        if (driveNeedsReverse is not bool needsReverse)
        {
            return step.Label ?? "";
        }

        switch (step.Kind)
        {
            case SwitchListStepKind.TurnAround:
                if (SwitchListDriveFacing.IsDriveToTurntable(step.Label))
                {
                    return SwitchListDriveFacing.FormatDriveLabel(
                        needsReverse,
                        SwitchListDriveFacing.ToTurntableAction,
                        step.DestTrackId);
                }

                return SwitchListDriveFacing.FormatTurnAroundLabel(needsReverse);
            case SwitchListStepKind.Prep:
                if (SwitchListWarehouseLegs.IsLoaderSpot(step.Label))
                {
                    return SwitchListDriveFacing.FormatDriveLabel(
                        needsReverse,
                        SwitchListWarehouseLegs.IntoLoaderAction,
                        step.DestTrackId);
                }

                return SwitchListDriveFacing.FormatDriveLabel(needsReverse, "Prep", step.DestTrackId);
            case SwitchListStepKind.Transit:
                return FormatTransitLabel(step, needsReverse, showPassPin);
            case SwitchListStepKind.Pivot:
                return FormatPivotLabel(step, needsReverse, showPassPin);
            case SwitchListStepKind.ReverseInto:
                var action = needsReverse ? "Reverse into" : "into";
                return SwitchListDriveFacing.FormatDriveLabel(needsReverse, action, step.DestTrackId);
            case SwitchListStepKind.Delivery:
                return SwitchListDriveFacing.FormatDriveLabel(needsReverse, "Delivery", step.DestTrackId);
            default:
                return step.Label ?? "";
        }
    }

    private static string FormatTransitLabel(SwitchListStep step, bool needsReverse, bool showPassPin)
    {
        var label = step.Label ?? "";
        if (label.IndexOf("Past switch", System.StringComparison.Ordinal) >= 0)
        {
            if (!showPassPin)
            {
                return SwitchListDriveFacing.FormatDriveLabel(needsReverse, "to", step.DestTrackId);
            }

            return SwitchListDriveFacing.FormatDriveLabel(needsReverse, "Past switch", step.DestTrackId)
                + " until CLEARED";
        }

        return SwitchListDriveFacing.FormatDriveLabel(needsReverse, "Transit", step.DestTrackId);
    }

    private static string FormatPivotLabel(SwitchListStep step, bool needsReverse, bool showPassPin)
    {
        var label = step.Label ?? "";
        if (label.IndexOf("until CLEARED", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (!showPassPin)
            {
                return SwitchListDriveFacing.FormatDriveLabel(needsReverse, "to", step.DestTrackId);
            }

            return SwitchListDriveFacing.FormatDriveLabel(needsReverse, "Pivot", step.DestTrackId)
                + " until CLEARED";
        }

        return SwitchListDriveFacing.FormatDriveLabel(needsReverse, "Pivot", step.DestTrackId);
    }

    public static string FormatDeskLine(
        SwitchListStep step,
        int stepIndex,
        int stepCount,
        bool isActive,
        bool? destNeedsReverse) =>
        FormatDeskLine(step, stepIndex, stepCount, isActive, destNeedsReverse, atTrack: false);

    public static string FormatDeskLine(
        SwitchListStep step,
        int stepIndex,
        int stepCount,
        bool isActive,
        bool? destNeedsReverse,
        bool atTrack)
    {
        var mark = isActive ? "▶ " : "  ";
        var head = stepCount > 0
            ? mark + (stepIndex + 1) + "/" + stepCount + " · "
            : mark;
        var facing = destNeedsReverse ?? step.BindNeedsReverse;
        var line = head + CompactLabel(LiveLabel(step, facing));
        return atTrack && isActive ? line + " · at track" : line;
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

    public const int DeskLinePx = 20;
    public const int MinDeskWidthPx = 420;
    public const int DeskCharPx = 9;
    public const int DeskInnerPadPx = 56;
    public const int SwitchListTitlePx = 26;
    public const int SwitchListTabsPx = 28;
    public const int SwitchListLicensePx = 22;
    public const int SwitchListJobRowPx = 28;
    public const int SwitchListLoadRowPx = 30;
    public const int SwitchListAlignRowPx = 30;
    public const int SwitchListCruiseRowPx = 30;
    public const int SwitchListJobIdPx = 22;
    public const int SwitchListPathPx = 22;
    public const int SwitchListCoachPx = 38;
    public const int SwitchListHidePx = 26;
    public const int SwitchListBottomPadPx = 8;

    public static int LongestLineChars(
        System.Collections.Generic.IReadOnlyList<string>? lines,
        int usedCount = -1)
    {
        if (lines == null)
        {
            return 0;
        }

        var limit = usedCount < 0 || usedCount > lines.Count ? lines.Count : usedCount;
        var n = 0;
        for (var i = 0; i < limit; i++)
        {
            var len = lines[i] == null ? 0 : lines[i]!.Length;
            if (len > n)
            {
                n = len;
            }
        }

        return n;
    }

    public static int DeskPanelWidthPx(int longestChars, int screenWidthPx)
    {
        var w = (longestChars * DeskCharPx) + DeskInnerPadPx;
        if (w < MinDeskWidthPx)
        {
            w = MinDeskWidthPx;
        }

        var max = screenWidthPx > 80 ? screenWidthPx - 40 : 920;
        if (max < MinDeskWidthPx)
        {
            max = MinDeskWidthPx;
        }

        return w > max ? max : w;
    }

    public static int SwitchListDeskHeightPx(int stepCount, bool coach, int jobDropExtraPx)
    {
        var list = stepCount > 0
            ? SwitchListJobIdPx + DeskListViewHeightPx(stepCount, compact: false) + 4
            : 44;
        var coachH = coach ? SwitchListCoachPx : 0;
        var drop = jobDropExtraPx < 0 ? 0 : jobDropExtraPx;
        return SwitchListTitlePx
            + SwitchListTabsPx
            + SwitchListLicensePx
            + SwitchListJobRowPx
            + drop
            + SwitchListLoadRowPx
            + SwitchListAlignRowPx
            + SwitchListCruiseRowPx
            + list
            + SwitchListPathPx
            + coachH
            + SwitchListHidePx
            + SwitchListBottomPadPx;
    }

    public static int JobDropExtraPx(bool open, int jobCount)
    {
        if (!open || jobCount <= 0)
        {
            return 0;
        }

        var drop = (22 * jobCount) + 8;
        if (drop > 110)
        {
            drop = 110;
        }

        return drop + 4;
    }

    /// <summary>
    /// Full list height on Per job (no scroll). Route compact still caps so
    /// the Route tab does not grow with a 12-row Switch List.
    /// </summary>
    public static int DeskListViewHeightPx(int stepCount, bool compact)
    {
        if (stepCount <= 0)
        {
            return 0;
        }

        var content = (stepCount * DeskLinePx) + 4;
        if (!compact)
        {
            return content;
        }

        const int cap = 56;
        return content < cap ? content : cap;
    }
}
