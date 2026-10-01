using System.Collections.Generic;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cab 2.16.30 / Gemini Steps 5–7 as HTP locks on pin 1002848 (SL-55 step 6).
/// Spec only — no toy pathfinder. Along-track tail past the frog, not a
/// straight-line lead-car forward dot.
/// </summary>
[Collection("StaticSessions")]
public class HtpSawtoothPin1002848Tests
{
    public const string Pin = "1002848";
    public const string Approach = "#Y-#S1263#T";
    public const string Stem = "#Y-#S526#T";
    public const string NearSide = "#Y-#S241#T";
    public const float Consist44 = 44f;
    public const float Frog = RouteClearanceEval.DefaultFrogEnvelopeM;

    /// <summary>
    /// Synthetic corridor mirroring B1S → C4S through 1002848. Stem length
    /// from graph-sw-2026-09-01 track 987770 (~19.34 m).
    /// </summary>
    private static readonly string[] Hops =
    {
        "SW-B1S",
        NearSide,
        Approach,
        Stem,
        "#Y-#S989#T",
        "SW-C4S",
    };

    private static readonly float[] Lengths =
    {
        40f,
        15f,
        10f,
        19.34f,
        50f,
        80f,
    };

    private const int ApproachIndex = 2;

    public HtpSawtoothPin1002848Tests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_21630_step6_44m_pull_through_1002848_stays_at_switch_until_tail_12m_on_stem()
    {
        var total = 0f;
        for (var i = 0; i < Lengths.Length; i++)
        {
            total += Lengths[i];
        }

        var sawCleared = false;
        for (var noseAlong = 0f; noseAlong <= total + 1f; noseAlong += 2f)
        {
            LocateNose(noseAlong, out var leadHop, out var into);
            Assert.True(
                RouteTailAlongTrack.TryTailPastPin(
                    Hops,
                    Lengths,
                    leadHop,
                    into,
                    Consist44,
                    ApproachIndex,
                    out var tailPast));

            var nosePast = RouteTailAlongTrack.NosePastFromTail(tailPast, Consist44);
            var sample = new RouteClearanceSample(
                hasPin: true,
                nosePastJunctionM: nosePast,
                consistLengthM: Consist44,
                frogEnvelopeM: Frog,
                approachWindowM: RouteClearanceEval.DefaultApproachWindowM);
            var decision = RouteClearanceEval.Evaluate(RouteClearancePhase.AtSwitch, in sample);

            if (tailPast < Frog)
            {
                Assert.True(
                    decision.Phase != RouteClearancePhase.Cleared,
                    "noseAlong=" + noseAlong.ToString("0")
                    + " tailPast=" + tailPast.ToString("0.0")
                    + " must stay At switch until tail ≥ "
                    + Frog.ToString("0")
                    + " m past pin");
            }
            else
            {
                Assert.Equal(RouteClearancePhase.Cleared, decision.Phase);
                Assert.True(RouteClearanceEval.IsClearedOfFrog(in sample));
                sawCleared = true;
            }
        }

        Assert.True(sawCleared, "walk must eventually CLEARED after tail clears frog");
    }

    [Fact]
    public void Smoke_16_2_34_5_c4s_off_plan_after_past_10_clears_from_the_saved_sample()
    {
        // Cab 2.16.34.4: tail past=10 on #S526, then past=?. The approach hold
        // erased it. The saved sample is 2 m short of the 12 m line, inside the
        // aim tolerance. A raw 3D dot is not the input. Still approaching does not clear.
        const float consist = 7f;
        var savedPast10 = 10f + consist;
        var nose = RouteClearanceTravel.NosePastWhenAlongTrackLost(
            samePin: true,
            sawAtSwitchThisLeg: true,
            savedPast10,
            consist);
        Assert.True(RouteClearanceEval.IsClearedOfFrog(Sample(nose, consist)));

        var savedStillApproaching = -11f + consist;
        var held = RouteClearanceTravel.NosePastWhenAlongTrackLost(
            samePin: true,
            sawAtSwitchThisLeg: true,
            savedStillApproaching,
            consist);
        Assert.False(RouteClearanceEval.IsClearedOfFrog(Sample(held, consist)));

        var lost = RouteClearanceTravel.NosePastWhenAlongTrackLost(
            samePin: true,
            sawAtSwitchThisLeg: false,
            savedPast10,
            consist);
        Assert.Equal(RouteClearanceTravel.NoseHeldOnApproachSide(), lost);
    }

