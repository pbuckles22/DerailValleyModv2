using YardMasterSuite.Core;
using Xunit.Abstractions;

namespace YardMasterSuite.Tests;

/// <summary>
/// HTP CP1 — Physics tick loop for <b>9.1</b>. Straight fixture; Unity rigidbody
/// stays Tier 2. Named after the cab smoke: hold ~25 km/h, never dump air.
/// Change-only try/applied lines go to <see cref="ITestOutputHelper"/> (shown on fail).
/// </summary>
public class HtpPidStraightHoldTests
{
    private const float Dt = 0.05f;
    private const int HoldTicks = 400;
    private readonly HtpPidTrace _trace;

    public HtpPidStraightHoldTests(ITestOutputHelper output) => _trace = new HtpPidTrace(output);

    [Fact]
    public void Smoke_9_1_pid_holds_25_on_straight()
    {
        var speed = 0f;
        var along = 0f;
        var throttle = 0f;
        var independent = 0f;
        var maxThr = 0f;
        var state = default(PidSpeedState);
        for (var i = 0; i < HoldTicks; i++)
        {
            var cmd = Tick(
                speed,
                throttle,
                independent,
                request: PidSpeedTarget.DefaultRequestKmh,
                posted: null,
                armed: true,
                derail: false,
                ceiling: 1f,
                ref state);
            Assert.True(cmd.Active);
            Assert.Equal(25f, cmd.TargetKmh);
            CabPlant(cmd, ref speed, ref along, ref throttle, ref independent);
            Assert.True(PidSpeedNotch.IsExact(throttle));
            if (throttle > maxThr)
            {
                maxThr = throttle;
            }
        }

        Assert.InRange(speed, 20f, 27f);
        Assert.True(along > 50f);
        Assert.True(maxThr > PidSpeedNotch.Step * 2f - 1e-4f);
        Assert.True(independent <= PidSpeedHold.OverspeedIndependent + 1e-3f);
    }

    [Fact]
    public void Smoke_9_1_posted_caps_target_below_request()
    {
        var speed = 0f;
        var along = 0f;
        var throttle = 0f;
        var independent = 0f;
        var state = default(PidSpeedState);
        for (var i = 0; i < HoldTicks; i++)
        {
            var cmd = Tick(
                speed,
                throttle,
                independent,
                request: 40f,
                posted: 25f,
                armed: true,
                derail: false,
                ceiling: 1f,
                ref state);
            Assert.Equal(25f, cmd.TargetKmh);
            CabPlant(cmd, ref speed, ref along, ref throttle, ref independent);
            Assert.True(PidSpeedNotch.IsExact(throttle));
        }

        Assert.InRange(speed, 20f, 27f);
    }

    [Fact]
    public void Smoke_9_1_never_dumps_air_on_overspeed_trim()
    {
        var speed = 40f;
        var along = 0f;
        var throttle = 0.9f;
        var independent = 0.10f;
        var state = default(PidSpeedState);
        for (var i = 0; i < HoldTicks; i++)
        {
            var cmd = Tick(
                speed,
                throttle,
                independent,
                request: 25f,
                posted: null,
                armed: true,
                derail: false,
                ceiling: 1f,
                ref state);
            if (speed > 25f + PidSpeedHold.OverspeedBandKmh)
            {
                Assert.True(cmd.DesiredIndependent >= PidSpeedHold.OverspeedIndependent - 1e-4f);
                Assert.Equal(0f, cmd.DesiredThrottle);
            }

            CabPlant(cmd, ref speed, ref along, ref throttle, ref independent);
        }

        Assert.True(speed < 30f);
    }

    [Fact]
    public void Smoke_9_1_yields_to_7_5_derail_net()
    {
        var state = default(PidSpeedState);
        var cmd = Tick(
            speed: 20f,
            throttle: 0.85f,
            independent: 0.05f,
            request: 25f,
            posted: null,
            armed: true,
            derail: true,
            ceiling: 1f,
            ref state);
        Assert.False(cmd.Active);
        Assert.Equal(0.85f, cmd.DesiredThrottle);
        Assert.Equal(0.05f, cmd.DesiredIndependent);
    }

