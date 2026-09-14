using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SmokeJobHoldGateTests
{
    [Fact]
    public void Smoke_13_2_5_dropdown_keeps_selected_job_not_ranked_pick()
    {
        Assert.Equal(-1, SmokeJobHoldGate.ResolveSelectedIndex(0, 0));
        Assert.Equal(0, SmokeJobHoldGate.ResolveSelectedIndex(3, 0));
        Assert.Equal(2, SmokeJobHoldGate.ResolveSelectedIndex(3, 2));
        Assert.Equal(0, SmokeJobHoldGate.ResolveSelectedIndex(3, 99));

        var shuffled = new[] { "SW-FH-82", "SW-SL-55", "SW-SL-52" };
        Assert.Equal(
            1,
            SmokeJobHoldGate.IndexAfterRefresh(shuffled, "SW-SL-55", 0));
        Assert.Equal(
            0,
            SmokeJobHoldGate.IndexAfterRefresh(shuffled, null, 0));
        Assert.Equal(
            2,
            SmokeJobHoldGate.IndexAfterRefresh(shuffled, "missing", 2));

        Assert.Equal(1, SmokeJobHoldGate.IndexOfId(new[] { "SW-SL-52", "SW-SL-55" }, "SW-SL-55"));
        Assert.Equal(-1, SmokeJobHoldGate.IndexOfId(new[] { "SW-SL-52" }, "SW-SL-55"));
        Assert.Equal("SW-SL-55 ▼", SmokeJobHoldGate.FormatDeskJobButton("SW-SL-55", "SW-SL-55", true));
        Assert.Equal(
            "SW-SL-55 ▼ list SW-SL-52",
            SmokeJobHoldGate.FormatDeskJobButton("SW-SL-55", "SW-SL-52", true));
        Assert.Equal("SW-FH-82 ▼", SmokeJobHoldGate.FormatDeskJobButton("SW-FH-82", null, false));
    }

    [Fact]
    public void Format_lines_name_the_job()
    {
        Assert.Equal("T2 smoke-job: taken job=SW-FH-82", SmokeJobHoldGate.FormatTaken("SW-FH-82"));
        Assert.Equal("T2 smoke-job: wait available", SmokeJobHoldGate.FormatWait());
        Assert.Equal("T2 smoke-job: hold job=SW-FH-82", SmokeJobHoldGate.FormatHeld("SW-FH-82"));
        Assert.True(SmokeJobHoldGate.Enabled);
        Assert.Equal("SW-SL-55", SmokeJobHoldGate.PreferredJobId);
    }

    [Fact]
    public void Smoke_load_prefers_SL55_over_first_available_FH()
    {
        var board = new[] { "SW-FH-82", "SW-SL-55", "SW-SL-52" };
        Assert.Equal(1, SmokeJobHoldGate.IndexOfPreferredOrSelected(board, 0));
        Assert.Equal(0, SmokeJobHoldGate.IndexOfPreferredOrSelected(new[] { "SW-FH-82" }, 0));
        Assert.Equal(-1, SmokeJobHoldGate.IndexOfPreferredOrSelected(null, 0));
    }
}
