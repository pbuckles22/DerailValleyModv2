namespace YardMasterSuite.Core
{
    /// <summary>
    /// Boarded loco identity for Type A bus payloads. Instance id 0 = unboarded.
    /// Never pass a TrainCar (class) on the bus.
    /// </summary>
    public readonly struct LocoPresence
    {
        public readonly int InstanceId;

        public LocoPresence(int instanceId)
        {
            InstanceId = instanceId;
        }

        public bool IsBoarded => InstanceId != 0;

        public static LocoPresence None => default;
    }

    /// <summary>
    /// Cached boarded-loco instance id. 0 = not boarded on a loco.
    /// </summary>
    public struct LocoStateCache
    {
        public int CurrentInstanceId;
    }

    /// <summary>
    /// Unity-free board/unboard gate. The listener maps TrainCar → instance id
    /// (0 if null or not a loco) and calls <see cref="Observe"/>.
    /// </summary>
    public static class LocoState
    {
        /// <summary>
        /// Returns a T2 line when presence changes; null when unchanged.
        /// Allocates only on a real board/unboard/switch.
        /// </summary>
        public static string? Observe(int instanceId, ref LocoStateCache cache)
        {
            if (instanceId == cache.CurrentInstanceId)
            {
                return null;
            }

            var previous = cache.CurrentInstanceId;
            cache.CurrentInstanceId = instanceId;
            if (instanceId != 0)
            {
                return "T2 loco-board: id=" + instanceId;
            }

            return "T2 loco-unboard: id=" + previous;
        }
    }
}
