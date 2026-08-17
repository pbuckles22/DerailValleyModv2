namespace YardMasterSuite.Core
{
    /// <summary>
    /// Office-apron hide for the house AR marker (flat XZ radius).
    /// Exact building AABB is a later refinement — radius is the Smoke A lock.
    /// </summary>
    public static class ArOfficeGate
    {
        public const float HideRadiusMeters = 20f;

        public static bool ShouldShow(bool hasInZoneStation, bool atOffice) =>
            hasInZoneStation && !atOffice;

        public static bool IsAtOffice(
            float officeX,
            float officeZ,
            float playerX,
            float playerZ,
            float radiusMeters = HideRadiusMeters)
        {
            var dx = officeX - playerX;
            var dz = officeZ - playerZ;
            var r = radiusMeters;
            return (dx * dx) + (dz * dz) <= r * r;
        }
    }
}
