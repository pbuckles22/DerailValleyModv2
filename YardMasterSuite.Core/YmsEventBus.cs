namespace YardMasterSuite.Core
{
    /// <summary>
    /// Central Type A event bus. Story 1.2 adds Actions; 1.1 only needs the
    /// unsubscribe hook the UMM entry already calls on deactivate.
    /// </summary>
    public static class YmsEventBus
    {
        public static void ClearAllSubscriptions()
        {
        }
    }
}