    /// <summary>
    /// Cab 2.16.38 SL-55 step 1: still-approach on SW-B4L, then past=? hop=?.
    /// The 12 m test never saw a distance. A measure taken while the approach
    /// hold is up must clear, and the next blank poll must keep that distance.
    /// </summary>
    [Fact]
    public void Smoke_sl55_step1_on_plan_measure_clears_and_a_blank_poll_keeps_it()
    {
        const float consist = 7f;
        var frog = RouteClearanceEval.DefaultFrogEnvelopeM;
        var measuredNose = consist + frog;

        var nose = RouteClearanceTravel.NosePastForPoll(
            approachHold: true,
            measured: true,
            measuredNosePast: measuredNose,
            planHopCount: 6,
            travelAxisNosePast: 0f,
            samePin: true,
            sawAtSwitchThisLeg: false,
            bestNosePastMeters: null,
            consistLengthM: consist);
        Assert.True(RouteClearanceEval.IsClearedOfFrog(Sample(nose, consist)));
        Assert.NotEqual(RouteClearanceTravel.NoseHeldOnApproachSide(), nose);

        var kept = RouteClearanceTravel.NosePastForPoll(
            approachHold: false,
            measured: false,
            measuredNosePast: 0f,
            planHopCount: 6,
            travelAxisNosePast: 0f,
            samePin: true,
            sawAtSwitchThisLeg: true,
            bestNosePastMeters: measuredNose,
            consistLengthM: consist);
        Assert.True(RouteClearanceEval.IsClearedOfFrog(Sample(kept, consist)));

        var blank = RouteClearanceTravel.NosePastForPoll(
            approachHold: true,
            measured: false,
            measuredNosePast: 0f,
            planHopCount: 6,
            travelAxisNosePast: measuredNose,
            samePin: true,
            sawAtSwitchThisLeg: false,
            bestNosePastMeters: null,
            consistLengthM: consist);
        Assert.False(RouteClearanceEval.IsClearedOfFrog(Sample(blank, consist)));
        Assert.Equal(RouteClearanceTravel.NoseHeldOnApproachSide(), blank);

        // Cab 2.16.38: path-eval SW-B4L→SW-B4L. One hop cannot place the pin,
        // so the travel-axis tail is the distance. A longer plan must not use it.
        var sameTrack = RouteClearanceTravel.NosePastForPoll(
            approachHold: true,
            measured: false,
            measuredNosePast: 0f,
            planHopCount: 1,
            travelAxisNosePast: measuredNose,
            samePin: true,
            sawAtSwitchThisLeg: false,
            bestNosePastMeters: null,
            consistLengthM: consist);
        Assert.True(RouteClearanceEval.IsClearedOfFrog(Sample(sameTrack, consist)));

        var sameTrackShort = RouteClearanceTravel.NosePastForPoll(
            approachHold: true,
            measured: false,
            measuredNosePast: 0f,
            planHopCount: 1,
            travelAxisNosePast: 0f,
            samePin: true,
            sawAtSwitchThisLeg: false,
            bestNosePastMeters: null,
            consistLengthM: consist);
        Assert.False(RouteClearanceEval.IsClearedOfFrog(Sample(sameTrackShort, consist)));
    }

