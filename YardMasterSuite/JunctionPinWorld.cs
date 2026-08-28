using UnityEngine;

namespace YardMasterSuite
{
    /// <summary>Golden <c>2.8.7.2</c>: pin world is junction transform (no frog topology).</summary>
    internal static class JunctionPinWorld
    {
        internal static bool TryGet(Junction? junction, out float x, out float y, out float z)
        {
            x = y = z = 0f;
            if (junction == null)
            {
                return false;
            }

            try
            {
                var pos = junction.transform.position;
                x = pos.x;
                y = pos.y;
                z = pos.z;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