    [Fact]
    public void Smoke_9_1_unarmed_without_maps_leg_does_not_write()
    {
        Assert.False(PidSpeedArm.IsArmed(hasMapsDest: false, switchListActiveIncomplete: false, facingReady: false));
        Assert.False(PidSpeedArm.IsArmed(hasMapsDest: true, switchListActiveIncomplete: false, facingReady: false));
        Assert.True(PidSpeedArm.IsArmed(hasMapsDest: true, switchListActiveIncomplete: false, facingReady: true));
        Assert.False(PidSpeedArm.IsArmed(hasMapsDest: false, switchListActiveIncomplete: true, facingReady: true));

        var state = default(PidSpeedState);
        var cmd = Tick(
            speed: 0f,
            throttle: 0.4f,
            independent: 0.2f,
            request: 25f,
            posted: null,
            armed: false,
            derail: false,
            ceiling: 1f,
            ref state);
        Assert.False(cmd.Active);
        Assert.Equal(0.4f, cmd.DesiredThrottle);
        Assert.Equal(0.2f, cmd.DesiredIndependent);
    }

    [Fact]
    public void Smoke_9_1_thermal_ceiling_caps_throttle()
    {
        var state = default(PidSpeedState);
        var throttle = 0.9f;
        PidSpeedCommand cmd = default;
        for (var i = 0; i < 80; i++)
        {
            cmd = Tick(
                speed: 0f,
                throttle,
                independent: 0f,
                request: 25f,
                posted: null,
                armed: true,
                derail: false,
                ceiling: ThermalThrottleCap.DefaultMaxWhenCritical,
                ref state);
            throttle = cmd.DesiredThrottle;
        }

        Assert.True(cmd.Active);
        Assert.True(cmd.DesiredThrottle <= ThermalThrottleCap.DefaultMaxWhenCritical + 1e-4f);
    }