    /// <summary>
    /// Cab 2.16.39 SL-55 after the couple: cars=3 len=44, plan SW-B1S→SW-C4S,
    /// still-approach SW-B1S, then past=? hop=?. The lead car had left the hops.
    /// The tail car was still on SW-B1S. The measure must read that bogie and
    /// subtract only the train behind it. Every bogie off the hops stays blank.
    /// A multi-hop plan still does not clear from the travel axis.
    /// </summary>
    [Fact]
    public void Smoke_sl55_cars3_lead_off_plan_measures_the_tail_on_the_hops()
    {
        var hops = new[] { "SW-B1S", "#Y-#S241#T", Stem, "SW-C4S" };
        var lengths = new[] { 40f, 20f, 80f, 60f };
        const int approach = 1;
        const float consist = 44f;
        const float tailCar = 19f;
        var behindTailCar = RouteTailAlongTrack.MetersBehindCarFace(consist, consist - tailCar);
        Assert.Equal(tailCar, behindTailCar);

        var stillOnB1S = new[]
        {
            new RouteTailAlongTrack.ConsistBogieOnPlan("#Y-#S113#T", 10f, consist),
            new RouteTailAlongTrack.ConsistBogieOnPlan("#Y-#S989#T", 4f, consist),
            new RouteTailAlongTrack.ConsistBogieOnPlan("SW-B1S", 10f, behindTailCar),
        };
        Assert.True(RouteTailAlongTrack.TryTailPastFromConsistBogies(
            hops,
            lengths,
            stillOnB1S,
            stillOnB1S.Length,
            approach,
            out var approaching,
            out var hop));
        Assert.Equal("SW-B1S", hop);
        Assert.True(approaching < 0f, "tail still on B1S is not past the pin");
        Assert.False(RouteClearanceEval.IsClearedOfFrog(
            Sample(RouteTailAlongTrack.NosePastFromTail(approaching, consist), consist)));

        var tailThrough = new[]
        {
            new RouteTailAlongTrack.ConsistBogieOnPlan("#Y-#S113#T", 10f, consist),
            new RouteTailAlongTrack.ConsistBogieOnPlan(Stem, 40f, behindTailCar),
        };
        Assert.True(RouteTailAlongTrack.TryTailPastFromConsistBogies(
            hops,
            lengths,
            tailThrough,
            tailThrough.Length,
            approach,
            out var clearedPast,
            out var stemHop));
        Assert.Equal(Stem, stemHop);
        Assert.True(clearedPast >= RouteClearanceEval.DefaultFrogEnvelopeM);
        Assert.True(RouteClearanceEval.IsClearedOfFrog(
            Sample(RouteTailAlongTrack.NosePastFromTail(clearedPast, consist), consist)));

        // Nose still on the plan wins, and it matches the single-bogie ruler.
        var noseBehind = RouteTailAlongTrack.MetersBehindCarFace(consist, 0f);
        Assert.True(RouteTailAlongTrack.TryTailPastPin(
            hops, lengths, leadHopIndex: 0, leadIntoHopMeters: 8f, consist, approach, out var noseOnly));
        var both = new[]
        {
            new RouteTailAlongTrack.ConsistBogieOnPlan("SW-B1S", 8f, noseBehind),
            new RouteTailAlongTrack.ConsistBogieOnPlan(Stem, 40f, behindTailCar),
        };
        Assert.True(RouteTailAlongTrack.TryTailPastFromConsistBogies(
            hops, lengths, both, both.Length, approach, out var noseWins, out var noseHop));
        Assert.Equal("SW-B1S", noseHop);
        Assert.Equal(noseOnly, noseWins);

        var off = new[]
        {
            new RouteTailAlongTrack.ConsistBogieOnPlan("#Y-#S113#T", 10f, consist),
            new RouteTailAlongTrack.ConsistBogieOnPlan("#Y-#S989#T", 4f, consist),
        };
        Assert.False(RouteTailAlongTrack.TryTailPastFromConsistBogies(
            hops, lengths, off, off.Length, approach, out _, out _));

        var blank = RouteClearanceTravel.NosePastForPoll(
            approachHold: false,
            measured: false,
            measuredNosePast: 0f,
            planHopCount: hops.Length,
            travelAxisNosePast: consist + RouteClearanceEval.DefaultFrogEnvelopeM,
            samePin: true,
            sawAtSwitchThisLeg: false,
            bestNosePastMeters: null,
            consistLengthM: consist);
        Assert.False(RouteClearanceEval.IsClearedOfFrog(Sample(blank, consist)));
    }

