using UnityEngine;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Silent frametime / GC hitch monitor. Story 1.3 fills in Update();
    /// 1.1 only needs a MonoBehaviour the UMM entry can AddComponent.
    /// </summary>
    public sealed class GcCadenceProbe : MonoBehaviour
    {
    }
}
