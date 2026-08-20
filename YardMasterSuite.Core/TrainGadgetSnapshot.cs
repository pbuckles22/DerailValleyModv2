namespace YardMasterSuite.Core;

/// <summary>Loco gadget chips for the train bar (Epic 6 wave 2).</summary>
public readonly struct TrainGadgetSnapshot
{
    public readonly float? FuelPercent;
    public readonly float? OilPercent;
    public readonly float? MassTonnes;
    public readonly float? GradePercent;
    public readonly float? LoadPercent;
    public readonly MotorStatus? Motors;
    public readonly int? HandbrakeApplied;
    public readonly FreeMotionSeverity Mu;

    public TrainGadgetSnapshot(
        float? fuelPercent = null,
        float? oilPercent = null,
        float? massTonnes = null,
        float? gradePercent = null,
        float? loadPercent = null,
        MotorStatus? motors = null,
        int? handbrakeApplied = null,
        FreeMotionSeverity mu = FreeMotionSeverity.None)
    {
        FuelPercent = fuelPercent;
        OilPercent = oilPercent;
        MassTonnes = massTonnes;
        GradePercent = gradePercent;
        LoadPercent = loadPercent;
        Motors = motors;
        HandbrakeApplied = handbrakeApplied;
        Mu = mu;
    }
}