    /// <summary>
    /// Cab 2.16.47 square 8: along-track died at rem=19 (62 m consist, tail
    /// still short) after At switch. A later travel-axis sample with the tail
    /// past the frog CLEARED. A backward swing keeps the saved nose.
    /// </summary>
    [Fact]
    public void Smoke_sl55_square8_lost_track_clears_when_the_tail_passes()
    {
        const float consist = 62f;
        var frog = RouteClearanceEval.DefaultFrogEnvelopeM;
        var saved = (frog + consist) - 19f;
        var held = RouteClearanceTravel.NosePastForPoll(
            approachHold: false,
            measured: false,
            measuredNosePast: 0f,
            planHopCount: 8,
            travelAxisNosePast: saved,
            samePin: true,
            sawAtSwitchThisLeg: true,
            bestNosePastMeters: saved,
            consistLengthM: consist,
            pinOnPlan: true);
        Assert.False(RouteClearanceEval.IsClearedOfFrog(Sample(held, consist)));

        var swung = RouteClearanceTravel.NosePastForPoll(
            approachHold: false,
            measured: false,
            measuredNosePast: 0f,
            planHopCount: 8,
            travelAxisNosePast: saved - 40f,
            samePin: true,
            sawAtSwitchThisLeg: true,
            bestNosePastMeters: saved,
            consistLengthM: consist,
            pinOnPlan: true);
        Assert.True(swung >= saved - 0.01f);
        Assert.False(RouteClearanceEval.IsClearedOfFrog(Sample(swung, consist)));

        var passed = RouteClearanceTravel.NosePastForPoll(
            approachHold: false,
            measured: false,
            measuredNosePast: 0f,
            planHopCount: 8,
            travelAxisNosePast: consist + frog,
            samePin: true,
            sawAtSwitchThisLeg: true,
            bestNosePastMeters: saved,
            consistLengthM: consist,
            pinOnPlan: true);
        Assert.True(RouteClearanceEval.IsClearedOfFrog(Sample(passed, consist)));
    }

    [Fact]
    public void Smoke_16_2_34_7_c4s_logged_past_10_is_a_9_5m_tail_and_still_clears()
    {
        // Cab 2.16.34.6 B4L→C4S: the stem logged past=10, then past=?, and never
        // CLEARED. The log rounds 9.5 m up to 10. That tail is 2.5 m short of the
        // 12 m line, outside the 2 m aim tolerance, so the saved sample did not snap.
        const float consist = 7f;
        var loggedAs10 = 9.5f + consist;
        var nose = RouteClearanceTravel.NosePastWhenAlongTrackLost(
            samePin: true,
            sawAtSwitchThisLeg: true,
            loggedAs10,
            consist);
        Assert.True(RouteClearanceEval.IsClearedOfFrog(Sample(nose, consist)));

        var stillShort = 8f + consist;
        var held = RouteClearanceTravel.NosePastWhenAlongTrackLost(
            samePin: true,
            sawAtSwitchThisLeg: true,
            stillShort,
            consist);
        Assert.False(RouteClearanceEval.IsClearedOfFrog(Sample(held, consist)));
    }

    private static RouteClearanceSample Sample(float nosePast, float consist) =>
        new(
            hasPin: true,
            nosePastJunctionM: nosePast,
            consistLengthM: consist,
            frogEnvelopeM: RouteClearanceEval.DefaultFrogEnvelopeM,
            approachWindowM: RouteClearanceEval.DefaultApproachWindowM);

