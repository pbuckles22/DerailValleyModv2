namespace YardMasterSuite.Core
{
    /// <summary>
    /// Own-loco AR: show LastLoco while on foot or on a freight car of that consist.
    /// Hide only when the player is standing on the locomotive itself (3.2 / 6.16).
    /// </summary>
    public static class ArLocoGate
    {
        public static bool ShouldShow(bool hasLoco, bool playerIsOnThatLoco) =>
            hasLoco && !playerIsOnThatLoco;
    }
}
