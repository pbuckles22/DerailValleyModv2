namespace YardMasterSuite.Core;

/// <summary>Place-mode session for loco re-rail (**8.6**). Turn-in-place is one-shot (no session).</summary>
public static class LocoRerailSession
{
    private static bool _active;
    private static string _typeId = string.Empty;
    private static string? _targetTrackId;
    private static bool _forceRegularDirection = true;
    private static float _aimX;
    private static float _aimY;
    private static float _aimZ;
    private static bool _hasAim;
    private static bool _targetLocked;

    public static bool IsActive => _active;

    public static string TypeId => _typeId;

    public static string? TargetTrackId => _targetTrackId;

    public static bool ForceRegularDirection => _forceRegularDirection;

    public static bool HasLatchedTarget => _hasAim && !string.IsNullOrEmpty(_targetTrackId);

    public static bool IsTargetLocked => _targetLocked;

    public static void Begin(string typeId)
    {
        _typeId = LocoTypeId.Normalize(typeId);
        _active = !string.IsNullOrEmpty(_typeId);
        // Keep last aim across re-arm of the same session type so desk clicks do not wipe it.
        if (!_active)
        {
            _targetTrackId = null;
            _hasAim = false;
            _targetLocked = false;
        }

        _forceRegularDirection = true;
    }

    /// <summary>Update aim from look-at. No-op while locked.</summary>
    public static void SetTarget(string? trackId, float aimX, float aimY, float aimZ)
    {
        if (!_active || _targetLocked)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(trackId))
        {
            return;
        }

        _targetTrackId = trackId!.Trim();
        _aimX = aimX;
        _aimY = aimY;
        _aimZ = aimZ;
        _hasAim = true;
    }

    /// <summary>
    /// Soft clear for a poll miss — keeps the last good aim so moving the mouse
    /// onto the desk does not drop the destination.
    /// </summary>
    public static void ClearTargetIfUnlocked()
    {
        if (_targetLocked)
        {
            return;
        }

        // Intentional no-op: latch last good until Lock / Confirm / Cancel / Clear.
    }

    public static void LockTarget()
    {
        if (_active && HasLatchedTarget)
        {
            _targetLocked = true;
        }
    }

    public static void UnlockTarget()
    {
        _targetLocked = false;
    }

    public static bool TryGetAimPoint(out float x, out float y, out float z)
    {
        x = _aimX;
        y = _aimY;
        z = _aimZ;
        return _hasAim;
    }

    public static void ToggleFacing() => _forceRegularDirection = !_forceRegularDirection;

    public static void Clear()
    {
        _active = false;
        _typeId = string.Empty;
        _targetTrackId = null;
        _forceRegularDirection = true;
        _hasAim = false;
        _targetLocked = false;
    }
}