    [Fact]
    public void Tick_does_not_allocate_on_hot_path()
    {
        var state = default(PidSpeedState);
        Tick(0f, 0.2f, 0f, 25f, 40f, true, false, 1f, ref state);
        var throttle = 0.2f;
        var independent = 0.05f;
        var speed = 20f;
        var along = 0f;
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            var cmd = Tick(20f, throttle, independent, 25f, 40f, true, false, 1f, ref state);
            PidSpeedCab.Apply(cmd, true, ref throttle, ref independent);
            PidSpeedPlant.Step(ref speed, ref along, throttle, independent, Dt, LocoTypeId.De2);
            PidSpeedTarget.Resolve(25f, 40f);
            PidSpeedArm.IsArmed(true, false, facingReady: true);
            PidSpeedFacing.FacingReady(false, false, false);
            PidSpeedGear.Matches(PidSpeedGear.ForwardValue, needsReverse: false);
            PidSpeedGear.LabelNeedsReverse(SwitchListDriveFacing.Forward);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void Smoke_9_1_three_gate_applies_when_armed()
    {
        var calls = 0;
        var result = ThreeGate.TryApply(
            ThreeGateWrite.Integrity(worldActive: true, actorPresent: true),
            ThreeGateWrite.StateRegistry(controlPresent: true),
            ThreeGateWrite.Safety(overlayClear: true, controlNotBlocked: true),
            () =>
            {
                calls++;
                return true;
            });
        Assert.True(result.Applied);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Smoke_9_1_neutral_does_not_notch_throttle_until_step_reverser()
    {
        var state = default(PidSpeedState);
        var cmd = Tick(
            speed: 0f,
            throttle: 0f,
            independent: 0f,
            request: 25f,
            posted: null,
            armed: true,
            derail: false,
            ceiling: 1f,
            ref state,
            reverser: ProximityTravelDirectionGate.NeutralValue,
            legNeedsReverse: true);
        Assert.True(cmd.Active);
        Assert.True(cmd.GearPending);
        Assert.Equal(PidSpeedGear.ReverseValue, cmd.DesiredReverser);
        Assert.Equal(0f, cmd.DesiredThrottle);

        cmd = Tick(
            speed: 0f,
            throttle: 0f,
            independent: 0f,
            request: 25f,
            posted: null,
            armed: true,
            derail: false,
            ceiling: 1f,
            ref state,
            reverser: PidSpeedGear.ReverseValue,
            legNeedsReverse: true);
        Assert.False(cmd.GearPending);
        Assert.InRange(cmd.DesiredThrottle, PidSpeedHold.MinNotch - 1e-4f, PidSpeedHold.MinNotch + 1e-4f);
    }

    [Fact]
    public void Smoke_9_1_dest_set_before_latch_does_not_arm()
    {
        Assert.False(PidSpeedFacing.FacingReady(
            switchListActive: false,
            pinLatched: false,
            hasPlan: false));
        Assert.True(PidSpeedFacing.FacingReady(false, pinLatched: true, hasPlan: false));
        Assert.True(PidSpeedFacing.LegNeedsReverse(
            pinStepActive: true,
            pinStepReverse: true,
            destBehind: false));
        Assert.True(PidSpeedFacing.LegNeedsReverse(
            pinStepActive: true,
            pinStepReverse: true,
            destBehind: true));
        Assert.True(PidSpeedFacing.LegNeedsReverse(
            pinStepActive: false,
            pinStepReverse: true,
            destBehind: true));
        Assert.False(PidSpeedFacing.LegNeedsReverse(
            pinStepActive: false,
            pinStepReverse: true,
            destBehind: false));
    }

    /// <summary>
    /// Cab 2.9.1.31: reverse to pin, live pin-behind flipped at frog, PID wrote
    /// rev=100 at 25 then takeoff F away from dest. Latch reverse until pin
    /// step ends; after CLEARED dest-behind stays reverse.
    /// </summary>
    [Fact]
    public void Smoke_frog_pin_behind_flip_does_not_drop_latch_reverse()
    {
        Assert.True(PidSpeedFacing.LegNeedsReverse(
            pinStepActive: true,
            pinStepReverse: true,
            destBehind: false));
    }

    [Fact]
    public void Smoke_9_1_throttle_ramps_not_slams()
    {
        var state = default(PidSpeedState);
        var cmd = Tick(
            speed: 0f,
            throttle: 0f,
            independent: 0f,
            request: 25f,
            posted: null,
            armed: true,
            derail: false,
            ceiling: 1f,
            ref state);
        Assert.InRange(
            cmd.DesiredThrottle,
            PidSpeedHold.MinNotch - 1e-4f,
            PidSpeedHold.MinNotch + 0.001f);
        Assert.True(cmd.DesiredThrottle < 0.5f);
    }

    /// <summary>
    /// Cab: motors=Dead then Numpad . at 24 km/h → 9% same tick, re-blow.
    /// Latch WaitCrawl: fuse restore at rolling speed stays thr=0 until crawl.
    /// </summary>
    [Fact]
    public void Smoke_motors_dead_snaps_throttle_off_fuse_reset_starts_from_zero()
    {
        var state = default(PidSpeedState);
        state.CommandedThrottle = 0.27f;
        var dead = Tick(
            speed: 24f,
            throttle: 0.27f,
            independent: 0f,
            request: 25f,
            posted: 120f,
            armed: true,
            derail: false,
            ceiling: 0f,
            ref state);
        Assert.Equal(0f, dead.DesiredThrottle);
        Assert.Equal(0f, state.CommandedThrottle);
        Assert.True(state.WaitCrawl);
        Assert.False(
            PidSpeedTelemetry.WantsThrottle(true, dead.GearPending, dead.BrakePending, dead.DesiredThrottle));

        var liveRolling = Tick(
            speed: 24f,
            throttle: 0f,
            independent: 0f,
            request: 25f,
            posted: 120f,
            armed: true,
            derail: false,
            ceiling: 1f,
            ref state);
        Assert.Equal(0f, liveRolling.DesiredThrottle);
        Assert.True(state.WaitCrawl);

        var liveCrawl = Tick(
            speed: 1.5f,
            throttle: 0f,
            independent: 0f,
            request: 25f,
            posted: 120f,
            armed: true,
            derail: false,
            ceiling: 1f,
            ref state);
        Assert.False(state.WaitCrawl);
        Assert.True(liveCrawl.DesiredThrottle > 0f);
    }

    /// <summary>
    /// Cab: CLEARED flips reverse at 25 km/h then first-notch 9% blows TMS.
    /// WaitCrawl holds thr=0 until crawl. Reverser still forward, step wants reverse.
    /// </summary>
    [Fact]
    public void Smoke_gear_pending_at_speed_latches_wait_crawl()
    {
        var state = default(PidSpeedState);
        var cmd = Tick(
            speed: 25f,
            throttle: 0.27f,
            independent: 0f,
            request: 25f,
            posted: null,
            armed: true,
            derail: false,
            ceiling: 1f,
            ref state,
            reverser: PidSpeedGear.ForwardValue,
            legNeedsReverse: true);
        Assert.Equal(0f, cmd.DesiredThrottle);
        Assert.True(state.WaitCrawl);
        Assert.True(cmd.GearPending);
        Assert.Equal(PidSpeedGear.ForwardValue, cmd.DesiredReverser);
    }

    /// <summary>
    /// Cab 2.9.1.31: wait-crawl wrote rev=100 while still reverse at 25.
    /// Hold the current lever until crawl; do not plug F at speed.
    /// </summary>
    [Fact]
    public void Smoke_wait_crawl_holds_current_reverser_does_not_plug_forward()
    {
        var state = default(PidSpeedState);
        var cmd = Tick(
            speed: 25f,
            throttle: 0f,
            independent: 0f,
            request: 25f,
            posted: null,
            armed: true,
            derail: false,
            ceiling: 1f,
            ref state,
            reverser: PidSpeedGear.ReverseValue,
            legNeedsReverse: false);
        Assert.True(state.WaitCrawl);
        Assert.Equal(0f, cmd.DesiredThrottle);
        Assert.Equal(PidSpeedGear.ReverseValue, cmd.DesiredReverser);
    }

    /// <summary>
    /// Cab debt: thr 9→100 by ~10 km/h → wheel slip / motors=Dead. Slew must
    /// keep applied throttle well below full when speed first hits 10.
    /// </summary>
    [Fact]
    public void Smoke_9_1_takeoff_thr_not_100_by_10_kmh()
    {
        var speed = 0f;
        var along = 0f;
        var throttle = 0f;
        var independent = 0f;
        var state = default(PidSpeedState);
        var thrAt10 = -1f;
        for (var i = 0; i < 2000 && thrAt10 < 0f; i++)
        {
            var cmd = Tick(
                speed,
                throttle,
                independent,
                request: 25f,
                posted: null,
                armed: true,
                derail: false,
                ceiling: 1f,
                ref state);
            CabPlant(cmd, ref speed, ref along, ref throttle, ref independent);
            if (speed >= 10f)
            {
                thrAt10 = throttle;
            }
        }

        Assert.True(thrAt10 >= 0f, "never reached 10 km/h");
        Assert.True(
            thrAt10 < 0.55f,
            "takeoff thr at 10 km/h was " + thrAt10 + " (want &lt; 0.55, not 1.0)");
    }

    /// <summary>
    /// Cab debt: snappy thr↔indy at hold. Inside OverspeedBand, coast — thr idle,
    /// no indy raise (lets TMs cool; avoids motors=Dead after CLEARED).
    /// </summary>
    [Fact]
    public void Smoke_9_1_hold_deadband_coasts_no_indy()
    {
        var state = default(PidSpeedState);
        state.CommandedThrottle = 0.36f;
        var cmd = Tick(
            speed: 25f,
            throttle: 0.36f,
            independent: 0f,
            request: 25f,
            posted: null,
            armed: true,
            derail: false,
            ceiling: 1f,
            ref state,
            reverser: PidSpeedGear.ReverseValue,
            legNeedsReverse: true);
        Assert.Equal(0f, cmd.DesiredThrottle);
        Assert.True(cmd.DesiredIndependent < PidSpeedHold.OverspeedIndependent - 1e-3f);

        cmd = Tick(
            speed: 25f + PidSpeedHold.OverspeedBandKmh - 0.1f,
            throttle: 0.18f,
            independent: 0f,
            request: 25f,
            posted: null,
            armed: true,
            derail: false,
            ceiling: 1f,
            ref state,
            reverser: PidSpeedGear.ReverseValue,
            legNeedsReverse: true);
        Assert.Equal(0f, cmd.DesiredThrottle);
        Assert.True(cmd.DesiredIndependent < PidSpeedHold.OverspeedIndependent - 1e-3f);

        cmd = Tick(
            speed: 25f + PidSpeedHold.OverspeedBandKmh + 0.5f,
            throttle: 0.18f,
            independent: 0f,
            request: 25f,
            posted: null,
            armed: true,
            derail: false,
            ceiling: 1f,
            ref state,
            reverser: PidSpeedGear.ReverseValue,
            legNeedsReverse: true);
        Assert.Equal(0f, cmd.DesiredThrottle);
        Assert.Equal(PidSpeedHold.OverspeedIndependent, cmd.DesiredIndependent);
    }

    /// <summary>
    /// Hold walk: once near target, must not thr↔indy chatter (motors=Dead root).
    /// </summary>
    [Fact]
    public void Smoke_9_1_hold_near_target_no_thr_indy_chatter()
    {
        var speed = 0f;
        var along = 0f;
        var throttle = 0f;
        var independent = 0f;
        var state = default(PidSpeedState);
        var flips = 0;
        var priorThrOn = false;
        var priorIndyOn = false;
        var sawHold = false;
        for (var i = 0; i < 800; i++)
        {
            var cmd = Tick(
                speed,
                throttle,
                independent,
                request: 25f,
                posted: null,
                armed: true,
                derail: false,
                ceiling: 1f,
                ref state);
            CabPlant(cmd, ref speed, ref along, ref throttle, ref independent);
            if (speed < 20f)
            {
                continue;
            }

            sawHold = true;
            var thrOn = throttle >= PidSpeedHold.MinNotch - 1e-4f;
            var indyOn = independent >= PidSpeedHold.OverspeedIndependent - 1e-3f;
            if ((thrOn && priorIndyOn) || (indyOn && priorThrOn))
            {
                flips++;
            }

            priorThrOn = thrOn;
            priorIndyOn = indyOn;
        }

        Assert.True(sawHold);
        Assert.InRange(speed, 20f, 27f);
        Assert.True(flips <= 4, "thr↔indy flips=" + flips);
    }

    [Fact]
    public void Smoke_9_1_hold_after_air_off_writes_first_notch()
    {
        var state = default(PidSpeedState);
        var indy = 1f;
        PidSpeedCommand cmd = default;
        for (var i = 0; i < 200 && indy > PidSpeedHold.BrakeReleaseEpsilon; i++)
        {
            cmd = Tick(
                speed: 0f,
                throttle: 0f,
                independent: indy,
                request: 25f,
                posted: null,
                armed: true,
                derail: false,
                ceiling: 1f,
                ref state,
                reverser: PidSpeedGear.ReverseValue,
                legNeedsReverse: true);
            Assert.True(cmd.BrakePending);
            Assert.Equal(0f, cmd.DesiredThrottle);
            indy = cmd.DesiredIndependent;
        }

        Assert.True(indy <= PidSpeedHold.BrakeReleaseEpsilon);
        cmd = Tick(
            speed: 0f,
            throttle: 0f,
            independent: 0f,
            request: 25f,
            posted: null,
            armed: true,
            derail: false,
            ceiling: 1f,
            ref state,
            reverser: PidSpeedGear.ReverseValue,
            legNeedsReverse: true);
        Assert.False(cmd.BrakePending);
        Assert.InRange(
            cmd.DesiredThrottle,
            PidSpeedHold.MinNotch - 1e-4f,
            PidSpeedHold.MinNotch + 0.001f);
    }

    [Fact]
    public void Smoke_9_1_releases_independent_before_throttle()
    {
        var state = default(PidSpeedState);
        var indy = 1f;
        PidSpeedCommand cmd = default;
        for (var i = 0; i < 8; i++)
        {
            cmd = Tick(
                speed: 0f,
                throttle: 0f,
                independent: indy,
                request: 25f,
                posted: null,
                armed: true,
                derail: false,
                ceiling: 1f,
                ref state,
                reverser: PidSpeedGear.ReverseValue,
                legNeedsReverse: true);
            Assert.True(cmd.BrakePending);
            Assert.Equal(0f, cmd.DesiredThrottle);
            Assert.True(cmd.DesiredIndependent < indy || i == 0);
            indy = cmd.DesiredIndependent;
        }

        Assert.True(indy < 1f - (PidSpeedHold.BrakeReleasePerSecond * Dt));
        Assert.True(indy > 0.5f);
    }

    [Fact]
    public void Smoke_9_1_releases_train_before_throttle()
    {
        var state = default(PidSpeedState);
        var cmd = Tick(
            speed: 0f,
            throttle: 0f,
            independent: 0f,
            request: 25f,
            posted: null,
            armed: true,
            derail: false,
            ceiling: 1f,
            ref state,
            reverser: PidSpeedGear.ReverseValue,
            legNeedsReverse: true,
            trainBrake: 1f);
        Assert.True(cmd.BrakePending);
        Assert.Equal(0f, cmd.DesiredThrottle);
        Assert.True(cmd.DesiredTrain < 1f);
        Assert.Equal(0f, cmd.DesiredIndependent);
    }

    [Fact]
    public void Smoke_9_1_gear_holds_air_until_reverser_matches()
    {
        var state = default(PidSpeedState);
        var cmd = Tick(
            speed: 0f,
            throttle: 0f,
            independent: 1f,
            request: 25f,
            posted: null,
            armed: true,
            derail: false,
            ceiling: 1f,
            ref state,
            reverser: ProximityTravelDirectionGate.NeutralValue,
            legNeedsReverse: true,
            trainBrake: 0.8f);
        Assert.True(cmd.GearPending);
        Assert.False(cmd.BrakePending);
        Assert.Equal(1f, cmd.DesiredIndependent);
        Assert.Equal(0.8f, cmd.DesiredTrain);
        Assert.Equal(0f, cmd.DesiredThrottle);
    }

    [Fact]
    public void Smoke_9_1_snapped_throttle_zero_still_ramps()
    {
        var state = default(PidSpeedState);
        PidSpeedCommand cmd = default;
        for (var i = 0; i < 40; i++)
        {
            cmd = Tick(
                speed: 0f,
                throttle: 0f,
                independent: 0f,
                request: 25f,
                posted: null,
                armed: true,
                derail: false,
                ceiling: 1f,
                ref state,
                reverser: PidSpeedGear.ReverseValue,
                legNeedsReverse: true);
            Assert.False(cmd.BrakePending);
        }

        Assert.True(cmd.DesiredThrottle > 0.15f);
        Assert.True(cmd.DesiredThrottle < 0.5f);
    }

    [Fact]
    public void Smoke_9_1_overspeed_idles_and_raises_indy()
    {
        var state = default(PidSpeedState);
        var cmd = Tick(
            speed: 32f,
            throttle: 0.4f,
            independent: 0f,
            request: 25f,
            posted: null,
            armed: true,
            derail: false,
            ceiling: 1f,
            ref state,
            reverser: PidSpeedGear.ReverseValue,
            legNeedsReverse: true);
        Assert.False(cmd.GearPending);
        Assert.Equal(0f, cmd.DesiredThrottle);
        Assert.Equal(PidSpeedHold.OverspeedIndependent, cmd.DesiredIndependent);
    }

    [Fact]
    public void Smoke_9_1_gear_pending_does_not_coast_above_target()
    {
        var state = default(PidSpeedState);
        var cmd = Tick(
            speed: 32f,
            throttle: 0.4f,
            independent: 0f,
            request: 25f,
            posted: null,
            armed: true,
            derail: false,
            ceiling: 1f,
            ref state,
            reverser: ProximityTravelDirectionGate.NeutralValue,
            legNeedsReverse: true);
        Assert.True(cmd.GearPending);
        Assert.Equal(0f, cmd.DesiredThrottle);
        Assert.Equal(PidSpeedHold.OverspeedIndependent, cmd.DesiredIndependent);
    }

    [Fact]
    public void Smoke_9_1_thr_off_then_gear_does_not_coast_to_55()
    {
        var speed = 28f;
        var along = 0f;
        var throttle = 0.09f;
        var independent = 0f;
        var state = default(PidSpeedState);
        var overBand = 25f + PidSpeedHold.OverspeedBandKmh;
        for (var i = 0; i < 40; i++)
        {
            var cmd = Tick(
                speed,
                throttle,
                independent,
                request: 25f,
                posted: null,
                armed: true,
                derail: false,
                ceiling: 1f,
                ref state,
                reverser: PidSpeedGear.ReverseValue,
                legNeedsReverse: true);
            Assert.False(cmd.GearPending);
            if (speed > overBand)
            {
                Assert.Equal(0f, cmd.DesiredThrottle);
                Assert.Equal(PidSpeedHold.OverspeedIndependent, cmd.DesiredIndependent);
            }
            Assert.True(
                independent + 1e-4f >= cmd.DesiredIndependent
                || PidSpeedWrite.Independent(
                    independent,
                    cmd.DesiredIndependent,
                    cmd.GearPending,
                    cmd.BrakePending));
            CabPlant(cmd, ref speed, ref along, ref throttle, ref independent);
        }

        Assert.True(speed < 28f);
        for (var i = 0; i < 40; i++)
        {
            var cmd = Tick(
                speed,
                throttle,
                independent,
                request: 25f,
                posted: null,
                armed: true,
                derail: false,
                ceiling: 1f,
                ref state,
                reverser: ProximityTravelDirectionGate.NeutralValue,
                legNeedsReverse: true);
            Assert.True(cmd.GearPending);
            if (speed > overBand)
            {
                Assert.Equal(0f, cmd.DesiredThrottle);
                Assert.True(cmd.DesiredIndependent >= PidSpeedHold.OverspeedIndependent - 1e-4f);
            }
            Assert.True(
                independent + 1e-4f >= cmd.DesiredIndependent
                || PidSpeedWrite.Independent(
                    independent,
                    cmd.DesiredIndependent,
                    cmd.GearPending,
                    cmd.BrakePending));
            CabPlant(cmd, ref speed, ref along, ref throttle, ref independent);
        }

        Assert.True(speed < 40f);
    }

    [Fact]
    public void Smoke_9_1_gear_pending_plant_does_not_coast_to_54()
    {
        var speed = 32f;
        var along = 0f;
        var throttle = 0.4f;
        var independent = 0f;
        var state = default(PidSpeedState);
        for (var i = 0; i < 80; i++)
        {
            var cmd = Tick(
                speed,
                throttle,
                independent,
                request: 25f,
                posted: null,
                armed: true,
                derail: false,
                ceiling: 1f,
                ref state,
                reverser: ProximityTravelDirectionGate.NeutralValue,
                legNeedsReverse: true);
            Assert.True(cmd.GearPending);
            Assert.Equal(0f, cmd.DesiredThrottle);
            Assert.True(cmd.DesiredIndependent > 0f);
            CabPlant(cmd, ref speed, ref along, ref throttle, ref independent);
        }

        Assert.True(speed < 32f);
        Assert.True(speed < 40f);
    }

    [Fact]
    public void Smoke_9_1_sw_tt_log_cleared_at_33_does_not_run_through()
    {
        var snap = HtpFixtures.LoadCorridor();
        var dumped = HtpFixtures.DumpedPose(in snap, HtpSwTurntableLiveDumpTests.SawtoothPin);
        var fx = dumped.LocoForwardX;
        var fz = dumped.LocoForwardZ;
        var mag = (float)Math.Sqrt((fx * fx) + (fz * fz));
        Assert.True(mag > 1e-6f);
        fx /= mag;
        fz /= mag;
        var latchedReverse = dumped.PinIsBehind;
        var dirX = latchedReverse ? -fx : fx;
        var dirZ = latchedReverse ? -fz : fz;
        var speed = 0f;
        var along = 0f;
        var throttle = 0f;
        var independent = 1f;
        var state = default(PidSpeedState);
        var maxAfter25 = 0f;
        var saw25 = false;
        var pinCleared = false;
        var prior = RouteClearancePhase.Idle;
        var dumpedPhase = RouteCorridorDrive.EvaluatePose(
            RouteClearancePhase.Idle,
            in dumped,
            travelUsesReverse: latchedReverse);
        Assert.NotEqual(RouteClearancePhase.Cleared, dumpedPhase.Phase);
        for (var i = 0; i < 4000; i++)
        {
            var pose = new RouteCorridorPose(
                dumped.NoseX + (along * dirX),
                dumped.NoseZ + (along * dirZ),
                dumped.PinX,
                dumped.PinZ,
                dumped.LocoForwardX,
                dumped.LocoForwardZ,
                dumped.ConsistLengthM);
            var d = RouteCorridorDrive.EvaluatePose(prior, in pose, travelUsesReverse: latchedReverse);
            pinCleared = d.Phase == RouteClearancePhase.Cleared;
            prior = d.Phase;

            var cmd = Tick(
                speed,
                throttle,
                independent,
                request: 25f,
                posted: null,
                armed: true,
                derail: false,
                ceiling: 1f,
                ref state,
                reverser: pinCleared
                    ? ProximityTravelDirectionGate.NeutralValue
                    : PidSpeedGear.ReverseValue,
                legNeedsReverse: !pinCleared);
            CabPlant(cmd, ref speed, ref along, ref throttle, ref independent);
            Assert.True(PidSpeedNotch.IsExact(throttle));
            if (speed >= 25f)
            {
                saw25 = true;
                if (speed > maxAfter25)
                {
                    maxAfter25 = speed;
                }
            }
        }

        Assert.True(pinCleared);
        Assert.True(saw25);
        Assert.True(maxAfter25 < 28f);
        Assert.True(along > 50f);
    }

    [Fact]
    public void Smoke_9_1_past_switch_reverse_then_forward_on_next()
    {
        var past = SwitchListDriveFacing.FormatDriveLabel(true, "Past switch", "SW-B4L")
            + " until CLEARED";
        var next = SwitchListDriveFacing.FormatDriveLabel(false, "Transit", "#Y-#S1774#T");
        Assert.True(PidSpeedGear.LabelNeedsReverse(past));
        Assert.False(PidSpeedGear.LabelNeedsReverse(next));
        Assert.True(PidSpeedGear.LegNeedsReverse(past, destOnlyPinReverse: false));
        Assert.False(PidSpeedGear.LegNeedsReverse(next, destOnlyPinReverse: true));

        var state = default(PidSpeedState);
        var reverseStep = Tick(
            0f,
            0.4f,
            0f,
            25f,
            null,
            true,
            false,
            1f,
            ref state,
            ProximityTravelDirectionGate.NeutralValue,
            PidSpeedGear.LegNeedsReverse(past, false));
        Assert.True(reverseStep.GearPending);
        Assert.Equal(PidSpeedGear.ReverseValue, reverseStep.DesiredReverser);
        Assert.True(reverseStep.DesiredThrottle <= 0.4f);

        var afterNext = Tick(
            10f,
            0.4f,
            0f,
            25f,
            null,
            true,
            false,
            1f,
            ref state,
            PidSpeedGear.ReverseValue,
            PidSpeedGear.LegNeedsReverse(next, false));
        Assert.True(afterNext.GearPending);
        Assert.True(state.WaitCrawl);
        Assert.Equal(PidSpeedGear.ReverseValue, afterNext.DesiredReverser);
        Assert.Equal(0f, afterNext.DesiredThrottle);

        var atCrawl = Tick(
            1.5f,
            0f,
            0f,
            25f,
            null,
            true,
            false,
            1f,
            ref state,
            PidSpeedGear.ReverseValue,
            PidSpeedGear.LegNeedsReverse(next, false));
        Assert.False(state.WaitCrawl);
        Assert.True(atCrawl.GearPending);
        Assert.Equal(PidSpeedGear.ForwardValue, atCrawl.DesiredReverser);
        Assert.Equal(0f, atCrawl.DesiredThrottle);
    }

    private void CabPlant(
        in PidSpeedCommand cmd,
        ref float speed,
        ref float along,
        ref float throttle,
        ref float independent)
    {
        var want = PidSpeedTelemetry.WantsThrottle(
            armed: true,
            cmd.GearPending,
            cmd.BrakePending,
            cmd.DesiredThrottle);
        PidSpeedCab.Apply(cmd, want, ref throttle, ref independent);
        _trace.Tick(in cmd, throttle, independent, speed);
        PidSpeedPlant.Step(ref speed, ref along, throttle, independent, Dt, LocoTypeId.De2);
    }

    private static PidSpeedCommand Tick(
        float speed,
        float throttle,
        float independent,
        float request,
        float? posted,
        bool armed,
        bool derail,
        float ceiling,
        ref PidSpeedState state,
        float reverser = 1f,
        bool legNeedsReverse = false,
        float trainBrake = 0f) =>
        PidSpeedHold.Tick(
            new PidSpeedInput(
                Dt,
                speed,
                request,
                posted,
                throttle,
                independent,
                armed,
                derail,
                ceiling,
                reverser,
                legNeedsReverse,
                trainBrake),
            ref state);
}
