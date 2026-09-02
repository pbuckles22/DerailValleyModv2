using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 8.7 smoke harvest: length-aware frog clearance (not a circular radius, not DE2 18 m).
/// Consist must fully clear the pin junction before Align/Next may throw or advance.
/// </summary>
public class RouteClearanceEvalTests
{
    private const float Frog = 12f;
    private const float Approach = 120f;

    private static RouteClearanceSample Sample(
        bool hasPin,
        float nosePast,
        float length,
        float frog = Frog,
        float approach = Approach) =>
        new(hasPin, nosePast, length, frog, approach);

    [Fact]
    public void ConsistLengthMeters_sums_car_lengths_fail_closed_empty()
    {
        Assert.Equal(0f, ConsistLengthMeters.Sum(null));
        Assert.Equal(0f, ConsistLengthMeters.Sum(System.Array.Empty<float>()));
        Assert.Equal(45f, ConsistLengthMeters.Sum(new[] { 18f, 12f, 15f }));
        Assert.Equal(30f, ConsistLengthMeters.Sum(new[] { 18f, -2f, 12f }));
        Assert.Equal(18f, ConsistLengthMeters.Sum(new[] { 18f, 12f, 15f }, count: 1));
        Assert.Equal(0f, ConsistLengthMeters.Sum(new[] { 18f }, count: 0));
    }

    [Fact]
    public void Smoke_forward_CLEARED_bobtail_hides_coach_tail_still_fouling()
    {
        const float nosePast = 25f;
        const float bobtail = 7.49f;
        const float trainset = 38f;
        Assert.True(RouteClearanceEval.IsClearedOfFrog(Sample(true, nosePast, bobtail)));
        Assert.False(RouteClearanceEval.IsClearedOfFrog(Sample(true, nosePast, trainset)));
        Assert.True(RouteClearanceEval.IsFouling(Sample(true, nosePast, trainset)));
    }

    [Fact]
    public void Fouling_when_any_consist_span_overlaps_frog_envelope()
    {
        // Solo loco 18 m: nose 5 m past → still fouling (tail at -13).
        Assert.True(RouteClearanceEval.IsFouling(Sample(true, nosePast: 5f, length: 18f)));
        // Long consist: nose 40 past, length 50 → tail at -10, still in frog.
        Assert.True(RouteClearanceEval.IsFouling(Sample(true, nosePast: 40f, length: 50f)));
        // Clear: nose 70 past, length 50 → tail at +20 >= frog 12.
        Assert.False(RouteClearanceEval.IsFouling(Sample(true, nosePast: 70f, length: 50f)));
        // Approaching far: nose -80, length 50 → occupies [-130,-80], no frog overlap.
        Assert.False(RouteClearanceEval.IsFouling(Sample(true, nosePast: -80f, length: 50f)));
    }

    [Fact]
    public void Cleared_requires_tail_past_frog_envelope_length_aware()
    {
        // DE2-alone would clear at ~30; 3-car 50 m still fouls there.
        Assert.False(RouteClearanceEval.IsClearedOfFrog(Sample(true, nosePast: 30f, length: 50f)));
        Assert.True(RouteClearanceEval.IsClearedOfFrog(Sample(true, nosePast: 62f, length: 50f)));
        Assert.False(RouteClearanceEval.IsClearedOfFrog(Sample(true, nosePast: 62f, length: 0f)));
        Assert.False(RouteClearanceEval.IsClearedOfFrog(Sample(hasPin: false, nosePast: 100f, length: 50f)));
    }

    [Fact]
    public void Smoke_approach_foul_clear_latch_and_reenter_cancels()
    {
        var phase = RouteClearancePhase.Idle;

        // Far approach → Approaching / At switch caption when inside window.
        var d = RouteClearanceEval.Evaluate(phase, Sample(true, nosePast: -40f, length: 40f));
        Assert.Equal(RouteClearancePhase.AtSwitch, d.Phase);
        Assert.Equal("At switch", d.Caption);
        Assert.False(d.CanThrowAlign);
        Assert.False(d.CanAdvanceNext);
        phase = d.Phase;

        // Fouling the points.
        d = RouteClearanceEval.Evaluate(phase, Sample(true, nosePast: 8f, length: 40f));
        Assert.Equal(RouteClearancePhase.AtSwitch, d.Phase);
        Assert.True(d.Fouling);
        Assert.False(d.CanThrowAlign);
        phase = d.Phase;

        // Tail clear of frog → CLEARED latch.
        d = RouteClearanceEval.Evaluate(phase, Sample(true, nosePast: 55f, length: 40f));
        Assert.Equal(RouteClearancePhase.Cleared, d.Phase);
        Assert.Equal("CLEARED", d.Caption);
        Assert.True(d.CanThrowAlign);
        Assert.True(d.CanAdvanceNext);
        phase = d.Phase;

        // Stay latched while still past (not fouling).
        d = RouteClearanceEval.Evaluate(phase, Sample(true, nosePast: 80f, length: 40f));
        Assert.Equal(RouteClearancePhase.Cleared, d.Phase);
        Assert.True(d.CanThrowAlign);

        // Re-enter danger cancels latch.
        d = RouteClearanceEval.Evaluate(phase, Sample(true, nosePast: 10f, length: 40f));
        Assert.Equal(RouteClearancePhase.AtSwitch, d.Phase);
        Assert.Equal("At switch", d.Caption);
        Assert.False(d.CanThrowAlign);
    }

