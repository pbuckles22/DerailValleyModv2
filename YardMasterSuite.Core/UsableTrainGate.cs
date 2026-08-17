namespace YardMasterSuite.Core;

/// <summary>
/// v1 **4.3** — loco bar visibility. No consist memory on foot when there is no usable loco train.
/// </summary>
public static class UsableTrainGate
{
    public static bool ShouldShowLocoBar(bool hasUsableLocoTrain) => hasUsableLocoTrain;
}
