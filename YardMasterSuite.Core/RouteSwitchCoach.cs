namespace YardMasterSuite.Core;

/// <summary>
/// Two-step switch-back coach for Maps / Switch List (**8.7**).
/// Armed while pin active: misaligned throws and/or junction-first sawtooth (Path OK).
/// </summary>
public readonly struct RouteSwitchCoachLines
{
    public RouteSwitchCoachLines(
        bool show,
        int activeStep,
        string? step1,
        string? step2,
        string? arCaption)
    {
        Show = show;
        ActiveStep = activeStep;
        Step1 = step1;
        Step2 = step2;
        ArCaption = arCaption;
    }

    public bool Show { get; }
    /// <summary>1 = drive past pin; 2 = Align then drive to dest.</summary>
    public int ActiveStep { get; }
    public string? Step1 { get; }
    public string? Step2 { get; }
    /// <summary>AR glyph override (At switch / CLEARED / 1/2 · …).</summary>
    public string? ArCaption { get; }
}

public static class RouteSwitchCoach
{
    public static RouteSwitchCoachLines Format(
        bool pinArmed,
        RouteClearancePhase phase,
        bool pinIsBehind,
        bool destIsBehind)
    {
        if (!pinArmed)
        {
            return default;
        }

        var towardPin = SwitchListDriveFacing.SetWord(pinIsBehind);
        var destSetReverse = RouteDestFacingPolicy.DestNeedsReverse(pinIsBehind, destIsBehind);
        var towardDest = SwitchListDriveFacing.SetWord(destSetReverse);
        var cleared = phase == RouteClearancePhase.Cleared;

        string step1;
        string step2;
        string ar;
        int active;

        if (!cleared)
        {
            active = 1;
            step1 = "1/2 Drive past switch — " + towardPin + " until CLEARED";
            step2 = "2/2 Align Route, then " + towardDest + " to dest";
            ar = "At switch";
        }
        else
        {
            active = 2;
            step1 = "1/2 CLEARED — press Align";
            step2 = "2/2 Align Route, then " + towardDest + " to dest";
            ar = "CLEARED";
        }

        return new RouteSwitchCoachLines(
            show: true,
            activeStep: active,
            step1: step1,
            step2: step2,
            arCaption: ar);
    }

    /// <summary>Single desk line for the active step (compact Path row).</summary>
    public static string? ActiveLine(in RouteSwitchCoachLines lines)
    {
        if (!lines.Show)
        {
            return null;
        }

        return lines.ActiveStep <= 1 ? lines.Step1 : lines.Step2;
    }
}
