namespace YardMasterSuite.Core;

public static class SwitchListRunnerTelemetry
{
    public const string Go = "T2 switch-list: go";
    public const string GoStop = "T2 switch-list: go-stop";
    public const string HumanHold = "T2 switch-list: human hold";
    public const string Done = "T2 switch-list: done";
    public const string NextBlocked = "T2 switch-list: next blocked";
    public const string CoupleNext = "T2 switch-list: couple-next";

    public static string FormatResult(SwitchListRunnerResult result) =>
        result switch
        {
            SwitchListRunnerResult.NeedPlan => "T2 switch-list: go need plan",
            SwitchListRunnerResult.NeedCleared => RouteClearanceGate.DenyAlignLog,
            SwitchListRunnerResult.WrongStepKind => "T2 switch-list: go wrong step",
            SwitchListRunnerResult.NotHumanHold => "T2 switch-list: not human",
            SwitchListRunnerResult.NotGoActive => "T2 switch-list: not go",
            SwitchListRunnerResult.NextBlocked => NextBlocked,
            _ => "",
        };
}
