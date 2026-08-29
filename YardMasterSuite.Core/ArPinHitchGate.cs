namespace YardMasterSuite.Core;

/// <summary>
/// Pin AR LateUpdate + OnGUI diet. Smoke 2.8.7.29 frog/CLEARED with the desk
/// off scored <c>feature=11–17</c>. Cab reverse cruise is already gold
/// <c>feature=0</c>; this gate cuts WorldToScreen + IMGUI Layout/CalcSize
/// while a route pin is up in the cab. On-foot park pin stays per-frame.
/// </summary>
public static class ArPinHitchGate
{
    /// <summary>20 Hz while throttling — camera still moves the pin on screen.</summary>
    public const float ProjectIntervalSeconds = 0.05f;

    public const float PinWorldEpsilonMeters = 0.05f;

    public static bool ShouldThrottleProject(bool boardedLoco, bool routePinOccupied) =>
        boardedLoco && routePinOccupied;

    public static bool ShouldProject(
        bool throttleCabRoutePin,
        bool identityChanged,
        float secondsSinceProject)
    {
        if (identityChanged || !throttleCabRoutePin)
        {
            return true;
        }

        return secondsSinceProject >= ProjectIntervalSeconds;
    }

    /// <summary>Absolute-rect IMGUI: Layout/Used/mouse must not CalcSize or restack.</summary>
    public static bool ShouldRunOnGuiPass(bool eventIsRepaint) => eventIsRepaint;

    public static bool ShouldRemeasureCaptions(bool captionDirty, bool screenSizeChanged) =>
        captionDirty || screenSizeChanged;

    public static bool PinWorldMoved(
        float ax,
        float ay,
        float az,
        float bx,
        float by,
        float bz)
    {
        var dx = ax - bx;
        var dy = ay - by;
        var dz = az - bz;
        var e = PinWorldEpsilonMeters;
        return (dx * dx) + (dy * dy) + (dz * dz) > e * e;
    }

    public static string? ObserveThrottle(bool throttle, ref bool wasThrottle)
    {
        if (throttle == wasThrottle)
        {
            return null;
        }

        wasThrottle = throttle;
        return throttle ? "T2 ar-pin: hitch throttle" : "T2 ar-pin: hitch full";
    }
}
