namespace YardMasterSuite.Core;

/// <summary>
/// Desk Take puts the booklet in a numbered hotbar slot and equips it.
/// World-spawned paper at the player's feet lets the office overview eat
/// the job with nothing left to turn in (cab 2.13.2.5.22.21).
/// </summary>
public static class RemoteTakeInventoryPolicy
{
    public static bool SpawnInWorldStorage => false;

    public static int ResolveSlot(int firstFreeHotbarSlot, int firstFreeAnySlot)
    {
        if (firstFreeHotbarSlot >= 0)
        {
            return firstFreeHotbarSlot;
        }

        return firstFreeAnySlot;
    }

    public static bool ShouldEquipAfterAdd(int slot) => slot >= 0;
}
