namespace YardMasterSuite.Core;

public struct TrainGadgetCache
{
    public int GradeTenths;
    public int MassTonnes;
    public int Handbrakes;
    public bool HasHandbrakes;
    public int FuelPercent;
    public int OilPercent;
    public int LoadPercent;
    public int Motors;
    public int DerailRisk;
    public int DerailLead;
    public int Mu;
    public bool Seeded;
    public bool Known;
}

public enum TrainGadgetLogKind
{
    Init = 0,
    Change = 1,
    Hide = 2,
}

/// <summary>
/// Unity-free Mass + Grade + Load + Fluids + Motors + Derail Risk + MU gate. HUD updates when a
/// display bucket changes; T2 is init / change / hide — not every 10 Hz sample.
/// Derail Risk T2: <c>risk=</c> consist max, <c>lead=</c> boarded loco (not per-car spam).
/// </summary>
public static class TrainGadgetTelemetry
{
    public const float MinChangeLogSeconds = 2f;

    public static bool Observe(
        bool known,
        float? gradePercent,
        float? massTonnes,
        int? handbrakes,
        float? fuelPercent,
        float? oilPercent,
        float? loadPercent,
        MotorStatus? motors,
        float? derailRiskPercent,
        float? derailLeadPercent,
        FreeMotionSeverity mu,
        ref TrainGadgetCache cache)
    {
        var gradeTenths = GradeDisplay.BucketTenths(gradePercent);
        var mass = TonnageDisplay.BucketTonnes(massTonnes);
        var hasHandbrakes = handbrakes.HasValue;
        var hb = handbrakes.GetValueOrDefault();
        var fuel = FluidDisplay.BucketPercent(fuelPercent);
        var oil = FluidDisplay.BucketPercent(oilPercent);
        var load = LoadDisplay.BucketPercent(loadPercent);
        var motor = MotorDisplay.Bucket(motors);
        var risk = DerailRiskDisplay.BucketPercent(derailRiskPercent);
        var lead = DerailRiskDisplay.BucketPercent(derailLeadPercent);
        var muBucket = (int)mu;

        if (!cache.Seeded)
        {
            cache.Seeded = true;
            cache.Known = known;
            if (!known)
            {
                return false;
            }

            WriteCache(ref cache, gradeTenths, mass, hasHandbrakes, hb, fuel, oil, load, motor, risk, lead, muBucket);
            return true;
        }

        if (cache.Known == known
            && (!known
                || (cache.GradeTenths == gradeTenths
                    && cache.MassTonnes == mass
                    && cache.HasHandbrakes == hasHandbrakes
                    && cache.Handbrakes == hb
                    && cache.FuelPercent == fuel
                    && cache.OilPercent == oil
                    && cache.LoadPercent == load
                    && cache.Motors == motor
                    && cache.DerailRisk == risk
                    && cache.DerailLead == lead
                    && cache.Mu == muBucket)))
        {
            return false;
        }

        cache.Known = known;
        if (known)
        {
            WriteCache(ref cache, gradeTenths, mass, hasHandbrakes, hb, fuel, oil, load, motor, risk, lead, muBucket);
        }

        return true;
    }

    public static string? NextLog(
        float? gradePercent,
        float? massTonnes,
        float? fuelPercent,
        float? oilPercent,
        float? loadPercent,
        MotorStatus? motors,
        float? derailRiskPercent,
        float? derailLeadPercent,
        FreeMotionSeverity mu,
        TrainGadgetLogKind kind,
        float nowSeconds,
        ref float lastChangeLogAt)
    {
        if (kind == TrainGadgetLogKind.Hide)
        {
            return "T2 gadgets hide";
        }

        if (kind == TrainGadgetLogKind.Change
            && nowSeconds - lastChangeLogAt < MinChangeLogSeconds)
        {
            return null;
        }

        lastChangeLogAt = nowSeconds;
        var grade = GradeDisplay.FormatSignedToken(gradePercent);
        var mass = TonnageDisplay.FormatTonnesToken(massTonnes);
        var load = LoadDisplay.FormatPercentToken(loadPercent);
        var fuel = FluidDisplay.FormatPercentToken(fuelPercent);
        var oil = FluidDisplay.FormatPercentToken(oilPercent);
        var motor = MotorDisplay.FormatToken(motors);
        var risk = DerailRiskDisplay.FormatPercentToken(derailRiskPercent);
        var lead = DerailRiskDisplay.FormatPercentToken(derailLeadPercent);
        var muToken = ConsistFreeMotion.FormatToken(mu);
        var prefix = kind == TrainGadgetLogKind.Init ? "T2 gadgets init: " : "T2 gadgets change: ";
        return prefix
            + "grade=" + grade
            + " mass=" + mass
            + " load=" + load
            + " fuel=" + fuel
            + " oil=" + oil
            + " motors=" + motor
            + " mu=" + muToken
            + " risk=" + risk
            + " lead=" + lead;
    }

    private static void WriteCache(
        ref TrainGadgetCache cache,
        int gradeTenths,
        int mass,
        bool hasHandbrakes,
        int hb,
        int fuel,
        int oil,
        int load,
        int motor,
        int risk,
        int lead,
        int mu)
    {
        cache.GradeTenths = gradeTenths;
        cache.MassTonnes = mass;
        cache.HasHandbrakes = hasHandbrakes;
        cache.Handbrakes = hb;
        cache.FuelPercent = fuel;
        cache.OilPercent = oil;
        cache.LoadPercent = load;
        cache.Motors = motor;
        cache.DerailRisk = risk;
        cache.DerailLead = lead;
        cache.Mu = mu;
    }
}
