using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>6.20 Abandoned/Expired → 8 s Cancelled flash.</summary>
public class CancelledFlashTests
{
    [Fact]
    public void Smoke_abandoned_taken_job_flashes_cancelled_eight_seconds()
    {
        var state = default(CancelledFlashState);
        CancelledFlash.Note(ref state, "SM-FH-12", now: 10f);

        Assert.True(CancelledFlash.TryConsume(ref state, now: 10.5f, liveTakenNotCancelled: false, out var id));
        Assert.Equal("SM-FH-12", id);
        Assert.Equal(
            "Job SM-FH-12  |  Cancelled",
            ActiveJobHudLine.FormatCancelled(id));
        Assert.Contains(
            ActiveJobHudLine.CancelledColor,
            ActiveJobHudLine.FormatCancelled(id, richText: true));

        Assert.True(CancelledFlash.TryConsume(ref state, now: 17.9f, liveTakenNotCancelled: false, out _));
        Assert.False(CancelledFlash.TryConsume(ref state, now: 18.1f, liveTakenNotCancelled: false, out _));
    }

    [Fact]
    public void Live_taken_job_wins_over_flash()
    {
        var state = default(CancelledFlashState);
        CancelledFlash.Note(ref state, "OLD", now: 1f);
        Assert.False(CancelledFlash.TryConsume(ref state, now: 2f, liveTakenNotCancelled: true, out _));
        Assert.False(CancelledFlash.TryConsume(ref state, now: 2f, liveTakenNotCancelled: false, out _));
    }

    [Fact]
    public void Complete_clears_flash()
    {
        var state = default(CancelledFlashState);
        CancelledFlash.Note(ref state, "SM-FH-12", now: 1f);
        CancelledFlash.Clear(ref state);
        Assert.False(CancelledFlash.TryConsume(ref state, now: 2f, liveTakenNotCancelled: false, out _));
    }
}
