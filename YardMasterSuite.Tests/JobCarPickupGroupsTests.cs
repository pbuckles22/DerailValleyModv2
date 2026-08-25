using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// One pin per pickup spur, not per car. Nearest groups win the 8-slot cap.
/// </summary>
public class JobCarPickupGroupsTests
{
    [Fact]
    public void Same_spur_accumulates_one_pin()
    {
        var groups = new JobCarPickupAccum[JobCarPickupGroups.AccumCapacity];
        var n = 0;
        Assert.True(JobCarPickupGroups.Add(groups, ref n, "C2S", 10f, 1f, 0f));
        Assert.True(JobCarPickupGroups.Add(groups, ref n, "C2S", 12f, 1f, 0f));
        Assert.Equal(1, n);
        Assert.Equal(2, groups[0].Count);
        Assert.Equal(11f, groups[0].CentroidX);
        Assert.Equal("C2S", groups[0].TrackLabel);
    }

    [Fact]
    public void Different_spurs_are_separate_pins()
    {
        var groups = new JobCarPickupAccum[JobCarPickupGroups.AccumCapacity];
        var n = 0;
        JobCarPickupGroups.Add(groups, ref n, "C2S", 0f, 0f, 0f);
        JobCarPickupGroups.Add(groups, ref n, "B6S", 100f, 0f, 0f);
        Assert.Equal(2, n);

        var dest = new JobCarPickupMarker[JobCarPickupGroups.DefaultMaxMarkers];
        var ranked = JobCarPickupGroups.RankNearest(
            groups, n, havePlayer: true, 0f, 0f, 0f, dest);
        Assert.Equal(2, ranked);
        Assert.Equal("C2S", dest[0].TrackLabel);
        Assert.Equal("B6S", dest[1].TrackLabel);
        Assert.Equal(1, dest[0].Count);
        Assert.Equal(1, dest[1].Count);
    }

    [Fact]
    public void Rank_keeps_eight_nearest_of_nine()
    {
        var groups = new JobCarPickupAccum[JobCarPickupGroups.AccumCapacity];
        var n = 0;
        for (var i = 0; i < 9; i++)
        {
            JobCarPickupGroups.Add(groups, ref n, "S" + i, i * 10f, 0f, 0f);
        }

        Assert.Equal(9, n);
        var dest = new JobCarPickupMarker[JobCarPickupGroups.DefaultMaxMarkers];
        var ranked = JobCarPickupGroups.RankNearest(
            groups, n, havePlayer: true, 0f, 0f, 0f, dest);
        Assert.Equal(8, ranked);
        Assert.Equal("S0", dest[0].TrackLabel);
        Assert.Equal("S7", dest[7].TrackLabel);
        for (var i = 0; i < ranked; i++)
        {
            Assert.NotEqual("S8", dest[i].TrackLabel);
        }
    }

    [Fact]
    public void Missing_spur_uses_em_dash()
    {
        var groups = new JobCarPickupAccum[JobCarPickupGroups.AccumCapacity];
        var n = 0;
        JobCarPickupGroups.Add(groups, ref n, null, 1f, 2f, 3f);
        JobCarPickupGroups.Add(groups, ref n, "  ", 4f, 5f, 6f);
        Assert.Equal(1, n);
        Assert.Equal("—", groups[0].TrackLabel);
        Assert.Equal(2, groups[0].Count);
    }

    [Fact]
    public void Smoke_walk_along_consist_pin_follows_nearest_car()
    {
        var samples = new JobCarPickupSample[JobCarPickupGroups.SampleCapacity];
        var n = 3;
        samples[0] = new JobCarPickupSample(0, 0f, 1f, 0f);
        samples[1] = new JobCarPickupSample(0, 36f, 1f, 0f);
        samples[2] = new JobCarPickupSample(0, 72f, 1f, 0f);

        Assert.True(JobCarPickupGroups.TryPickNearestInGroup(
            samples, n, 0, 0f, 1f, 0f, out var x0, out var y0, out var z0));
        Assert.Equal(0f, x0);
        Assert.Equal(1f, y0);
        Assert.Equal(0f, z0);

        Assert.True(JobCarPickupGroups.TryPickNearestInGroup(
            samples, n, 0, 72f, 1f, 0f, out var x1, out _, out _));
        Assert.Equal(72f, x1);

        Assert.True(JobCarPickupGroups.TryPickNearestInGroup(
            samples, n, 0, 40f, 1f, 0f, out var xMid, out _, out _));
        Assert.Equal(36f, xMid);
        Assert.NotEqual(36f, x0);
    }

    [Fact]
    public void Smoke_taken_job_turnaround_pin_stays_on_cars()
    {
        var samples = new JobCarPickupSample[JobCarPickupGroups.SampleCapacity];
        samples[0] = new JobCarPickupSample(0, 100f, 2f, 50f);
        samples[1] = new JobCarPickupSample(0, 108f, 2f, 50f);
        Assert.True(JobCarPickupGroups.TryPickNearestInGroup(
            samples, 2, 0, 800f, 4f, -200f, out var x, out var y, out var z));
        Assert.Equal(108f, x);
        Assert.Equal(2f, y);
        Assert.Equal(50f, z);
    }

