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
