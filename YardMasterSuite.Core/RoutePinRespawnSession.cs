namespace YardMasterSuite.Core;

/// <summary>
/// After a shared 1+4 frog is spent, hide the combined chip then spawn a
/// fresh caption-only pin at the cached switch XYZ.
/// </summary>
public static class RoutePinRespawnSession
{
    public static bool Active { get; private set; }

    public static string? PinId { get; private set; }

    public static string Caption { get; private set; } = "4";

    public static float X { get; private set; }

    public static float Y { get; private set; }

    public static float Z { get; private set; }

    public static bool HasWorld { get; private set; }

    public static void Arm(
        string? pinId,
        string? caption,
        float x,
        float y,
        float z,
        bool hasWorld)
    {
        var id = pinId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            Clear();
            return;
        }

        var cap = caption?.Trim();
        PinId = id;
        Caption = string.IsNullOrEmpty(cap) ? "4" : cap!;
        X = x;
        Y = y;
        Z = z;
        HasWorld = hasWorld;
        Active = true;
    }

    public static void Clear()
    {
        Active = false;
        PinId = null;
        Caption = "4";
        X = Y = Z = 0f;
        HasWorld = false;
    }
}
