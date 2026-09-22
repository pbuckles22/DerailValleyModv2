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
    private static float? _bestNosePastMeters;

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

    /// <summary>Best nose-past on this pin after At switch. Ignores curve regress.</summary>
    public static float? BestNosePastMeters => _bestNosePastMeters;

    /// <summary>
    /// Meters until frog CLEARED (tail past envelope) — primary Past-switch taper rem.
    /// Live couple length wins over a stale pin-poll sample so GO does not kiss
    /// at loco-on-frog after the consist grows (cab 2.13.2.5.14).
    /// </summary>
    public static float? RemToClearedMeters
    {
        get
        {
            if (!_hasPin)
            {
                return null;
            }

            var len = _consistLengthM;
            var live = ConsistLengthSession.Meters;
            if (live > len)
            {
                len = live;
            }

            return YardApproachKinematics.RemToClearedMeters(
                _nosePastJunctionM,
                len,
                RouteClearanceEval.DefaultFrogEnvelopeM);
        }
    }

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
        _bestNosePastMeters = null;
    }

    /// <summary>
    /// New Switch List pin-leg: do not inherit At-switch from the previous frog
    /// when the pin object stays on screen (cab 2.13.2.5.5 C4S skip).
    /// </summary>
    public static void ResetSawAtSwitchThisLeg()
    {
        _sawAtSwitchThisLeg = false;
        _bestNosePastMeters = null;
    }

    /// <summary>
    /// When the list is on a CLEARED-frog row with a board pin, only that
    /// frog may enter this session. No board / not a frog row → accept.
    /// </summary>
    public static bool ShouldAcceptPin(string? pinJunctionId)
    {
        var step = SwitchListSession.CurrentStep;
        if (!SwitchListPinFacing.IsClearedFrogPin(step))
        {
            return true;
        }

        var board = RoutePinBoardSession.PinIdForStep(step!.Index);
        if (string.IsNullOrEmpty(board))
        {
            return true;
        }

        var id = pinJunctionId?.Trim();
        return !string.IsNullOrEmpty(id)
            && string.Equals(board, id, System.StringComparison.Ordinal);
    }

    public static void Apply(
        in RouteClearanceDecision decision,
        string? pinJunctionId,
        float pinX,
        float pinY,
        float pinZ,
        float? nosePastJunctionM = null,
        float consistLengthM = 0f)
    {
        var id = pinJunctionId?.Trim();
        if (string.IsNullOrEmpty(id) || decision.Phase == RouteClearancePhase.Idle)
        {
            _phase = decision.Phase;
            _caption = decision.Caption;
            _canThrowAlign = decision.CanThrowAlign;
            _canAdvanceNext = decision.CanAdvanceNext;
            _hasPin = false;
            _pinJunctionId = null;
            _pinX = _pinY = _pinZ = 0f;
            _nosePastJunctionM = null;
            _consistLengthM = 0f;
            _sawAtSwitchThisLeg = false;
            _bestNosePastMeters = null;
            return;
        }

        // CLEARED-frog steps: leftover board pins (e.g. row 8) must not
        // At-switch / CLEARED-complete while this step's pin is active.
        if (!ShouldAcceptPin(id))
        {
            return;
        }

        if (!string.Equals(_pinJunctionId, id, System.StringComparison.Ordinal))
        {
            _sawAtSwitchThisLeg = false;
            _bestNosePastMeters = null;
        }

        var phase = decision.Phase;
        var caption = decision.Caption;
        var canThrow = decision.CanThrowAlign;
        var canNext = decision.CanAdvanceNext;
        if (phase == RouteClearancePhase.Cleared && !_sawAtSwitchThisLeg)
        {
            phase = RouteClearancePhase.Approaching;
            caption = null;
            canThrow = false;
            canNext = false;
        }

        _phase = phase;
        _caption = caption;
        _canThrowAlign = canThrow;
        _canAdvanceNext = canNext;
        _nosePastJunctionM = nosePastJunctionM;
        _consistLengthM = consistLengthM > 0f ? consistLengthM : 0f;

        if (phase == RouteClearancePhase.AtSwitch)
        {
            _sawAtSwitchThisLeg = true;
        }

        if (_sawAtSwitchThisLeg && nosePastJunctionM is float nose)
        {
            if (_bestNosePastMeters is not float best || nose > best)
            {
                _bestNosePastMeters = nose;
            }
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
