using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// HTP CP3 — multi-leg yard chain through Prep (steps 1–5). Haul is Epic 15.
/// </summary>
[Collection("StaticSessions")]
public class SwitchListYardChainTests
{
    public SwitchListYardChainTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_13_4_yard_chain_auto_go_cleared_next_stops_at_prep()
    {
        var steps = new[]
        {
            new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "SW-B4L", "Past switch until CLEARED"),
            new SwitchListStep(
                2,
                SwitchListStepKind.TurnAround,
                "SW",
                "#Y-#S1774#T",
                SwitchListDriveFacing.FormatDriveLabel(false, SwitchListDriveFacing.ToTurntableAction, "#Y-#S1774#T")),
            new SwitchListStep(
                3,
                SwitchListStepKind.TurnAround,
                "SW",
                "#Y-#S1774#T",
                SwitchListDriveFacing.TurnAroundOnTurntable),
            new SwitchListStep(4, SwitchListStepKind.Transit, "SW", "#Y-#S1512#T", "Past switch until CLEARED"),
            new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O"),
            new SwitchListStep(6, SwitchListStepKind.Transit, "GF", "GF-D5I", "Transit → GF-D5I"),
        };

        Assert.Equal(4, SwitchListYardChain.LastPrepIndex(steps));
        Assert.True(SwitchListYardChain.InYardPrepScope(steps, 0));
        Assert.True(SwitchListYardChain.InYardPrepScope(steps, 4));
        Assert.False(SwitchListYardChain.InYardPrepScope(steps, 5));

        Assert.True(SwitchListRunner.StepSupportsGo(steps[0]));
        Assert.True(SwitchListRunner.StepSupportsGo(steps[1]));
        Assert.False(SwitchListRunner.StepSupportsGo(steps[2]));
        Assert.True(SwitchListRunner.StepSupportsGo(steps[4]));

        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                steps[0],
                steps,
                currentIndex: 0,
                RouteClearancePhase.AtSwitch,
                prepAtSpur: false,
                hasPlan: true,
                pinBlocksAlign: true));

        Assert.Equal(
            SwitchListYardChainAction.ArmGo,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                steps[0],
                steps,
                currentIndex: 0,
                RouteClearancePhase.Cleared,
                prepAtSpur: false,
                hasPlan: true,
                pinBlocksAlign: true));

        Assert.Equal(
            SwitchListYardChainAction.ArmGo,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                steps[1],
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                pinBlocksAlign: false));

        Assert.Equal(
            SwitchListYardChainAction.StopGoCompleteCleared,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                steps[0],
                steps,
                currentIndex: 0,
                RouteClearancePhase.Cleared,
                prepAtSpur: false,
                hasPlan: true,
                pinBlocksAlign: true));

        Assert.True(SwitchListYardChain.ShouldAutoNextAfterCleared(steps, 0, hasNextStep: true));
        Assert.False(SwitchListYardChain.ShouldAutoNextAfterCleared(steps, 4, hasNextStep: true));

        Assert.Equal(
            SwitchListYardChainAction.StopGoAtPrepSpur,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                steps[4],
                steps,
                currentIndex: 4,
                RouteClearancePhase.Idle,
                prepAtSpur: true,
                hasPlan: true));

        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                steps[5],
                steps,
                currentIndex: 5,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true));
    }

    [Fact]
    public void Smoke_13_4_yard_chain_walk_pin_legs_to_prep()
    {
        var inbound = new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "SW-B4L", "Past switch");
        var leave = new SwitchListStep(2, SwitchListStepKind.Transit, "SW", "#Y-#S1512#T", "Past switch");
        var prep = new SwitchListStep(3, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep");
        var haul = new SwitchListStep(4, SwitchListStepKind.Transit, "GF", "GF-D5I", "Transit");
        var steps = new[] { inbound, leave, prep, haul };
        SwitchListSession.Bind("SW-FH-82", steps);

        Assert.Equal(
            SwitchListYardChainAction.ArmGo,
            SwitchListYardChain.Evaluate(
                SwitchListRunnerSession.Mode,
                SwitchListSession.CurrentStep,
                steps,
                0,
                RouteClearancePhase.Cleared,
                false,
                true,
                pinBlocksAlign: true));
        Assert.Equal(
            SwitchListRunnerResult.Ok,
            SwitchListRunnerSession.TrySetGo(
                inbound,
                hasPlan: true,
                pinForAlign: true,
                RouteClearancePhase.Cleared,
                derailRiskPercent: 10f));

        Assert.Equal(
            SwitchListYardChainAction.StopGoCompleteCleared,
            SwitchListYardChain.Evaluate(
                SwitchListRunnerSession.Mode,
                inbound,
                steps,
                0,
                RouteClearancePhase.Cleared,
                false,
                true,
                pinBlocksAlign: true));
        Assert.Equal(SwitchListRunnerResult.Ok, SwitchListRunnerSession.TryStopGo());
        Assert.True(SwitchListSession.TryAdvance());
        Assert.Equal(SwitchListStepKind.Transit, SwitchListSession.CurrentStep!.Kind);

        Assert.Equal(
            SwitchListRunnerResult.Ok,
            SwitchListRunnerSession.TrySetGo(
                leave,
                hasPlan: true,
                pinForAlign: true,
                RouteClearancePhase.Cleared,
                10f));
        Assert.Equal(SwitchListRunnerResult.Ok, SwitchListRunnerSession.TryStopGo());
        Assert.True(SwitchListSession.TryAdvance());
        Assert.Equal(SwitchListStepKind.Prep, SwitchListSession.CurrentStep!.Kind);
        Assert.True(SwitchListYardChain.InYardPrepScope(steps, SwitchListSession.CurrentIndex));
        Assert.False(SwitchListTakeArm.IsHaulTransitTake(
            steps,
            SwitchListSession.CurrentIndex,
            SwitchListSession.CurrentStep));
    }
}
