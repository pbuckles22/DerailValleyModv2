namespace YardMasterSuite.Core;

/// <summary>
/// Published each OnGUI after the HUD stack lays out. AR sticky row reads
/// <see cref="LastBottomGuiY"/> (v1 <c>LastStackBottomGuiY</c>).
/// </summary>
public static class HudStackLayout
{
    public static float LastBottomGuiY { get; private set; }

    public static void PublishLastBottomGuiY(float guiY) => LastBottomGuiY = guiY;

    public static void Reset() => LastBottomGuiY = 0f;
}
