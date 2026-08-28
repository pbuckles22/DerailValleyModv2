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

    public static RouteClearancePhase Phase => _phase;

    public static bool HasPin => _hasPin;

    public static string? PinJunctionId => _pinJunctionId;

    public static string? Caption => _caption;

    public static bool CanThrowAlign => _canThrowAlign;

    public static bool CanAdvanceNext => _canAdvanceNext;

    public static void Clear()
    {
        _phase = RouteClearancePhase.Idle;
        _hasPin = false;
        _pinJunctionId = null;
        _caption = null;
        _pinX = _pinY = _pinZ = 0f;
        _canThrowAlign = true;
        _canAdvanceNext = true;
    }

    public static void Apply(
        in RouteClearanceDecision decision,
        string? pinJunctionId,
        float pinX,
        float pinY,
        float pinZ)
    {
        _phase = decision.Phase;
        _caption = decision.Caption;
        _canThrowAlign = decision.CanThrowAlign;
        _canAdvanceNext = decision.CanAdvanceNext;

        var id = pinJunctionId?.Trim();
        if (string.IsNullOrEmpty(id) || decision.Phase == RouteClearancePhase.Idle)
        {
            _hasPin = false;
            _pinJunctionId = null;
            _pinX = _pinY = _pinZ = 0f;
            return;
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
