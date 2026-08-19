using System;

namespace YardMasterSuite.Core;

public struct TrainGadgetCache
{
    public int GradeTenths;
    public int MassTonnes;
    public int Handbrakes;
    public bool HasHandbrakes;
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
/// Unity-free Mass + Grade gate. HUD updates when the 0.1 % grade bucket or
/// whole-tonne mass (or handbrake count) changes; T2 is init / change / hide
/// — not every 10 Hz sample.
/// </summary>
public static class TrainGadgetTelemetry
{
    public const float MinChangeLogSeconds = 2f;

    public static bool Observe(
        bool known,
        float? gradePercent,
        float? massTonnes,
        int? handbrakes,
        ref TrainGadgetCache cache)
    {
        var gradeTenths = GradeDisplay.BucketTenths(gradePercent);
        var mass = TonnageDisplay.BucketTonnes(massTonnes);
        var hasHandbrakes = handbrakes.HasValue;
        var hb = handbrakes.GetValueOrDefault();

        if (!cache.Seeded)
        {
            cache.Seeded = true;
            cache.Known = known;
            if (!known)
            {
                return false;
            }

            cache.GradeTenths = gradeTenths;
            cache.MassTonnes = mass;
            cache.HasHandbrakes = hasHandbrakes;
            cache.Handbrakes = hb;
            return true;
        }

        if (cache.Known == known
            && (!known
                || (cache.GradeTenths == gradeTenths
                    && cache.MassTonnes == mass
                    && cache.HasHandbrakes == hasHandbrakes
                    && cache.Handbrakes == hb)))
        {
            return false;
        }

        cache.Known = known;
        if (known)
        {
            cache.GradeTenths = gradeTenths;
            cache.MassTonnes = mass;
            cache.HasHandbrakes = hasHandbrakes;
            cache.Handbrakes = hb;
        }

        return true;
    }

    public static string? NextLog(
        float? gradePercent,
        float? massTonnes,
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
        if (kind == TrainGadgetLogKind.Init)
        {
            return "T2 gadgets init: grade=" + grade + " mass=" + mass;
        }

        return "T2 gadgets change: grade=" + grade + " mass=" + mass;
    }
}
