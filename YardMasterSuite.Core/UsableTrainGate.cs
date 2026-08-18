namespace YardMasterSuite.Core;

/// <summary>
/// v1 **4.3** — loco bar visibility. No consist memory on foot when there is no usable loco train.
/// </summary>
public static class UsableTrainGate
{
    public static bool ShouldShowLocoBar(bool hasUsableLocoTrain) => hasUsableLocoTrain;

    /// <summary>
    /// Publish when the 4.3 gate or the usable consist anchor changes
    /// (look-at switch between trains stays <c>usable=true</c>).
    /// </summary>
    public static bool ShouldPublish(
        bool seeded,
        bool lastUsable,
        int lastAnchorId,
        bool usable,
        int anchorId)
    {
        if (!seeded)
        {
            return true;
        }

        return lastUsable != usable || lastAnchorId != anchorId;
    }
}
