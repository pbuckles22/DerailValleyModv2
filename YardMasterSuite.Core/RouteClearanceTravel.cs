using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Travel-axis meters past a pin. Golden <c>2.8.7.2</c> is
/// <c>Dot(car − pin, loco.forward)</c> at the sampled car.
/// When the desk says Set Reverse, the butt is the virtual nose: sample the
/// rear car and use <see cref="LeadingEdgePastM"/>. Pin in the windshield
/// after you reverse through the frog is the cleared side, not "still approaching".
/// The fwd-end form <c>−golden + length</c> is the same math when the sample
/// is the +forward coupler instead of the butt.
/// </summary>
public static class RouteClearanceTravel
{
    public static float GoldenNosePastM(
        float noseX,
        float noseZ,
        float pinX,
        float pinZ,
        float locoForwardX,
        float locoForwardZ)
    {
        var mag = Math.Sqrt((locoForwardX * locoForwardX) + (locoForwardZ * locoForwardZ));
        if (mag < 1e-6)
        {
            return 0f;
        }

        var fx = (float)(locoForwardX / mag);
        var fz = (float)(locoForwardZ / mag);
        var dx = noseX - pinX;
        var dz = noseZ - pinZ;
        return (dx * fx) + (dz * fz);
    }

    /// <summary>
    /// <paramref name="goldenFromLeadingCar"/> is Dot(lead − pin, fwd) at the
    /// travel-leading coupler (butt when reverse, hood when forward).
    /// </summary>
    public static float LeadingEdgePastM(float goldenFromLeadingCar, bool travelReverse) =>
        travelReverse ? -goldenFromLeadingCar : goldenFromLeadingCar;

    /// <param name="travelReverse">
    /// Latched Set Reverse: convert a <b>+forward-end</b> golden into butt-leading
    /// meters (<c>−golden + length</c>). Do not pass live windshield IsPinBehind
    /// — after you pass, the pin is in front and that flip blocks CLEARED.
    /// </param>
    public static float TravelPastJunctionM(
        float goldenNosePastM,
        float consistLengthM,
        bool travelReverse) =>
        travelReverse ? (-goldenNosePastM) + consistLengthM : goldenNosePastM;

    public static float TravelPastJunctionM(
        float noseX,
        float noseZ,
        float pinX,
        float pinZ,
        float locoForwardX,
        float locoForwardZ,
        float consistLengthM,
        bool travelReverse)
    {
        var golden = GoldenNosePastM(noseX, noseZ, pinX, pinZ, locoForwardX, locoForwardZ);
        return TravelPastJunctionM(golden, consistLengthM, travelReverse);
    }

    /// <summary>
    /// Unity poll: hood world pose + latched travel axis. Solo reverse samples the
    /// butt (travel-leading edge), not the hood — smoke: pin in windshield after
    /// backing through is past-side, not still approaching.
    /// </summary>
    public static float SampleTravelPastM(
        float hoodX,
        float hoodZ,
        float pinX,
        float pinZ,
        float locoForwardX,
        float locoForwardZ,
        float consistLengthM,
        bool travelUsesReverse,
        bool soloConsist)
    {
        var mag = Math.Sqrt((locoForwardX * locoForwardX) + (locoForwardZ * locoForwardZ));
        if (mag < 1e-6f)
        {
            return 0f;
        }

        var fx = (float)(locoForwardX / mag);
        var fz = (float)(locoForwardZ / mag);
        var sampleX = hoodX;
        var sampleZ = hoodZ;
        if (travelUsesReverse && soloConsist && consistLengthM > 0f)
        {
            sampleX -= fx * consistLengthM;
            sampleZ -= fz * consistLengthM;
        }

        var golden = GoldenNosePastM(sampleX, sampleZ, pinX, pinZ, fx, fz);
        return travelUsesReverse
            ? TravelPastJunctionM(golden, consistLengthM, travelReverse: true)
            : golden;
    }
}
