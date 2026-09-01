using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Core funnel: warm / roll / pop / push + EventBus publish (no Unity).
/// </summary>
public class PostedLimitFunnelTests
{
    [Fact]
    public void Straight_trip_pop_sets_sticky_and_next()
    {
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[]
            {
                Board(1, 0f, 50f, 40f),
                Board(2, 0f, 150f, 60f),
                Board(3, 0f, 300f, 80f),
            },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f);

        Assert.Equal(3, funnel.Count);
        Assert.Null(funnel.StickyKmh);

        LockTravel(funnel);
        funnel.Tick(0f, 0f, 51f, 0f, 0f, 1f, speedKmh: 20f);
        var snap = funnel.ToSnapshot();
        Assert.Equal(40f, snap.Kmh);
        Assert.Equal(2, snap.RosterCount);
        Assert.Equal(60f, snap.NextKmh);
        Assert.True(snap.NextAlongMeters is float along && along > 90f && along < 110f);
        Assert.True(funnel.LastTakeAlongMeters <= 0f);
    }

    [Fact]
    public void Pop_then_push_keeps_cap_and_order()
    {
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[]
            {
                Board(1, 0f, 50f, 40f),
                Board(2, 0f, 150f, 60f),
                Board(3, 0f, 300f, 80f),
            },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f);

        LockTravel(funnel);
        funnel.Tick(0f, 0f, 51f, 0f, 0f, 1f, speedKmh: 20f);
        Assert.Equal(2, funnel.Count);
        Assert.True(
            funnel.TryPush(Board(4, 0f, 400f, 90f), 0f, 0f, 51f, 0f, 0f, 1f));
        Assert.Equal(3, funnel.Count);
        Assert.Equal(90f, funnel.BoardAt(2).ThroughKmh);

        var snap = funnel.ToSnapshot();
        Assert.Equal(40f, snap.Kmh);
        Assert.Equal(60f, snap.NextKmh);
        Assert.True(funnel.Count <= PostedLimitFilo.MaxDepth);
    }

    [Fact]
    public void Y_sit_keeps_both_exits_until_direction_lock()
    {
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[]
            {
                Board(1, 0f, 135f, 40f),
                Board(2, 0f, -80f, 60f),
            },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f);

        Assert.False(funnel.DirectionLocked);
        Assert.Equal(2, funnel.Count);

        funnel.SetTravel(0f, 0f, 1f, speedKmh: 6f, locoX: 0f, locoY: 0f, locoZ: 0f);
        Assert.True(funnel.DirectionLocked);
        Assert.Equal(1, funnel.Count);
        Assert.Equal(40f, funnel.BoardAt(0).ThroughKmh);
    }

    [Fact]
    public void Station_warm_caps_ahead_at_max_depth()
    {
        var boards = new ParsedPostedBoard[8];
        for (var i = 0; i < boards.Length; i++)
        {
            boards[i] = Board(i + 1, 0f, 50f + (i * 40f), 40f + (i * 10f));
        }

        var funnel = new PostedLimitFunnel();
        funnel.Warm(boards, 0f, 0f, 0f, 0f, 0f, 1f);
        Assert.Equal(PostedLimitFilo.MaxDepth, funnel.Count);
        Assert.Equal(40f, funnel.BoardAt(0).ThroughKmh);
    }

    [Fact]
    public void PublishIfChanged_raises_eventbus_once_on_take()
    {
        YmsEventBus.ClearAllSubscriptions();
        var stickyFortyRaises = 0;
        YmsEventBus.OnPostedLimitChanged += snap =>
        {
            if (snap.Kmh == 40f)
            {
                stickyFortyRaises++;
            }
        };

        try
        {
            var funnel = new PostedLimitFunnel();
            var cache = default(PostedLimitCache);
            funnel.Warm(
                new[]
                {
                    Board(1, 0f, 50f, 40f),
                    Board(2, 0f, 150f, 60f),
                },
                0f,
                0f,
                0f,
                0f,
                0f,
                1f);
            funnel.PublishIfChanged(ref cache);

            LockTravel(funnel);
            funnel.Tick(0f, 0f, 51f, 0f, 0f, 1f, speedKmh: 20f);
            Assert.True(funnel.PublishIfChanged(ref cache));
            Assert.Equal(1, stickyFortyRaises);

            funnel.Tick(0f, 0f, 51f, 0f, 0f, 1f, speedKmh: 20f);
            Assert.False(funnel.PublishIfChanged(ref cache));
            Assert.Equal(1, stickyFortyRaises);
        }
        finally
        {
            YmsEventBus.ClearAllSubscriptions();
        }
    }

    [Fact]
    public void PublishIfChanged_raiseEvent_false_skips_eventbus()
    {
        YmsEventBus.ClearAllSubscriptions();
        var raises = 0;
        YmsEventBus.OnPostedLimitChanged += _ => raises++;

        try
        {
            var funnel = new PostedLimitFunnel();
            var cache = default(PostedLimitCache);
            funnel.Warm(
                new[] { Board(1, 0f, 50f, 40f) },
                0f,
                0f,
                0f,
                0f,
                0f,
                1f);
            LockTravel(funnel);
            funnel.Tick(0f, 0f, 51f, 0f, 0f, 1f, speedKmh: 20f);
            Assert.True(funnel.PublishIfChanged(ref cache, raiseEvent: false));
            Assert.Equal(0, raises);
            Assert.Equal(40f, funnel.StickyKmh);
        }
        finally
        {
            YmsEventBus.ClearAllSubscriptions();
        }
    }

    [Fact]
    public void Tick_and_PublishIfChanged_do_not_allocate()
    {
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[]
            {
                Board(1, 0f, 200f, 40f),
                Board(2, 0f, 400f, 60f),
            },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f);
        LockTravel(funnel);
        var cache = default(PostedLimitCache);
        funnel.PublishIfChanged(ref cache);

        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            funnel.Tick(0f, 0f, 10f + (i % 50), 0f, 0f, 1f, speedKmh: 20f);
            funnel.PublishIfChanged(ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void Smoke_town_rewarm_preserves_sticky_limit()
    {
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[]
            {
                Board(1, 0f, 50f, 40f),
                Board(2, 0f, 150f, 60f),
                Board(3, 0f, 300f, 80f),
            },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f);
        LockTravel(funnel);
        funnel.Tick(0f, 0f, 51f, 0f, 0f, 1f, speedKmh: 20f);
        Assert.Equal(40f, funnel.StickyKmh);

        funnel.Warm(
            new[]
            {
                Board(2, 0f, 100f, 60f),
                Board(3, 0f, 250f, 80f),
                Board(4, 0f, -50f, 50f),
            },
            0f,
            0f,
            51f,
            0f,
            0f,
            1f,
            preserveSticky: 40f);

        Assert.Equal(40f, funnel.StickyKmh);
        Assert.True(funnel.AlongAt(0) > 0f);
        var snap = funnel.ToSnapshot();
        Assert.Equal(40f, snap.Kmh);
        Assert.Null(snap.NextKmh);
        Assert.NotEqual(SpeedLimitState.UnrestrictedKmh, snap.Kmh);
    }

    [Fact]
    public void Smoke_yard_null_flicker_does_not_rewarm()
    {
        Assert.False(PostedLimitFilo.ShouldRewarmForYard("SW", null));
        Assert.False(PostedLimitFilo.ShouldRewarmForYard("SW", ""));
        Assert.False(PostedLimitFilo.ShouldRewarmForYard("SW", "SW"));
        Assert.True(PostedLimitFilo.ShouldRewarmForYard("SW", "CS"));
        Assert.True(PostedLimitFilo.ShouldRewarmForYard(null, "SW"));
    }

    [Fact]
    public void Smoke_empty_fot_disabled_refill_from_roster_instead()
    {
        Assert.False(PostedLimitFilo.ShouldEmptyFot());

        var roster = new ParsedPostedBoard[8];
        for (var i = 0; i < roster.Length; i++)
        {
            roster[i] = Board(i + 1, 0f, 50f + (i * 80f), 40f + (i * 10f));
        }

        var funnel = new PostedLimitFunnel();
        funnel.Warm(roster, 0f, 0f, 0f, 0f, 0f, 1f);
        Assert.Equal(PostedLimitFilo.MaxDepth, funnel.Count);

        LockTravel(funnel);
        funnel.Tick(0f, 0f, 51f, 0f, 0f, 1f, speedKmh: 20f);
        Assert.Equal(40f, funnel.StickyKmh);
        var afterPop = funnel.Count;
        Assert.Equal(PostedLimitFilo.MaxDepth - 1, afterPop);

        var added = funnel.RefillFrom(roster, 0f, 0f, 51f, 0f, 0f, 1f);
        Assert.True(added >= 1);
        Assert.True(funnel.Count > afterPop);
        Assert.Equal(40f, funnel.StickyKmh);
    }

    [Fact]
    public void Smoke_unlocked_sit_withholds_next_from_snapshot()
    {
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[]
            {
                Board(1, 0f, 122f, 50f),
                Board(2, 0f, -167f, 60f),
            },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f);

        Assert.False(funnel.DirectionLocked);
        var sit = funnel.ToSnapshot();
        Assert.Null(sit.NextKmh);
        Assert.Null(sit.NextAlongMeters);

        LockTravel(funnel);
        var locked = funnel.ToSnapshot();
        Assert.NotNull(locked.NextKmh);
    }

    [Fact]
    public void Smoke_sit_jitter_keeps_next_visible_within_tolerance()
    {
        Assert.True(PostedLimitFilo.IsVisibleAlong(-0.01f));
        Assert.True(PostedLimitFilo.IsVisibleAlong(-1.9f));
        Assert.False(PostedLimitFilo.IsVisibleAlong(-2.01f));
    }

    [Fact]
    public void Smoke_standstill_tick_freezes_along_and_sticky()
    {
        Assert.True(PostedLimitFilo.ShouldFreezeAtStandstill(0f));
        Assert.True(PostedLimitFilo.ShouldFreezeAtStandstill(0.5f));
        Assert.False(PostedLimitFilo.ShouldFreezeAtStandstill(0.51f));

        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[]
            {
                Board(1, 0f, 50f, 40f),
                Board(2, 0f, 150f, 60f),
            },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f);
        LockTravel(funnel);
        var alongBefore = funnel.AlongAt(0);
        Assert.True(alongBefore > 40f);

        funnel.SetTravel(0f, 0f, 1f, speedKmh: 0f, locoX: 0f, locoY: 0f, locoZ: 51f);
        funnel.Tick(0f, 0f, 51f, 0f, 0f, 1f, speedKmh: 0f);
        Assert.Null(funnel.StickyKmh);
        Assert.Equal(alongBefore, funnel.AlongAt(0));
        Assert.Equal(2, funnel.Count);

        funnel.Tick(0f, 0f, 51f, 0f, 0f, 1f, speedKmh: 20f);
        Assert.Equal(40f, funnel.StickyKmh);
        Assert.Equal(1, funnel.Count);
    }

    [Fact]
    public void Smoke_no_pop_until_direction_locked()
    {
        Assert.False(PostedLimitFilo.ShouldPopOnTick(10f, directionLocked: false));
        Assert.False(PostedLimitFilo.ShouldPopOnTick(1f, directionLocked: false));
        Assert.False(PostedLimitFilo.ShouldPopOnTick(0.2f, directionLocked: true));
        Assert.True(PostedLimitFilo.ShouldPopOnTick(1f, directionLocked: true));
        Assert.True(PostedLimitFilo.ShouldPopOnTick(10f, directionLocked: true));

        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[]
            {
                Board(1, 0f, 50f, 40f),
                Board(2, 0f, 150f, 60f),
            },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f);

        // Past the 40 board but unlocked — no take.
        funnel.Tick(0f, 0f, 51f, 0f, 0f, 1f, speedKmh: 10f);
        Assert.Null(funnel.StickyKmh);
        Assert.Equal(2, funnel.Count);

        LockTravel(funnel);
        funnel.Tick(0f, 0f, 51f, 0f, 0f, 1f, speedKmh: 10f);
        Assert.Equal(40f, funnel.StickyKmh);
        Assert.Equal(1, funnel.Count);
        Assert.True(funnel.LastTakeAlongMeters <= 0f);
    }

    [Fact]
    public void Smoke_9_1_reverse_tt_next_skips_ghost_50_off_path()
    {
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[]
            {
                Board(50, 0f, 346f, 50f),
                Board(40, 0f, 356f, 40f),
            },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f);
        LockTravel(funnel);
        funnel.RequireOnPath = true;
        for (var i = 0; i < funnel.Count; i++)
        {
            funnel.SetOnPath(i, funnel.BoardAt(i).ThroughKmh < 45f);
        }

        var snap = funnel.ToSnapshot();
        Assert.Null(snap.Kmh);
        Assert.Equal(40f, snap.NextKmh);
        Assert.True(snap.NextAlongMeters is float along && along > 350f);
    }

    [Fact]
    public void Smoke_9_1_path_miss_withholds_ghost_next()
    {
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[] { Board(50, 0f, 346f, 50f) },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f);
        LockTravel(funnel);
        funnel.RequireOnPath = true;
        funnel.SetAllOnPath(false);
        var snap = funnel.ToSnapshot();
        Assert.Null(snap.Kmh);
        Assert.Null(snap.NextKmh);
    }

    [Fact]
    public void Smoke_9_1_pop_off_path_does_not_take()
    {
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[]
            {
                Board(50, 0f, 10f, 50f),
                Board(40, 0f, 80f, 40f),
            },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f);
        LockTravel(funnel);
        funnel.RequireOnPath = true;
        for (var i = 0; i < funnel.Count; i++)
        {
            funnel.SetOnPath(i, funnel.BoardAt(i).ThroughKmh < 45f);
        }

        funnel.Tick(0f, 0f, 12f, 0f, 0f, 1f, speedKmh: 20f);
        Assert.Null(funnel.StickyKmh);
        var snap = funnel.ToSnapshot();
        Assert.Equal(40f, snap.NextKmh);
    }

    [Fact]
    public void Symmetric_dual_through_skips_next_and_take()
    {
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[]
            {
                Board(1, 0f, 50f, 40f),
                DualBoard(1398162, 0f, 120f, 50f, 50f),
                DualBoard(1402212, 0f, 250f, 60f, 40f),
            },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f);

        LockTravel(funnel);
        funnel.Tick(0f, 0f, 51f, 0f, 0f, 1f, speedKmh: 20f);
        var snap = funnel.ToSnapshot();
        Assert.Equal(40f, snap.Kmh);
        Assert.Equal(60f, snap.NextKmh);
        Assert.NotEqual(50f, snap.NextKmh);

        funnel.Tick(0f, 0f, 121f, 0f, 0f, 1f, speedKmh: 20f);
        Assert.Equal(40f, funnel.StickyKmh);
    }

    [Fact]
    public void Win5_far_behind_same_rail_does_not_take()
    {
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[] { Board(1, 0f, 50f, 40f) },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f);
        LockTravel(funnel);
        funnel.Tick(0f, 0f, 50f + PostedBoardActiveRoster.TakeAheadMeters + 20f, 0f, 0f, 1f, speedKmh: 20f);
        Assert.Null(funnel.StickyKmh);
        Assert.Equal(0, funnel.Count);
    }

    [Fact]
    public void Win5_off_rail_just_behind_does_not_take()
    {
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[] { Board(1396790, 0f, 50f, 50f) },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f);
        LockTravel(funnel);
        funnel.SetAllOnPath(false);
        funnel.Tick(0f, 0f, 51f, 0f, 0f, 1f, speedKmh: 20f);
        Assert.Null(funnel.StickyKmh);
        Assert.Equal(0, funnel.Count);
    }

    [Fact]
    public void ShouldRefillAfterPop_only_when_count_dropped_and_room()
    {
        Assert.True(PostedLimitFilo.ShouldRefillAfterPop(5, 4, 5, 10));
        Assert.False(PostedLimitFilo.ShouldRefillAfterPop(5, 5, 5, 10));
        Assert.False(PostedLimitFilo.ShouldRefillAfterPop(5, 4, 4, 10));
        Assert.False(PostedLimitFilo.ShouldRefillAfterPop(5, 4, 5, 0));
    }

    [Fact]
    public void ShouldLogAhead_only_on_sticky_or_next_kmh_change()
    {
        Assert.True(PostedBoardTelemetry.ShouldLogAhead(40f, 60f, float.NaN, float.NaN));
        Assert.False(PostedBoardTelemetry.ShouldLogAhead(40f, 60f, 40f, 60f));
        Assert.True(PostedBoardTelemetry.ShouldLogAhead(50f, 60f, 40f, 60f));
        Assert.True(PostedBoardTelemetry.ShouldLogAhead(40f, 80f, 40f, 60f));
    }

    [Fact]
    public void Win6_evaluate_path_abs_next_skips_chord_ghost()
    {
        var corridor = new[]
        {
            new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 400f),
        };
        var onPathForty = new ParsedPostedBoard(
            1398156, 0f, 0f, 120f, 0f, -1f, 1f, 0f, 40f, 40f, false, false);
        var chordGhostFifty = new ParsedPostedBoard(
            1396842, 80f, 0f, 80f, 0f, -1f, 1f, 0f, 50f, 50f, false, false);
        var onPathSixty = new ParsedPostedBoard(
            1402212, 0f, 0f, 250f, 0f, -1f, 1f, 0f, 60f, 40f, true, true);

        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[] { chordGhostFifty, onPathForty, onPathSixty },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f);
        LockTravel(funnel);
        funnel.Evaluate(
            new[] { chordGhostFifty, onPathForty, onPathSixty },
            corridor,
            1,
            0f,
            0f,
            0f,
            0f,
            0f,
            1f,
            speedKmh: 20f);

        var snap = funnel.ToSnapshot();
        Assert.Equal(40f, snap.NextKmh);
        Assert.NotEqual(50f, snap.NextKmh);

        funnel.Evaluate(
            new[] { chordGhostFifty, onPathForty, onPathSixty },
            corridor,
            1,
            0f,
            0f,
            121f,
            0f,
            0f,
            1f,
            speedKmh: 20f);
        var past = funnel.ToSnapshot();
        Assert.Equal(40f, past.Kmh);
        Assert.Equal(60f, past.NextKmh);
        Assert.NotEqual(50f, past.NextKmh);
    }

    /// <summary>
    /// Win 7 cab: on-path 60 with unknown facing must Next/take; a nearer
    /// strongly-away 70 must not steal (HTP 40→60).
    /// </summary>
    [Fact]
    public void Win7_on_path_unknown_facing_60_governs_away_70_does_not()
    {
        var corridor = new[]
        {
            new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 400f),
        };
        var seventyAway = new ParsedPostedBoard(
            1402324, 0f, 0f, 100f, 0f, 1f, 1f, 0f, 70f, 70f, false, false);
        var sixtyUnknown = new ParsedPostedBoard(
            1402212, 0f, 0f, 180f, 1f, 0f, 0f, 1f, 60f, 40f, true, true);
        Assert.True(PostedBoardHarvestCodec.FacesAway(in seventyAway, 0f, 1f));
        Assert.False(PostedBoardHarvestCodec.FacesTravel(in sixtyUnknown, 0f, 1f));
        Assert.False(PostedBoardHarvestCodec.FacesAway(in sixtyUnknown, 0f, 1f));

        var roster = new[] { seventyAway, sixtyUnknown };
        var funnel = new PostedLimitFunnel();
        funnel.Warm(roster, 0f, 0f, 0f, 0f, 0f, 1f);
        LockTravel(funnel);
        funnel.Evaluate(roster, corridor, 1, 0f, 0f, 0f, 0f, 0f, 1f, speedKmh: 20f);
        Assert.Equal(60f, funnel.ToSnapshot().NextKmh);
        Assert.NotEqual(70f, funnel.ToSnapshot().NextKmh);

        funnel.Evaluate(roster, corridor, 1, 0f, 0f, 181f, 0f, 0f, 1f, speedKmh: 20f);
        Assert.Equal(60f, funnel.StickyKmh);
    }

    [Fact]
    public void Win6_evaluate_and_snapshot_do_not_allocate()
    {
        var corridor = new[]
        {
            new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 400f),
        };
        var roster = new[]
        {
            Board(1, 0f, 80f, 40f),
            Board(2, 0f, 200f, 60f),
        };
        var funnel = new PostedLimitFunnel();
        funnel.Warm(roster, 0f, 0f, 0f, 0f, 0f, 1f);
        LockTravel(funnel);
        var cache = default(PostedLimitCache);
        funnel.Evaluate(roster, corridor, 1, 0f, 0f, 0f, 0f, 0f, 1f, speedKmh: 20f);
        funnel.PublishIfChanged(ref cache);

        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            funnel.Evaluate(
                roster,
                corridor,
                1,
                0f,
                0f,
                10f + (i % 50),
                0f,
                0f,
                1f,
                speedKmh: 20f);
            funnel.PublishIfChanged(ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private static void LockTravel(PostedLimitFunnel funnel) =>
        funnel.SetTravel(0f, 0f, 1f, speedKmh: 20f, locoX: 0f, locoY: 0f, locoZ: 0f);

    private static ParsedPostedBoard Board(int id, float x, float z, float kmh) =>
        new ParsedPostedBoard(
            id,
            x,
            0f,
            z,
            0f,
            -1f,
            1f,
            0f,
            kmh,
            kmh,
            false,
            false);

    private static ParsedPostedBoard DualBoard(
        int id,
        float x,
        float z,
        float throughKmh,
        float divergeKmh,
        bool junctionNearby = true) =>
        new ParsedPostedBoard(
            id,
            x,
            0f,
            z,
            0f,
            -1f,
            1f,
            0f,
            throughKmh,
            divergeKmh,
            isDual: true,
            junctionNearby);
}
