namespace YardMasterSuite.Core;

/// <summary>
/// Along-track frog clearance for Maps / Switch List pin (**8.7**).
/// Axis: +meters = past the pin junction in travel direction; − = still approaching.
/// Consist occupies [nosePast − length, nosePast]. Cleared when the trailing end
/// is past +frogEnvelope. Not a circular yard radius (avoids parallel-track false fouls).
/// </summary>
public enum RouteClearancePhase
{
    Idle = 0,
    Approaching = 1,
    AtSwitch = 2,
    Cleared = 3,
}

public readonly struct RouteClearanceSample
{
    public RouteClearanceSample(
        bool hasPin,
        float nosePastJunctionM,
        float consistLengthM,
        float frogEnvelopeM,
        float approachWindowM)
    {
        HasPin = hasPin;
        NosePastJunctionM = nosePastJunctionM;
        ConsistLengthM = consistLengthM;
        FrogEnvelopeM = frogEnvelopeM;
        ApproachWindowM = approachWindowM;
    }

    public bool HasPin { get; }
    public float NosePastJunctionM { get; }
    public float ConsistLengthM { get; }
    public float FrogEnvelopeM { get; }
    public float ApproachWindowM { get; }
}

public readonly struct RouteClearanceDecision
{
    public RouteClearanceDecision(
        RouteClearancePhase phase,
        bool fouling,
        bool canThrowAlign,
        bool canAdvanceNext,
        string? caption)
    {
        Phase = phase;
        Fouling = fouling;
        CanThrowAlign = canThrowAlign;
        CanAdvanceNext = canAdvanceNext;
        Caption = caption;
    }

    public RouteClearancePhase Phase { get; }
    public bool Fouling { get; }
    public bool CanThrowAlign { get; }
    public bool CanAdvanceNext { get; }
    public string? Caption { get; }
}

public static class RouteClearanceEval
{
    public const float DefaultFrogEnvelopeM = 12f;
    public const float DefaultApproachWindowM = 120f;

    public static bool IsFouling(in RouteClearanceSample sample)
    {
        if (!sample.HasPin || sample.ConsistLengthM <= 0f)
        {
            return false;
        }

        var frog = sample.FrogEnvelopeM > 0f ? sample.FrogEnvelopeM : DefaultFrogEnvelopeM;
        var nose = sample.NosePastJunctionM;
        var tail = nose - sample.ConsistLengthM;
        // Overlap of [tail, nose] with [-frog, +frog]
        return nose >= -frog && tail <= frog;
    }

    public static bool IsClearedOfFrog(in RouteClearanceSample sample)
    {
        if (!sample.HasPin || sample.ConsistLengthM <= 0f)
        {
            return false;
        }

        var frog = sample.FrogEnvelopeM > 0f ? sample.FrogEnvelopeM : DefaultFrogEnvelopeM;
        return (sample.NosePastJunctionM - sample.ConsistLengthM) >= frog;
    }

    public static RouteClearanceDecision Evaluate(
        RouteClearancePhase prior,
        in RouteClearanceSample sample)
    {
        if (!sample.HasPin)
        {
            return new RouteClearanceDecision(
                RouteClearancePhase.Idle,
                fouling: false,
                canThrowAlign: true,
                canAdvanceNext: true,
                caption: null);
        }

        var fouling = IsFouling(in sample);
        var clear = IsClearedOfFrog(in sample);
        RouteClearancePhase phase;

        if (prior == RouteClearancePhase.Cleared && !fouling)
        {
            phase = RouteClearancePhase.Cleared;
        }
        else if (clear)
        {
            phase = RouteClearancePhase.Cleared;
        }
        else if (fouling || sample.NosePastJunctionM >= -sample.ApproachWindowM)
        {
            phase = RouteClearancePhase.AtSwitch;
        }
        else
        {
            phase = RouteClearancePhase.Approaching;
        }

        var caption = phase == RouteClearancePhase.Cleared ? "CLEARED" : "At switch";
        var allow = phase == RouteClearancePhase.Cleared;
        return new RouteClearanceDecision(
            phase,
            fouling,
            canThrowAlign: allow,
            canAdvanceNext: allow,
            caption);
    }
}

public enum RouteClearanceGateReason
{
    Ok = 0,
    NeedCleared = 1,
}

public static class RouteClearanceGate
{
    public static RouteClearanceGateReason Align(bool hasPin, RouteClearancePhase phase) =>
        !hasPin || phase == RouteClearancePhase.Cleared
            ? RouteClearanceGateReason.Ok
            : RouteClearanceGateReason.NeedCleared;

    public static RouteClearanceGateReason Next(bool hasPin, RouteClearancePhase phase) =>
        Align(hasPin, phase);

    public static string DenyAlignLog => "T2 align: need CLEARED";

    public static string DenyNextLog => "T2 switch-list: need CLEARED";
}