    [Fact]
    public void Smoke_turn_around_uses_closest_car_in_fov()
    {
        var samples = new JobCarPickupSample[JobCarPickupGroups.SampleCapacity];
        samples[0] = new JobCarPickupSample(0, 0f, 1f, 0f);
        samples[1] = new JobCarPickupSample(0, 72f, 1f, 0f);

        Assert.True(JobCarPickupGroups.TryPickNearestInView(
            samples, 2, 0,
            36f, 1f, 0f,
            1f, 0f, 0f,
            JobCarPickupGroups.InViewMinForward,
            out var lookAheadX, out _, out _));
        Assert.Equal(72f, lookAheadX);

        Assert.True(JobCarPickupGroups.TryPickNearestInView(
            samples, 2, 0,
            36f, 1f, 0f,
            -1f, 0f, 0f,
            JobCarPickupGroups.InViewMinForward,
            out var lookBackX, out _, out _));
        Assert.Equal(0f, lookBackX);
    }

    [Fact]
    public void Smoke_beside_consist_pin_stays_on_near_car_in_fov()
    {
        // 2.6.21.4: looking down the alley, lumber fills the left FOV (~4 m)
        // but the pin sat on a 43 m car and fanned to the HUD corner.
        var samples = new JobCarPickupSample[JobCarPickupGroups.SampleCapacity];
        samples[0] = new JobCarPickupSample(0, 4f, 1f, 0f);
        samples[1] = new JobCarPickupSample(0, 4f, 1f, 43f);

        Assert.True(JobCarPickupGroups.TryPickNearestInView(
            samples, 2, 0,
            0f, 1f, 0f,
            0f, 0f, 1f,
            JobCarPickupGroups.InViewMinForward,
            out var x, out var y, out var z));
        Assert.Equal(4f, x);
        Assert.Equal(1f, y);
        Assert.Equal(0f, z);
    }

    [Fact]
    public void Smoke_mid_flatcar_origin_off_axis_still_beats_far_car()
    {
        // Gemini: 17 m flatcar, camera at mid-side looking at lumber. Origin is
        // ~8.5 m off-axis and slightly behind the camera plane (vf ≈ -2 m).
        var samples = new JobCarPickupSample[JobCarPickupGroups.SampleCapacity];
        samples[0] = new JobCarPickupSample(0, 8.5f, 1f, -2f);
        samples[1] = new JobCarPickupSample(0, 4f, 1f, 43f);

        Assert.True(JobCarPickupGroups.IsInView(
            8.5f, 0f, -2f, 0f, 0f, 1f));
        Assert.False(JobCarPickupGroups.IsInView(
            0f, 0f, -4f, 0f, 0f, 1f));

        Assert.True(JobCarPickupGroups.TryPickNearestInView(
            samples, 2, 0,
            0f, 1f, 0f,
            0f, 0f, 1f,
            JobCarPickupGroups.InViewMinForward,
            out var x, out _, out var z));
        Assert.Equal(8.5f, x);
        Assert.Equal(-2f, z);
    }

    [Fact]
    public void In_view_pick_falls_back_when_none_ahead()
    {
        var samples = new JobCarPickupSample[JobCarPickupGroups.SampleCapacity];
        samples[0] = new JobCarPickupSample(0, 0f, 0f, 0f);
        samples[1] = new JobCarPickupSample(0, 10f, 0f, 0f);
        Assert.False(JobCarPickupGroups.TryPickNearestInView(
            samples, 2, 0,
            50f, 0f, 0f,
            1f, 0f, 0f,
            JobCarPickupGroups.InViewMinForward,
            out _, out _, out _));
    }

    [Fact]
    public void Pick_nearest_in_view_does_not_allocate()
    {
        var samples = new JobCarPickupSample[JobCarPickupGroups.SampleCapacity];
        samples[0] = new JobCarPickupSample(0, 0f, 0f, 0f);
        samples[1] = new JobCarPickupSample(0, 40f, 0f, 0f);
        JobCarPickupGroups.TryPickNearestInView(
            samples, 2, 0, 0f, 0f, 0f, 1f, 0f, 0f,
            JobCarPickupGroups.InViewMinForward, out _, out _, out _);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            JobCarPickupGroups.TryPickNearestInView(
                samples, 2, 0, 0f, 0f, 0f, 1f, 0f, 0f,
                JobCarPickupGroups.InViewMinForward, out _, out _, out _);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void Rank_does_not_allocate()
    {
        var groups = new JobCarPickupAccum[JobCarPickupGroups.AccumCapacity];
        var n = 0;
        JobCarPickupGroups.Add(groups, ref n, "C2S", 0f, 0f, 0f);
        JobCarPickupGroups.Add(groups, ref n, "B6S", 40f, 0f, 0f);
        var dest = new JobCarPickupMarker[JobCarPickupGroups.DefaultMaxMarkers];
        JobCarPickupGroups.RankNearest(groups, n, true, 0f, 0f, 0f, dest);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            n = 0;
            JobCarPickupGroups.Add(groups, ref n, "C2S", 0f, 0f, 0f);
            JobCarPickupGroups.Add(groups, ref n, "B6S", 40f, 0f, 0f);
            JobCarPickupGroups.RankNearest(groups, n, true, 0f, 0f, 0f, dest);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