    [Fact]
    public void Smoke_16_2_34_4_c4s_nose_off_plan_still_clears_when_the_tail_bogie_is_past_the_frog()
    {
        // Cab 2.16.34.3: tail past=9 on #S526, then the nose hop left the plan
        // (#S989 / #S113) and the phase reset to still-approach. No CLEARED.
        const string stem = "#Y-#S526#T";
        var hops = new[] { "#Y-#S1263#T", stem, "#Y-#S842#T" };
        var lengths = new[] { 40f, 20f, 80f };
        Assert.False(RouteTailAlongTrack.ShouldHoldApproachForTail(tailTrackIsOnPlan: false, tailStillBeforePinExit: true));
        Assert.True(RouteTailAlongTrack.ShouldHoldApproachForTail(tailTrackIsOnPlan: true, tailStillBeforePinExit: true));

        Assert.True(RouteTailAlongTrack.TryOnPlanBogieHop(
            hops,
            frontTrackId: "#Y-#S113#T",
            rearTrackId: stem,
            out var hop,
            out var frontOnPlan));
        Assert.False(frontOnPlan);
        Assert.Equal(1, hop);
        Assert.False(RouteTailAlongTrack.TryOnPlanBogieHop(
            hops,
            "#Y-#S113#T",
            "#Y-#S989#T",
            out _,
            out _));

        Assert.True(RouteTailAlongTrack.TryPointPastPin(
            hops,
            lengths,
            hop,
            intoHopMeters: 15f,
            approachHopIndex: 0,
            out var tailPast));
        Assert.True(tailPast >= RouteClearanceEval.DefaultFrogEnvelopeM);
        var nosePast = RouteTailAlongTrack.NosePastFromTail(tailPast, Consist44);
        Assert.True(RouteClearanceEval.IsClearedOfFrog(
            new RouteClearanceSample(
                hasPin: true,
                nosePastJunctionM: nosePast,
                consistLengthM: Consist44,
                frogEnvelopeM: RouteClearanceEval.DefaultFrogEnvelopeM,
                approachWindowM: RouteClearanceEval.DefaultApproachWindowM)));
    }

    [Fact]
    public void Smoke_21630_step6_flipped_lead_car_forward_does_not_read_rem_0()
    {
        // Cab 2.16.30: latch reverse=0, arm-go, yard-req rem=0 while still on
        // B side. Flipped hopper forward turns ~60 m ahead into +60 past.
        const float pinAheadMeters = 60f;
        var flippedGolden = -pinAheadMeters; // wrong sign from reversed car forward
        var flippedNosePast = -flippedGolden; // if treated as already past
        Assert.Equal(60f, flippedNosePast);
        var flippedRem = YardApproachKinematics.RemToClearedMeters(
            flippedNosePast,
            Consist44,
            Frog);
        Assert.Equal(0f, flippedRem);

        // Along-track: nose still on approach, 2 m into Approach hop.
        Assert.True(
            RouteTailAlongTrack.TryTailPastPin(
                Hops,
                Lengths,
                leadHopIndex: ApproachIndex,
                leadIntoHopMeters: 2f,
                Consist44,
                ApproachIndex,
                out var tailPast));
        var nosePast = RouteTailAlongTrack.NosePastFromTail(tailPast, Consist44);
        var rem = YardApproachKinematics.RemToClearedMeters(nosePast, Consist44, Frog);
        Assert.True(rem is float r && r > 50f, "rem=" + rem);
        Assert.False(
            RouteClearanceEval.IsClearedOfFrog(
                new RouteClearanceSample(
                    hasPin: true,
                    nosePastJunctionM: nosePast,
                    consistLengthM: Consist44,
                    frogEnvelopeM: Frog,
                    approachWindowM: RouteClearanceEval.DefaultApproachWindowM)));
    }