    [Fact]
    public void No_pin_allows_align_and_next()
    {
        var d = RouteClearanceEval.Evaluate(
            RouteClearancePhase.Idle,
            Sample(hasPin: false, nosePast: 0f, length: 40f));
        Assert.Equal(RouteClearancePhase.Idle, d.Phase);
        Assert.Null(d.Caption);
        Assert.True(d.CanThrowAlign);
        Assert.True(d.CanAdvanceNext);
    }

    [Fact]
    public void Gate_denies_align_until_cleared_when_pin_active()
    {
        Assert.Equal(
            RouteClearanceGateReason.NeedCleared,
            RouteClearanceGate.Align(hasPin: true, RouteClearancePhase.AtSwitch));
        Assert.Equal(
            RouteClearanceGateReason.Ok,
            RouteClearanceGate.Align(hasPin: true, RouteClearancePhase.Cleared));
        Assert.Equal(
            RouteClearanceGateReason.Ok,
            RouteClearanceGate.Align(hasPin: false, RouteClearancePhase.Idle));
        Assert.Equal(
            RouteClearanceGateReason.NeedCleared,
            RouteClearanceGate.Next(hasPin: true, RouteClearancePhase.Approaching));
        Assert.Equal(
            RouteClearanceGateReason.Ok,
            RouteClearanceGate.Next(hasPin: true, RouteClearancePhase.Cleared));
    }

    [Fact]
    public void Smoke_solo_reverse_past_pin_in_windshield_CLEARED()
    {
        // Backed through in R: pin visible ahead in cab; hood only ~5 m west of pin
        // but butt is ~12.5 m west — old hood-only math stayed At switch.
        const float length = 7.5f;
        const float hoodPastPinM = -5f;
        var past = RouteClearanceTravel.SampleTravelPastM(
            hoodPastPinM,
            0f,
            pinX: 0f,
            pinZ: 0f,
            locoForwardX: 1f,
            locoForwardZ: 0f,
            length,
            travelUsesReverse: true,
            soloConsist: true);
        Assert.True(RouteClearanceEval.IsClearedOfFrog(Sample(true, past, length)));

        var hoodOnly = RouteClearanceTravel.TravelPastJunctionM(
            hoodPastPinM, length, travelReverse: true);
        Assert.False(RouteClearanceEval.IsClearedOfFrog(Sample(true, hoodOnly, length)));
    }

    [Fact]
    public void Smoke_reverse_gear_approach_not_cleared_before_pin()
    {
        // leadingPast = -goldenNosePast + length (Gemini .13).
        const float length = 38f;
        var nosePast = (-10f) + length; // still fouling
        Assert.True(nosePast > 0f);
        Assert.False(RouteClearanceEval.IsClearedOfFrog(Sample(true, nosePast, length)));
    }

    [Fact]
    public void Smoke_locomotive_forward_axis_inverted_without_reverse_travel()
    {
        // Golden raw Dot(+10) with short consist looks past frog; reverse leading-edge stays fouling.
        Assert.True(RouteClearanceEval.IsClearedOfFrog(Sample(true, nosePast: 30f, length: 10f)));
        Assert.False(RouteClearanceEval.IsClearedOfFrog(Sample(true, nosePast: (-10f) + 38f, length: 38f)));
    }

    [Fact]
    public void Telemetry_emits_only_on_phase_change()
    {
        var cache = default(RouteClearanceTelemetryCache);
        Assert.Equal(
            "T2 route-pin: At switch",
            RouteClearanceTelemetry.Observe(RouteClearancePhase.AtSwitch, "At switch", ref cache));
        Assert.Null(RouteClearanceTelemetry.Observe(RouteClearancePhase.AtSwitch, "At switch", ref cache));
        Assert.Equal(
            "T2 route-pin: CLEARED",
            RouteClearanceTelemetry.Observe(RouteClearancePhase.Cleared, "CLEARED", ref cache));
        Assert.Equal(
            "T2 route-pin: idle",
            RouteClearanceTelemetry.Observe(RouteClearancePhase.Idle, null, ref cache));
    }

    [Fact]
    public void Session_stores_decision_for_align_next_and_ar()
    {
        RouteClearanceSession.Clear();
        Assert.True(RouteClearanceSession.CanThrowAlign);
        Assert.False(RouteClearanceSession.HasPin);

        RouteClearanceSession.Apply(
            new RouteClearanceDecision(
                RouteClearancePhase.AtSwitch,
                fouling: true,
                canThrowAlign: false,
                canAdvanceNext: false,
                caption: "At switch"),
            pinJunctionId: "42",
            pinX: 1f,
            pinY: 2f,
            pinZ: 3f);
        Assert.True(RouteClearanceSession.HasPin);
        Assert.False(RouteClearanceSession.CanThrowAlign);
        Assert.Equal("At switch", RouteClearanceSession.Caption);
        Assert.True(RouteClearanceSession.TryGetPinWorld(out var x, out var y, out var z));
        Assert.Equal(1f, x);
        Assert.Equal(2f, y);
        Assert.Equal(3f, z);

        RouteClearanceSession.Apply(
            new RouteClearanceDecision(
                RouteClearancePhase.Cleared,
                fouling: false,
                canThrowAlign: true,
                canAdvanceNext: true,
                caption: "CLEARED"),
            pinJunctionId: "42",
            pinX: 1f,
            pinY: 2f,
            pinZ: 3f);
        Assert.True(RouteClearanceSession.CanThrowAlign);
        Assert.True(RouteClearanceSession.CanAdvanceNext);
        Assert.Equal(RouteClearancePhase.Cleared, RouteClearanceSession.Phase);

        RouteClearanceSession.Clear();
        Assert.False(RouteClearanceSession.HasPin);
        Assert.Equal(RouteClearancePhase.Idle, RouteClearanceSession.Phase);
    }
}
