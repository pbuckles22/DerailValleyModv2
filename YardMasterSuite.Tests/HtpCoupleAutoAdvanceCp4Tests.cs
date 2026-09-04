using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// HTP CP4 — 7.4 couple success during Prep auto-advances the Switch List (**13.2.1**).
/// </summary>
[Collection("StaticSessions")]
public class HtpCoupleAutoAdvanceCp4Tests
{
    public HtpCoupleAutoAdvanceCp4Tests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_13_2_1_prep_couple_success_advances_step()
    {
        SwitchListSession.Bind(
            "SW-FH-82",
            new[]
            {
                new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O"),
                new SwitchListStep(6, SwitchListStepKind.Transit, "GF", "GF-D5I", "Transit → GF-D5I"),
            });

        Assert.Equal(SwitchListStepKind.Prep, SwitchListSession.CurrentStep!.Kind);
        Assert.Equal(0, SwitchListSession.CurrentIndex);
        Assert.False(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: false));
        Assert.Equal(0, SwitchListSession.CurrentIndex);

        Assert.True(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(1, SwitchListSession.CurrentIndex);
        Assert.Equal(SwitchListStepKind.Transit, SwitchListSession.CurrentStep!.Kind);
        Assert.Equal("T2 switch-list: couple-next", SwitchListRunnerTelemetry.CoupleNext);
    }

    [Fact]
    public void Smoke_13_2_1_couple_success_skips_non_prep_and_last_human()
    {
        SwitchListSession.Bind(
            "route:SW",
            new[]
            {
                new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "SW-B4L", "Transit"),
                new SwitchListStep(2, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep"),
            });
        Assert.False(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(0, SwitchListSession.CurrentIndex);

        SwitchListSession.Bind(
            "SW-FH-82",
            new[]
            {
                new SwitchListStep(7, SwitchListStepKind.Delivery, "GF", "GF-D5I", "Delivery"),
            });
        Assert.False(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(0, SwitchListSession.CurrentIndex);
        Assert.Equal(SwitchListStepKind.Delivery, SwitchListSession.CurrentStep!.Kind);

        SwitchListSession.Bind(
            "yard",
            new[]
            {
                new SwitchListStep(3, SwitchListStepKind.ReverseInto, "SW", "SW-C1O", "Reverse into"),
                new SwitchListStep(4, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep"),
            });
        Assert.False(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(SwitchListStepKind.ReverseInto, SwitchListSession.CurrentStep!.Kind);
    }

    [Fact]
    public void Smoke_13_2_1_couple_gate_is_prep_plus_next_allowed()
    {
        Assert.True(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.HumanHold,
            hasNextStep: true,
            coupleSuccess: true));
        Assert.True(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.Manual,
            hasNextStep: true,
            coupleSuccess: true));
        Assert.False(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.HumanHold,
            hasNextStep: true,
            coupleSuccess: false));
        Assert.False(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.HumanHold,
            hasNextStep: false,
            coupleSuccess: true));
        Assert.False(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Transit,
            SwitchListRunMode.Manual,
            hasNextStep: true,
            coupleSuccess: true));
        Assert.False(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.Go,
            hasNextStep: true,
            coupleSuccess: true));
    }
}
