using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

[Collection("StaticSessions")]
public class SwitchListResumeSessionTests
{
    public SwitchListResumeSessionTests()
    {
        YmsRouteSessions.ClearAll();
        SwitchListResumeSession.Forget();
    }

    [Fact]
    public void Smoke_mid_job_save_keeps_list_after_drive_session_wipe()
    {
        var steps = new[]
        {
            new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "SW-B4L", "Past 1"),
            new SwitchListStep(2, SwitchListStepKind.TurnAround, "SW", "#Y-#S1774#T", "to TT"),
            new SwitchListStep(4, SwitchListStepKind.Transit, "SW", "SW-B4L", "Past 4"),
        };
        SwitchListSession.Bind("SW-SL-55", steps);
        Assert.True(SwitchListSession.TrySeek(1));
        SwitchListResumeSession.CaptureLive();
        YmsRouteSessions.ClearAll();
        Assert.False(SwitchListSession.HasActive);
        Assert.True(SwitchListResumeSession.HasResume);
        Assert.True(SwitchListResumeSession.TryRestore());
        Assert.Equal("SW-SL-55", SwitchListSession.JobId);
        Assert.Equal(1, SwitchListSession.CurrentIndex);
        Assert.Equal(2, SwitchListSession.CurrentStep?.Index);
        SwitchListResumeSession.Forget();
    }

    [Fact]
    public void Smoke_desk_clear_forgets_resume_so_reload_stays_empty()
    {
        var steps = new[]
        {
            new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "SW-B4L", "Past 1"),
        };
        SwitchListSession.Bind("SW-FH-82", steps);
        SwitchListResumeSession.CaptureLive();
        Assert.Equal(MapsDestKind.Clear, MapsDestApply.Clear());
        Assert.False(SwitchListResumeSession.HasResume);
        Assert.False(SwitchListResumeSession.TryRestore());
        Assert.False(SwitchListSession.HasActive);
    }
}
