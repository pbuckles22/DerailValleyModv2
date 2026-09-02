namespace YardMasterSuite.Core;

/// <summary>Step runner drive mode (**13.1**).</summary>
public enum SwitchListRunMode
{
    /// <summary>Player drives; manual <c>Next</c> allowed (clearance gates still apply).</summary>
    Manual = 0,

    /// <summary>PID + Maps on a Transit / Pivot leg.</summary>
    Go = 1,

    /// <summary>Human-only leg — hold until <c>Done</c>; no <c>Next</c>.</summary>
    HumanHold = 2,
}

public enum SwitchListRunnerResult
{
    Ok = 0,
    NoActiveStep = 1,
    WrongStepKind = 2,
    NeedPlan = 3,
    NeedCleared = 4,
    NotHumanHold = 5,
    NotGoActive = 6,
    NextBlocked = 7,
}
