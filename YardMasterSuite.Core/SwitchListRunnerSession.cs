namespace YardMasterSuite.Core;

/// <summary>Active GO / Human / Done mode for the bound Switch List (**13.1**).</summary>
public static class SwitchListRunnerSession
{
    public static SwitchListRunMode Mode { get; private set; }

    public static bool IsGo => Mode == SwitchListRunMode.Go;

    public static bool IsHumanHold => Mode == SwitchListRunMode.HumanHold;

    public static bool AllowsManualNext =>
        SwitchListRunner.AllowsManualNext(Mode, SwitchListSession.PeekNext != null);

    public static void OnStepEntered(SwitchListStep? step)
    {
        Mode = SwitchListRunner.EnterModeForStep(step);
        PrepTrackArrivalSession.Clear();
        TurntableArrivalSession.Clear();
        PrepCreepSession.Clear();
    }

    public static SwitchListRunnerResult TrySetGo(
        SwitchListStep? step,
        bool hasPlan,
        bool pinForAlign,
        RouteClearancePhase clearancePhase,
        float? derailRiskPercent = null)
    {
        var result = SwitchListRunner.TrySetGo(
            step,
            hasPlan,
            pinForAlign,
            clearancePhase,
            derailRiskPercent);
        if (result == SwitchListRunnerResult.Ok)
        {
            Mode = SwitchListRunMode.Go;
            PidGoStopSession.Clear();
        }

        return result;
    }

    public static SwitchListRunnerResult TryMarkDone()
    {
        var result = SwitchListRunner.TryMarkDone(Mode);
        if (result == SwitchListRunnerResult.Ok)
        {
            Mode = SwitchListRunMode.Manual;
        }

        return result;
    }

    public static SwitchListRunnerResult TryStopGo()
    {
        var result = SwitchListRunner.TryStopGo(Mode);
        if (result == SwitchListRunnerResult.Ok)
        {
            Mode = SwitchListRunMode.Manual;
            PidGoStopSession.Arm();
            PidGoFacingSession.Clear();
            // Prep: sticky hold so yard-chain does not ArmGo again (shove after Stop GO).
            if (SwitchListSession.CurrentStep?.Kind == SwitchListStepKind.Prep)
            {
                PrepCreepSession.LatchCoupleHold();
            }
        }

        return result;
    }

    public static void Clear()
    {
        Mode = SwitchListRunMode.Manual;
        PidGoStopSession.Clear();
        PidGoFacingSession.Clear();
        TurntableArrivalSession.Clear();
        PrepCreepSession.Clear();
    }
}
