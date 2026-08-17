namespace YardMasterSuite.Core
{
    /// <summary>
    /// Own-loco AR: show LastLoco while on foot; hide when the player is in that loco.
    /// </summary>
    public static class ArLocoGate
    {
        public static bool ShouldShow(bool hasLoco, bool playerIsOnThatLoco) =>
            hasLoco && !playerIsOnThatLoco;
    }
}
