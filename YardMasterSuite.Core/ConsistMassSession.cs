namespace YardMasterSuite.Core;

/// <summary>Live consist mass (tonnes) for yard kiss d_stop. 0 = use 40 t floor.</summary>
public static class ConsistMassSession
{
    public static float Tonnes { get; private set; }

    public static void Observe(float tonnes)
    {
        if (float.IsNaN(tonnes) || float.IsInfinity(tonnes) || tonnes <= 0f)
        {
            return;
        }

        Tonnes = tonnes;
    }

    public static void Clear() => Tonnes = 0f;
}
