using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// SL-55 office ticket: two Prep spurs. Reader used to keep starts[0] only.
/// </summary>
public class SwitchListPickupTracksTests
{
    [Fact]
    public void Smoke_SL_55_task_starts_keep_B1S_and_C4S_drop_B4L_staging()
    {
        var pickups = SwitchListPickupTracks.FromTaskStarts(
            new[] { "SW-B1S", "SW-C4S", "SW-B4L" },
            destTrackId: "SW-C1O",
            reverseIntoTrackId: "SW-B4L");
        Assert.Equal(new[] { "SW-B1S", "SW-C4S" }, pickups);
    }

    [Fact]
    public void Smoke_SL_55_task_starts_keep_B1S_and_C3S()
    {
        var pickups = SwitchListPickupTracks.FromTaskStarts(
            new[] { "SW-B1S", "SW-C3S" },
            destTrackId: "SW-C1O");
        Assert.Equal(new[] { "SW-B1S", "SW-C3S" }, pickups);
    }

    [Fact]
    public void FromTaskStarts_skips_connectors_and_final_dest()
    {
        var pickups = SwitchListPickupTracks.FromTaskStarts(
            new[] { "#Y-#S200#T", "SW-B1S", "SW-C1O", "SW-C3S", "SW-B1S" },
            destTrackId: "SW-C1O");
        Assert.Equal(new[] { "SW-B1S", "SW-C3S" }, pickups);
    }

    [Fact]
    public void Resolve_origin_then_additional()
    {
        var job = new JobSummary
        {
            OriginTrackId = "SW-B1S",
            AdditionalPickupTrackIds = new[] { "SW-C3S", "SW-B1S", "#Y-#S1#T" },
        };
        Assert.Equal(new[] { "SW-B1S", "SW-C3S" }, SwitchListPickupTracks.Resolve(job));
    }
}