    [Fact]
    public void Smoke_21630_tail_track_off_plan_holds_still_approach()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "SW-B1S", NearSide, Approach, Stem, "SW-C4S" },
            System.Array.Empty<PathJunctionEval>(),
            misalignedCount: 0,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 593f,
            junctionApproachFrom: new Dictionary<string, string>
            {
                [Pin] = Approach,
            });

        Assert.True(plan.TailStillBeforePinExit(Pin, "#Y-#S9999#T"));
        Assert.True(plan.TailStillBeforePinExit(Pin, null));
        Assert.True(plan.TailStillBeforePinExit("missing-pin", Stem));

        var nose = RouteClearanceTravel.NoseHeldOnApproachSide();
        var held = new RouteClearanceSample(
            hasPin: true,
            nosePastJunctionM: nose,
            consistLengthM: Consist44,
            frogEnvelopeM: Frog,
            approachWindowM: RouteClearanceEval.DefaultApproachWindowM);
        var decision = RouteClearanceEval.Evaluate(RouteClearancePhase.Approaching, in held);
        Assert.Equal(RouteClearancePhase.Approaching, decision.Phase);
        Assert.False(decision.CanAdvanceNext);
    }

    [Fact]
    public void Sawtooth_1002848_pull_past_room_fits_44m_consist()
    {
        Assert.True(
            RouteTailAlongTrack.PullPastRoomFits(
                Lengths,
                Lengths.Length,
                ApproachIndex,
                Consist44,
                Frog));

        // Stem alone (~19 m) cannot hold 44 + 12.
        var stemOnly = new[] { 40f, 15f, 10f, 19.34f };
        Assert.False(
            RouteTailAlongTrack.PullPastRoomFits(
                stemOnly,
                stemOnly.Length,
                ApproachIndex,
                Consist44,
                Frog));

        Assert.False(
            RouteTailAlongTrack.PullPastRoomFits(
                Lengths,
                hopCount: 5,
                ApproachIndex,
                consistLengthMeters: 100f,
                Frog));
    }

    [Fact]
    public void Sawtooth_sl55_step6_to_step7_rows_are_forward_cleared_align_reverse()
    {
        var steps = SwitchListPlanner.Build(Sl55LiveMultiPickupJob());
        Assert.NotNull(steps);
        Assert.True(steps!.Count > 6);

        // Cab step 6 = planner index 5 (Past switch → C4S). Step 7 = Prep C4S.
        var past = steps[5];
        var prep = steps[6];
        Assert.Equal(SwitchListStepKind.Transit, past.Kind);
        Assert.Equal("SW-C4S", past.DestTrackId);
        Assert.False(past.BindNeedsReverse);
        Assert.Contains("Past switch", past.Label);

        Assert.Equal(SwitchListStepKind.Prep, prep.Kind);
        Assert.Equal("SW-C4S", prep.DestTrackId);
        Assert.True(prep.BindNeedsReverse);

        var livePast = SwitchListStepDisplay.LiveLabel(past, false);
        Assert.StartsWith("Set Forward", livePast);
        var livePrep = SwitchListStepDisplay.LiveLabel(prep, true);
        Assert.StartsWith("Set Reverse", livePrep);

        // B1S→C4S corridor on the harvest may skip 1002848 (cab uses pin
        // corridor). Lock: no reverse on that transit; Prep bind is Reverse.
        var snap = HtpFixtures.LoadGraph();
        var spatial = SpatialGraph.FromHarvestJunctions(snap.Junctions);
        var plan = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            "SW-B1S",
            "SW-C4S",
            destYardId: "SW",
            mode: PathPlanMode.Yard,
            spatial: spatial);
        Assert.NotEqual(PathCheckStatus.NoPath, plan.Status);
        Assert.Equal(0, plan.ReverseCount);
    }

    [Fact]
    public void Telemetry_tail_along_formats_past_and_hop()
    {
        Assert.Equal(
            "T2 route-pin: tail-along past=12 hop=#Y-#S526#T",
            RouteClearanceTelemetry.FormatTailAlong(12.4f, Stem));
    }

    private static void LocateNose(float noseAlong, out int leadHop, out float into)
    {
        var remaining = noseAlong;
        for (var i = 0; i < Lengths.Length; i++)
        {
            if (remaining <= Lengths[i] || i == Lengths.Length - 1)
            {
                leadHop = i;
                into = remaining < 0f ? 0f : remaining;
                if (into > Lengths[i])
                {
                    into = Lengths[i];
                }

                return;
            }

            remaining -= Lengths[i];
        }

        leadHop = Lengths.Length - 1;
        into = Lengths[leadHop];
    }

    private static JobSummary Sl55LiveMultiPickupJob() =>
        new()
        {
            JobId = "SW-SL-55",
            JobTypeLabel = "SL",
            OriginYardId = "SW",
            DestYardId = "SW",
            OriginTrackId = "SW-B1S",
            AdditionalPickupTrackIds = new[] { "SW-C4S" },
            DestTrackId = "SW-C1O",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1774#T",
            TurntablePivotTrackId = "SW-B4L",
            TurntableApproachNeedsReverse = true,
            PrepApproachTrackId = "#Y-#S1512#T",
            NeedsReverseInto = true,
            ReverseIntoTrackId = "SW-B4L",
            LoadTrackId = "SW-B4L",
            LoadCargoLabel = "Wood Chips",
        };
}
