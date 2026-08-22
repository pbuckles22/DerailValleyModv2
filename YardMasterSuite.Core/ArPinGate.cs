namespace YardMasterSuite.Core
{
    /// <summary>
    /// Home-mark AR pin (6.15 / v1 4.9). Show while a session mark exists and
    /// the player is not standing on it.
    /// </summary>
    public static class ArPinGate
    {
        public const float HideRadiusMeters = 8f;

        public static bool ShouldShow(bool hasMark, bool atPin) =>
            hasMark && !atPin;

        public static bool IsAtPin(
            float pinX,
            float pinZ,
            float playerX,
            float playerZ,
            float radiusMeters = HideRadiusMeters)
        {
            var dx = pinX - playerX;
            var dz = pinZ - playerZ;
            var r = radiusMeters;
            return (dx * dx) + (dz * dz) <= r * r;
        }
    }
}
