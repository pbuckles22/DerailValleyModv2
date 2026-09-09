namespace YardMasterSuite.Core;

/// <summary>Latched pin clearance for Align / Next / AR (**8.7**). Golden <c>2.8.7.2</c> — no travel facing.</summary>
public static class RouteClearanceSession
{
    private static RouteClearancePhase _phase;
    private static bool _hasPin;
    private static string? _pinJunctionId;
    private static string? _caption;
    private static float _pinX;
    private static float _pinY;
    private static float _pinZ;
    private static bool _canThrowAlign = true;
    private static bool _canAdvanceNext = true;
    private static float? _nosePastJunctionM;
    private static float _consistLengthM;
    private static bool _sawAtSwitchThisLeg;

    public static RouteClearancePhase Phase => _phase;

    public static bool HasPin => _hasPin;

    public static string? PinJunctionId => _pinJunctionId;

    public static string? Caption => _caption;

    public static bool CanThrowAlign => _canThrowAlign;

    public static bool CanAdvanceNext => _canAdvanceNext;

    /// <summary>
    /// True once this pin passed <see cref="RouteClearancePhase.AtSwitch"/>.
    /// Relatch / idle / already-CLEARED at rest stay false so pull-out cannot
    /// skip the frog (cab 2.13.2.5.3).
    /// </summary>
    public static bool SawAtSwitchThisLeg => _sawAtSwitchThisLeg;

    /// <summary>Meters until frog CLEARED (tail past envelope) — primary Past-switch taper rem.</summary>
    public static float? RemToClearedMeters =>
        !_hasPin
            ? null
            : YardApproachKinematics.RemToClearedMeters(
                _nosePastJunctionM,
                _consistLengthM,
                RouteClearanceEval.DefaultFrogEnvelopeM);

    public static void Clear()
    {
        _phase = RouteClearancePhase.Idle;
        _hasPin = false;
        _pinJunctionId = null;
        _caption = null;
        _pinX = _pinY = _pinZ = 0f;
        _canThrowAlign = true;
        _canAdvanceNext = true;
        _nosePastJunctionM = null;
        _consistLengthM = 0f;
        _sawAtSwitchThisLeg = false;
    }

    /// <summary>
    /// New Switch List pin-leg: do not inherit At-switch from the previous frog
    /// when the pin object stays on screen (cab 2.13.2.5.5 C4S skip).
    /// </summary>
    public static void ResetSawAtSwitchThisLeg() => _sawAtSwitchThisLeg = false;

    public static void Apply(
        in RouteClearanceDecision decision,
        string? pinJunctionId,
        float pinX,
        float pinY,
        float pinZ,
        float? nosePastJunctionM = null,
        float consistLengthM = 0f)
    {
        _phase = decision.Phase;
        _caption = decision.Caption;
        _canThrowAlign = decision.CanThrowAlign;
        _canAdvanceNext = decision.CanAdvanceNext;
        _nosePastJunctionM = nosePastJunctionM;
        _consistLengthM = consistLengthM > 0f ? consistLengthM : 0f;

        var id = pinJunctionId?.Trim();
        if (string.IsNullOrEmpty(id) || decision.Phase == RouteClearancePhase.Idle)
        {
            _hasPin = false;
            _pinJunctionId = null;
            _pinX = _pinY = _pinZ = 0f;
            _nosePastJunctionM = null;
            _consistLengthM = 0f;
            _sawAtSwitchThisLeg = false;
            return;
        }

        if (!string.Equals(_pinJunctionId, id, System.StringComparison.Ordinal))
        {
            _sawAtSwitchThisLeg = false;
        }

        if (decision.Phase == RouteClearancePhase.AtSwitch)
        {
            _sawAtSwitchThisLeg = true;
        }

        _hasPin = true;
        _pinJunctionId = id;
        _pinX = pinX;
        _pinY = pinY;
        _pinZ = pinZ;
    }

    public static bool TryGetPinWorld(out float x, out float y, out float z)
    {
        if (!_hasPin)
        {
            x = y = z = 0f;
            return false;
        }

        x = _pinX;
        y = _pinY;
        z = _pinZ;
        return true;
    }
}
